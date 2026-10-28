"""Builds App/wwwroot/data/outreach.json: Albany Creek's walkable streets with letterbox counts per side.

Streets: OpenStreetMap via Overpass (ODbL). Addresses: Queensland Government Addresses layer (CC BY 4.0).
Homes only: Moreton Bay lot land use (MBRC 2020, CC BY 4.0) and Brisbane City Plan 2014 zoning (Brisbane
City Council, CC BY 4.0). Only counts per street side, each letterbox's distance along its street in
thousandths, and the points of skipped non-homes, are written; no home address leaves this script.

    python3 tools/outreach/build_data.py
"""

import heapq
import json
import math
import random
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from collections import defaultdict
from pathlib import Path

SOUTH, WEST, NORTH, EAST = -27.3720, 152.9380, -27.3280, 153.0150
STREET_MARGIN = 0.004
CHURCH = (-27.34335, 152.96467)
ARTERIALS = {"trunk", "primary", "secondary", "trunk_link", "primary_link", "secondary_link", "tertiary_link"}
ROADS = {"residential", "living_street", "unclassified", "tertiary"} | ARTERIALS
WALK_THROUGH = {"residential", "living_street", "unclassified"}
MAX_SETBACK_M = 200
AREA_LETTERBOXES = 55
OUTREACH_ROUTES = 9
USER_AGENT = "GSBC-ChurchApp-POC/0.1 (+https://github.com/Asherslab/GSBC.ChurchApp)"
OVERPASS = "https://overpass-api.de/api/interpreter"
ADDRESSES = ("https://spatial-gis.information.qld.gov.au/arcgis/rest/services/PlanningCadastre/"
             "LandParcelPropertyFramework/MapServer/0/query")
MBRC_LOTS = ("https://services-ap1.arcgis.com/152ojN3Ts9H3cdtl/arcgis/rest/services/"
             "ZM_Zones_WebMercator_OpenData/FeatureServer/0/query")
BCC_ZONES = "https://services2.arcgis.com/dEKgZETqwmDAh1rP/arcgis/rest/services/Zoning_opendata/FeatureServer/0/query"
GRID_DEGREES = 0.002
SKIP_MERGE_DEGREES = 0.0003
EARTH_M = 6371000.0

MBRC_DELIVER = {"Single Unit Dwelling", "Multi Unit Dwelling (flats)", "Residential CTS Properties", "Retirement Village"}
MBRC_CHECK = {"", "Outbuildings", "Cattle Breeding & Fattening", "Turf Farms"}
MBRC_SKIP = [
    ("Shopping Centre", "shopping centre"), ("Shops", "shop"), ("Professional Offices", "office"),
    ("Restaurant", "restaurant"), ("Service Station", "service station"), ("Hotel", "hotel"), ("Club", "club"),
    ("Industr", "industry"), ("Extractive", "quarry"), ("Educational", "school"), ("Child Care", "childcare"),
    ("Religious", "church"), ("Hospitals", "hospital or care home"), ("Parks", "park"), ("Vacant", "vacant land"),
    ("Reservoir", "utility"), ("Telco", "utility"), ("Cemeteries", "cemetery"), ("Community Protection", "emergency services"),
    ("Nurseries", "nursery"),
]
BCC_DELIVER = {"LDR", "LMR", "MDR", "HDR", "CR", "EC", "RU", "RR", "EM", "TA"}
BCC_CHECK = {"MU"}
BCC_SKIP = {"NC": "shops", "DC": "shops", "MC": "shops", "PC": "shops", "CN": "shops", "SC": "shops", "OS": "park",
            "SR": "sport", "SP": "utility", "IN": "industry", "LII": "industry", "GI": "industry", "HI": "industry",
            "IN1": "industry", "IN2": "industry", "SI": "industry", "IPA": "industry"}
BCC_COMMUNITY = [("Cemetery", "cemetery"), ("Emergency", "emergency services"), ("Health", "health care"),
                 ("Education", "school"), ("Worship", "church")]
OUT = Path(__file__).resolve().parents[2] / "App" / "wwwroot" / "data" / "outreach.json"

