"""Builds App/wwwroot/data/outreach.json: Albany Creek's walkable streets with letterbox counts per side.

Streets: OpenStreetMap via Overpass (ODbL). Addresses: Queensland Government Addresses layer (CC BY 4.0).
Only counts per street side are written; no address leaves this script.

    python3 tools/outreach/build_data.py
"""

import heapq
import json
import math
import random
import sys
import urllib.parse
import urllib.request
from collections import defaultdict
from pathlib import Path

SOUTH, WEST, NORTH, EAST = -27.3720, 152.9380, -27.3280, 152.9900
STREET_MARGIN = 0.004
CHURCH = (-27.34335, 152.96467)
DELIVERY_ROADS = {"residential", "living_street", "unclassified", "tertiary", "secondary", "primary"}
ARTERIALS = {"primary", "secondary"}
WALK_THROUGH = {"residential", "living_street", "unclassified"}
MAX_SETBACK_M = 200
AREA_LETTERBOXES = 55
OUTREACH_ROUTES = 9
USER_AGENT = "GSBC-ChurchApp-POC/0.1 (+https://github.com/Asherslab/GSBC.ChurchApp)"
OVERPASS = "https://overpass-api.de/api/interpreter"
ADDRESSES = ("https://spatial-gis.information.qld.gov.au/arcgis/rest/services/PlanningCadastre/"
             "LandParcelPropertyFramework/MapServer/0/query")
EARTH_M = 6371000.0
OUT = Path(__file__).resolve().parents[2] / "App" / "wwwroot" / "data" / "outreach.json"

ABBREVIATIONS = {"Street": "St", "Road": "Rd", "Court": "Ct", "Drive": "Dr", "Crescent": "Cres", "Place": "Pl",
                 "Avenue": "Ave", "Close": "Cl", "Parade": "Pde", "Terrace": "Tce", "Circuit": "Cct",
                 "Boulevard": "Bvd", "Lane": "Ln", "Way": "Way", "Grove": "Gr", "Highway": "Hwy"}


def fetch(url, data=None):
    request = urllib.request.Request(url, data=data, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request, timeout=120) as response:
        return json.load(response)


def fetch_streets():
    box = f"{SOUTH - STREET_MARGIN},{WEST - STREET_MARGIN},{NORTH + STREET_MARGIN},{EAST + STREET_MARGIN}"
    roads = "|".join(sorted(DELIVERY_ROADS))
    query = f'[out:json][timeout:90];way["highway"~"^({roads})$"]({box});(._;>;);out body;'
    return fetch(OVERPASS, urllib.parse.urlencode({"data": query}).encode())


def fetch_addresses():
    features, offset = [], 0
    while True:
        params = {
            "geometry": f"{WEST},{SOUTH},{EAST},{NORTH}", "geometryType": "esriGeometryEnvelope", "inSR": 4326,
            "spatialRel": "esriSpatialRelIntersects", "where": "1=1", "outFields": "unit_number,street_full",
            "returnGeometry": "true", "outSR": 4326, "orderByFields": "objectid", "resultOffset": offset,
            "resultRecordCount": 2000, "f": "json",
        }
        page = fetch(f"{ADDRESSES}?{urllib.parse.urlencode(params)}")
        features += page["features"]
        if not page.get("exceededTransferLimit"):
            return features
        offset += len(page["features"])


def local_xy(lat, lon):
    lat0 = math.radians((SOUTH + NORTH) / 2)
    return math.radians(lon) * EARTH_M * math.cos(lat0), math.radians(lat) * EARTH_M


def nearest_on_segment(p, a, b):
    ax, ay = b[0] - a[0], b[1] - a[1]
    t = max(0.0, min(1.0, ((p[0] - a[0]) * ax + (p[1] - a[1]) * ay) / ((ax * ax + ay * ay) or 1)))
    q = (a[0] + t * ax, a[1] + t * ay)
    return math.dist(p, q), ax * (p[1] - a[1]) - ay * (p[0] - a[0])