ABBREVIATIONS = {"Street": "St", "Road": "Rd", "Court": "Ct", "Drive": "Dr", "Crescent": "Cres", "Place": "Pl",
                 "Avenue": "Ave", "Close": "Cl", "Parade": "Pde", "Terrace": "Tce", "Circuit": "Cct",
                 "Boulevard": "Bvd", "Lane": "Ln", "Way": "Way", "Grove": "Gr", "Highway": "Hwy"}


def fetch(url, data=None, attempts=4):
    for attempt in range(attempts):
        try:
            request = urllib.request.Request(url, data=data, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(request, timeout=120) as response:
                return json.load(response)
        except (urllib.error.HTTPError, urllib.error.URLError, TimeoutError):
            if attempt == attempts - 1:
                raise
            time.sleep(10 * (attempt + 1))


def fetch_streets():
    box = f"{SOUTH - STREET_MARGIN},{WEST - STREET_MARGIN},{NORTH + STREET_MARGIN},{EAST + STREET_MARGIN}"
    roads = "|".join(sorted(ROADS))
    query = f'[out:json][timeout:90];way["highway"~"^({roads})$"]({box});(._;>;);out body;'
    return fetch(OVERPASS, urllib.parse.urlencode({"data": query}).encode())


def fetch_addresses():
    features, offset = [], 0
    while True:
        params = {
            "geometry": f"{WEST},{SOUTH},{EAST},{NORTH}", "geometryType": "esriGeometryEnvelope", "inSR": 4326,
            "spatialRel": "esriSpatialRelIntersects", "where": "1=1",
            "outFields": "unit_number,street_full,lotplan,local_authority",
            "returnGeometry": "true", "outSR": 4326, "orderByFields": "objectid", "resultOffset": offset,
            "resultRecordCount": 2000, "f": "json",
        }
        page = fetch(f"{ADDRESSES}?{urllib.parse.urlencode(params)}")
        features += page["features"]
        if not page.get("exceededTransferLimit"):
            return features
        offset += len(page["features"])


def fetch_polygons(url, fields, page_size):
    features, offset = [], 0
    while True:
        params = {
            "geometry": f"{WEST},{SOUTH},{EAST},{NORTH}", "geometryType": "esriGeometryEnvelope", "inSR": 4326,
            "spatialRel": "esriSpatialRelIntersects", "where": "1=1", "outFields": fields, "returnGeometry": "true",
            "outSR": 4326, "orderByFields": "OBJECTID", "resultOffset": offset, "resultRecordCount": page_size,
            "f": "json",
        }
        page = fetch(f"{url}?{urllib.parse.urlencode(params)}")
        features += page.get("features", [])
        if not page.get("exceededTransferLimit") or not page.get("features"):
            return features
        offset += len(page["features"])


class PolygonIndex:
    def __init__(self, features):
        self.grid = defaultdict(list)
        self.items = []
        for feature in features:
            rings = (feature.get("geometry") or {}).get("rings") or []
            if not rings:
                continue
            xs = [p[0] for ring in rings for p in ring]
            ys = [p[1] for ring in rings for p in ring]
            box = (min(xs), min(ys), max(xs), max(ys))
            index = len(self.items)
            self.items.append((box, rings, feature["attributes"]))
            for gx in range(int(box[0] / GRID_DEGREES), int(box[2] / GRID_DEGREES) + 1):
                for gy in range(int(box[1] / GRID_DEGREES), int(box[3] / GRID_DEGREES) + 1):
                    self.grid[(gx, gy)].append(index)

    def find(self, lon, lat):
        for index in self.grid.get((int(lon / GRID_DEGREES), int(lat / GRID_DEGREES)), []):
            box, rings, attributes = self.items[index]
            if box[0] <= lon <= box[2] and box[1] <= lat <= box[3] and inside(rings, lon, lat):
                return attributes
        return None


def inside(rings, x, y):
    hit = False
    for ring in rings:
        for (x1, y1), (x2, y2) in zip(ring, ring[1:] + ring[:1]):
            if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1) + x1:
                hit = not hit
    return hit