def build_edges(raw):
    lat_lon = {e["id"]: (e["lat"], e["lon"]) for e in raw["elements"] if e["type"] == "node"}
    ways = [e for e in raw["elements"] if e["type"] == "way"]
    uses = defaultdict(int)
    for way in ways:
        for i, node in enumerate(way["nodes"]):
            uses[node] += 2 if i in (0, len(way["nodes"]) - 1) else 1
    edges = []
    for way in ways:
        segment = [way["nodes"][0]]
        for node in way["nodes"][1:]:
            segment.append(node)
            if uses[node] > 1 or node == way["nodes"][-1]:
                xy = [local_xy(*lat_lon[n]) for n in segment]
                edges.append({
                    "u": segment[0], "v": segment[-1], "name": way["tags"].get("name", ""),
                    "road": way["tags"]["highway"], "xy": xy, "ll": [lat_lon[n] for n in segment],
                    "length": sum(math.dist(xy[i], xy[i + 1]) for i in range(len(xy) - 1)),
                    "boxes": [0, 0], "units": [0, 0],
                })
                segment = [node]
    return edges


def count_letterboxes(edges, features):
    by_name = defaultdict(list)
    for i, edge in enumerate(edges):
        by_name[edge["name"].lower()].append(i)
    placed = unplaced = 0
    for feature in features:
        attributes = feature["attributes"]
        p = local_xy(feature["geometry"]["y"], feature["geometry"]["x"])
        best = None
        for i in by_name.get((attributes["street_full"] or "").lower(), []):
            xy = edges[i]["xy"]
            for j in range(len(xy) - 1):
                distance, cross = nearest_on_segment(p, xy[j], xy[j + 1])
                if best is None or distance < best[0]:
                    best = (distance, i, 0 if cross > 0 else 1)
        if best is None or best[0] > MAX_SETBACK_M:
            unplaced += 1
            continue
        _, i, side = best
        edges[i]["boxes"][side] += 1
        if attributes["unit_number"]:
            edges[i]["units"][side] += 1
        placed += 1
    return placed, unplaced


def adjacency(edges, ids):
    by_node = defaultdict(list)
    for i in ids:
        by_node[edges[i]["u"]].append(i)
        by_node[edges[i]["v"]].append(i)
    return {i: {j for n in (edges[i]["u"], edges[i]["v"]) for j in by_node[n] if j != i} for i in ids}


def letterboxes(edges, ids):
    return sum(sum(edges[i]["boxes"]) for i in ids)


def deliverable(edge):
    return edge["road"] not in ARTERIALS and (sum(edge["boxes"]) > 0 or edge["road"] in WALK_THROUGH)


def cut_areas(edges):
    served = [i for i, e in enumerate(edges) if deliverable(e)]
    neighbours = adjacency(edges, served)
    unassigned = set(served)
    areas = []
    for seed in sorted(served, key=lambda i: (edges[i]["xy"][0][1], edges[i]["xy"][0][0])):
        if seed not in unassigned or sum(edges[seed]["boxes"]) == 0:
            continue
        area, queue, total = [], [seed], 0
        while queue and total < AREA_LETTERBOXES:
            i = queue.pop(0)
            if i not in unassigned:
                continue
            unassigned.discard(i)
            area.append(i)
            total += sum(edges[i]["boxes"])
            queue += sorted(neighbours[i] & unassigned, key=lambda j: math.dist(edges[j]["xy"][0], edges[seed]["xy"][0]))
        areas.append(area)
    return merge_small(edges, areas, neighbours)


def merge_small(edges, areas, neighbours):
    while True:
        owner = {i: k for k, area in enumerate(areas) for i in area}
        small = [k for k, area in enumerate(areas) if area and letterboxes(edges, area) < AREA_LETTERBOXES // 2]
        merged = False
        for k in small:
            if not areas[k]:
                continue
            touching = {owner[j] for i in areas[k] for j in neighbours[i] if j in owner and owner[j] != k and areas[owner[j]]}
            if not touching:
                continue
            target = min(touching, key=lambda t: letterboxes(edges, areas[t]))
            for i in areas[k]:
                owner[i] = target
            areas[target] += areas[k]
            areas[k] = []
            merged = True
        if not merged:
            return [area for area in areas if area and letterboxes(edges, area) >= AREA_LETTERBOXES // 2]


def short_name(street):
    words = street.split()
    if words and words[-1] in ABBREVIATIONS:
        words[-1] = ABBREVIATIONS[words[-1]]
    return " ".join(words)


def area_name(edges, ids):
    totals = defaultdict(int)
    for i in ids:
        if edges[i]["name"]:
            totals[edges[i]["name"]] += sum(edges[i]["boxes"])
    names = [short_name(n) for n, _ in sorted(totals.items(), key=lambda kv: -kv[1])[:2]]
    return " & ".join(names) or "Unnamed streets"


def centroid(edges, ids):
    points = [p for i in ids for p in edges[i]["xy"]]
    return sum(p[0] for p in points) / len(points), sum(p[1] for p in points) / len(points)


def outreach_routes(edges, areas):
    church = local_xy(*CHURCH)
    by_distance = sorted(range(len(areas)), key=lambda k: math.dist(centroid(edges, areas[k]), church))
    edge_area = {i: k for k, area in enumerate(areas) for i in area}
    neighbours = adjacency(edges, list(edge_area))
    used, routes = set(), []
    for k in by_distance:
        if len(routes) == OUTREACH_ROUTES:
            break
        if k in used:
            continue
        touching = {edge_area[j] for i in areas[k] for j in neighbours[i]} - used - {k}
        partner = min(touching, key=lambda t: math.dist(centroid(edges, areas[t]), church), default=None)
        group = [k] if partner is None else [k, partner]
        used.update(group)
        routes.append(sorted(i for g in group for i in areas[g]))
    return routes


def write(edges, areas, routes, placed, unplaced):
    keep = list(range(len(edges)))
    index = {old: new for new, old in enumerate(keep)}
    nodes = sorted({n for i in keep for n in (edges[i]["u"], edges[i]["v"])})
    node_index = {n: k for k, n in enumerate(nodes)}
    rng = random.Random(31)
    data = {
        "attribution": "Streets © OpenStreetMap contributors (ODbL). Addresses © State of Queensland (CC BY 4.0).",
        "church": list(CHURCH),
        "bounds": [SOUTH, WEST, NORTH, EAST],
        "letterboxes": {"placed": placed, "unplaced": unplaced},
        "edges": [
            {
                "u": node_index[edges[i]["u"]], "v": node_index[edges[i]["v"]],
                "name": short_name(edges[i]["name"]), "road": edges[i]["road"],
                "length": round(edges[i]["length"]), "boxes": edges[i]["boxes"], "units": edges[i]["units"],
                "line": [[round(lon, 5), round(lat, 5)] for lat, lon in edges[i]["ll"]],
            }
            for i in keep
        ],
        "areas": [
            {"id": k + 1, "name": area_name(edges, area), "edges": sorted(index[i] for i in area),
             "lastDoneWeeksAgo": rng.choice([2, 3, 5, 6, 8, 9, 11, 14, 20, 26, None])}
            for k, area in enumerate(areas)
        ],
        "outreachRoutes": [
            {"id": k + 1, "name": area_name(edges, route), "edges": sorted(index[i] for i in route)}
            for k, route in enumerate(routes)
        ],
    }
    OUT.write_text(json.dumps(data, separators=(",", ":")))
    return len(keep), OUT.stat().st_size


def main():
    edges = build_edges(fetch_streets())
    placed, unplaced = count_letterboxes(edges, fetch_addresses())
    areas = cut_areas(edges)
    routes = outreach_routes(edges, areas)
    kept, size = write(edges, areas, routes, placed, unplaced)
    print(f"{kept} street segments, {placed} letterboxes placed, {unplaced} unplaced, "
          f"{len(areas)} areas, {len(routes)} outreach routes, {size // 1024} KB", file=sys.stderr)


if __name__ == "__main__":
    main()