def classify_mbrc(landuse):
    use = (landuse or "").strip()
    if use in MBRC_DELIVER:
        return "deliver", None
    if use in MBRC_CHECK:
        return "check", "check: " + (use.lower() or "no land use record")
    for key, label in MBRC_SKIP:
        if key.lower() in use.lower():
            return "skip", label
    return "check", "check: " + use.lower()


def classify_bcc(attributes):
    code = (attributes.get("ZONE_CODE") or "").strip()
    description = attributes.get("ZONE_PREC_DESC") or ""
    if code in BCC_DELIVER:
        return "deliver", None
    if code in BCC_CHECK:
        return "check", "check: mixed use"
    if code == "CF":
        return "skip", next((label for key, label in BCC_COMMUNITY if key in description), "community facility")
    return "skip", BCC_SKIP.get(code, description.split(" - ")[-1].lower() or "not homes")


class Classifier:
    def __init__(self, mbrc_features, bcc_features):
        self.mbrc_by_lot = {}
        for feature in mbrc_features:
            lot = (feature["attributes"].get("LOT_PLAN") or "").strip()
            if lot:
                self.mbrc_by_lot[lot] = feature["attributes"]
        self.mbrc_shapes = PolygonIndex(mbrc_features)
        self.bcc_shapes = PolygonIndex(bcc_features)
        self.stats = defaultdict(lambda: defaultdict(int))

    def classify(self, attributes, lon, lat):
        council = attributes.get("local_authority") or ""
        if council.startswith("Brisbane"):
            zone = self.bcc_shapes.find(lon, lat)
            self.stats["Brisbane"]["in a zone" if zone else "no zone"] += 1
            result = classify_bcc(zone) if zone else ("check", "check: no zone")
        else:
            lot = self.mbrc_by_lot.get((attributes.get("lotplan") or "").strip())
            how = "by lot" if lot else None
            if lot is None:
                lot = self.mbrc_shapes.find(lon, lat)
                how = "by lot polygon" if lot else "unmatched"
            self.stats["Moreton Bay"][how] += 1
            result = classify_mbrc(lot["LanduseRates"]) if lot else ("check", "check: no land use record")
        name = "Brisbane" if council.startswith("Brisbane") else "Moreton Bay"
        self.stats[name][result[0]] += 1
        return result


def local_xy(lat, lon):
    lat0 = math.radians((SOUTH + NORTH) / 2)
    return math.radians(lon) * EARTH_M * math.cos(lat0), math.radians(lat) * EARTH_M


def nearest_on_segment(p, a, b):
    ax, ay = b[0] - a[0], b[1] - a[1]
    t = max(0.0, min(1.0, ((p[0] - a[0]) * ax + (p[1] - a[1]) * ay) / ((ax * ax + ay * ay) or 1)))
    q = (a[0] + t * ax, a[1] + t * ay)
    return math.dist(p, q), ax * (p[1] - a[1]) - ay * (p[0] - a[0]), t


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
                    "boxes": [0, 0], "units": [0, 0], "at": [[], []],
                })
                segment = [node]
    return edges


def count_letterboxes(edges, features, classifier):
    by_name = defaultdict(list)
    for i, edge in enumerate(edges):
        by_name[edge["name"].lower()].append(i)
    placed = unplaced = 0
    marks = {"skips": {}, "checks": {}}
    for feature in features:
        attributes = feature["attributes"]
        p = local_xy(feature["geometry"]["y"], feature["geometry"]["x"])
        best = None
        for i in by_name.get((attributes["street_full"] or "").lower(), []):
            xy = edges[i]["xy"]
            walked = 0.0
            for j in range(len(xy) - 1):
                distance, cross, t = nearest_on_segment(p, xy[j], xy[j + 1])
                step = math.dist(xy[j], xy[j + 1])
                if best is None or distance < best[0]:
                    best = (distance, i, 0 if cross > 0 else 1, walked + t * step)
                walked += step
        if best is None or best[0] > MAX_SETBACK_M:
            unplaced += 1
            continue
        _, i, side, along = best
        lon, lat = feature["geometry"]["x"], feature["geometry"]["y"]
        kind, label = classifier.classify(attributes, lon, lat)
        if kind != "skip":
            edges[i]["boxes"][side] += 1
            edges[i]["at"][side].append(round(1000 * along / (edges[i]["length"] or 1)))
            if attributes["unit_number"]:
                edges[i]["units"][side] += 1
        if label is not None:
            bucket = marks["skips" if kind == "skip" else "checks"]
            key = (i, side, label, round(lon / SKIP_MERGE_DEGREES), round(lat / SKIP_MERGE_DEGREES))
            bucket.setdefault(key, [round(lon, 5), round(lat, 5), side, i, label])
        placed += 1
    return placed, unplaced, {k: list(v.values()) for k, v in marks.items()}


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


def write(edges, areas, routes, placed, unplaced, marks):
    keep = list(range(len(edges)))
    index = {old: new for new, old in enumerate(keep)}
    nodes = sorted({n for i in keep for n in (edges[i]["u"], edges[i]["v"])})
    node_index = {n: k for k, n in enumerate(nodes)}
    rng = random.Random(31)
    data = {
        "attribution": "Streets © OpenStreetMap contributors (ODbL). Addresses © State of Queensland (CC BY 4.0). "
                       "Land use © MBRC 2020 (CC BY 4.0). Zoning © Brisbane City Council (CC BY 4.0).",
        "church": list(CHURCH),
        "bounds": [SOUTH, WEST, NORTH, EAST],
        "letterboxes": {"placed": sum(sum(e["boxes"]) for e in edges), "unplaced": unplaced,
                        "skipped": placed - sum(sum(e["boxes"]) for e in edges)},
        "edges": [
            {
                "u": node_index[edges[i]["u"]], "v": node_index[edges[i]["v"]],
                "name": short_name(edges[i]["name"]), "road": edges[i]["road"],
                "length": round(edges[i]["length"]), "boxes": edges[i]["boxes"], "units": edges[i]["units"],
                "at": [sorted(side) for side in edges[i]["at"]],
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
        "skips": [{"lon": lon, "lat": lat, "side": side, "edge": index[i], "label": label}
                  for lon, lat, side, i, label in marks["skips"]],
        "checks": [{"lon": lon, "lat": lat, "side": side, "edge": index[i], "label": label}
                   for lon, lat, side, i, label in marks["checks"]],
    }
    OUT.write_text(json.dumps(data, separators=(",", ":")))
    return len(keep), OUT.stat().st_size


def main():
    edges = build_edges(fetch_streets())
    classifier = Classifier(fetch_polygons(MBRC_LOTS, "LOT_PLAN,LanduseRates", 1000),
                            fetch_polygons(BCC_ZONES, "ZONE_CODE,ZONE_PREC_DESC", 2000))
    placed, unplaced, marks = count_letterboxes(edges, fetch_addresses(), classifier)
    areas = cut_areas(edges)
    routes = outreach_routes(edges, areas)
    kept, size = write(edges, areas, routes, placed, unplaced, marks)
    homes = sum(sum(e["boxes"]) for e in edges)
    print(f"{kept} street segments, {placed} addresses placed ({homes} letterboxes, {placed - homes} skipped), "
          f"{unplaced} unplaced, {len(marks['skips'])} skip points, {len(marks['checks'])} check points, "
          f"{len(areas)} areas, {len(routes)} outreach routes, {size // 1024} KB", file=sys.stderr)
    for council, counts in classifier.stats.items():
        total = counts["deliver"] + counts["skip"] + counts["check"]
        matched = {k: v for k, v in counts.items() if k not in ("deliver", "skip", "check")}
        rates = ", ".join(f"{k} {v} ({v / total:.0%})" for k, v in matched.items())
        print(f"{council}: {total} placed addresses; {rates}; deliver {counts['deliver']}, "
              f"skip {counts['skip']}, check {counts['check']}", file=sys.stderr)


if __name__ == "__main__":
    main()
