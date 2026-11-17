import * as maplibregl from "./vendor/maplibre-gl.mjs";
import { FetchSource, PMTiles, Protocol } from "./vendor/pmtiles.mjs";
import { mapStyle } from "./map-style.js";
import maplibreCss from "./vendor/maplibre-gl.css.mjs";

maplibregl.setWorkerUrl(new URL("./vendor/maplibre-gl-worker.mjs", import.meta.url).href);

const protocol = new Protocol();
maplibregl.addProtocol("pmtiles", protocol.tile);

const glyphs = new URL("./glyphs/", import.meta.url).href + "{fontstack}/{range}.pbf";
const empty = { type: "FeatureCollection", features: [] };
const byZoom = (stops) => ["interpolate", ["exponential", 1.6], ["zoom"], ...stops.flat()];
const styleIs = (...styles) => ["in", ["get", "style"], ["literal", styles]];
const kindIs = (...kinds) => ["in", ["get", "kind"], ["literal", kinds]];
const keptSources = new Map();
function loadStylesheet() {
  if (document.getElementById("maplibre-gl-css")) return;
  const style = document.createElement("style");
  style.id = "maplibre-gl-css";
  style.textContent = maplibreCss;
  document.head.append(style);
}

const scheme = () => (document.documentElement.dataset.scheme === "dark" ? "dark" : "light");
const token = (name) => getComputedStyle(document.documentElement).getPropertyValue(name).trim();

function colours() {
  return {
    walk: token("--accent"),
    walked: token("--ink-3"),
    you: token("--marker"),
    marker: token("--marker"),
    onMarker: token("--on-marker"),
    ink: token("--ink"),
    surface: token("--surface"),
    danger: token("--danger"),
    primary: token("--primary"),
    land: token("--map-land"),
    veil: token("--map-veil"),
  };
}

const keptCheckEvery = 2000;

function keptOrRemote(source, kept) {
  const remote = source.url ? new FetchSource(source.url) : null;
  let whole = false;
  let checkedAt = 0;
  const keptIsWhole = async () => {
    if (whole || Date.now() - checkedAt < keptCheckEvery) return whole;
    checkedAt = Date.now();
    const files = JSON.parse(await kept.list());
    whole = files.some((file) => file.id === source.id && file.url === source.url && file.state === "done");
    return whole;
  };
  return async (offset, length, signal) => {
    if (remote && !(await keptIsWhole())) return remote.getBytes(offset, length, signal);
    try {
      return { data: (await kept.readKept(source.id, offset, length)).buffer };
    } catch (error) {
      if (!remote) throw error;
      whole = false;
      return remote.getBytes(offset, length, signal);
    }
  };
}

async function tilesFor(source) {
  if (source.kind === "urls") return source.urls.map((url) => "pmtiles://" + url);
  if (source.kind !== "kept") return "pmtiles://" + source.url;
  const key = source.url ? `${source.id}/${source.url.split("/").pop()}` : source.id;
  if (!keptSources.has(key)) {
    const kept = await import("../../GSBC.ChurchApp.Platform.Client/kept-files.js");
    keptSources.set(key, true);
    protocol.add(new PMTiles({ getKey: () => key, getBytes: keptOrRemote(source, kept) }));
  }
  return "pmtiles://" + key;
}

const coneRadius = { sure: 104, unsure: 86 };
const coneSpread = { sure: 50, unsure: 110 };
const sureWithin = 20;
const unsureBeyond = 60;
const movingMetres = 6;
const movingFor = 10000;

function headingFrom(compass, course, now) {
  if (course && now - course.at < movingFor) return { heading: course.heading, spread: coneSpread.sure, sureness: 1 };
  if (!compass || compass.accuracy < 0 || compass.accuracy > unsureBeyond) return null;
  const doubt = Math.min(1, Math.max(0, (compass.accuracy - sureWithin) / (unsureBeyond - sureWithin)));
  const step = 10;
  const spread = Math.round((coneSpread.sure + doubt * (coneSpread.unsure - coneSpread.sure)) / step) * step;
  return { heading: compass.heading, spread, sureness: 1 - doubt * 0.45 };
}

function coneImage(colour, spread) {
  const ratio = 2;
  const doubt = (spread - coneSpread.sure) / (coneSpread.unsure - coneSpread.sure);
  const radius = coneRadius.sure + doubt * (coneRadius.unsure - coneRadius.sure);
  const size = Math.round(coneRadius.sure * 2 * ratio);
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = size;
  const g = canvas.getContext("2d");
  const centre = size / 2;
  g.fillStyle = colour;
  const [r, gr, b] = g.fillStyle.match(/[0-9a-f]{2}/gi).map((hex) => parseInt(hex, 16));
  const fade = g.createRadialGradient(centre, centre, 0, centre, centre, radius * ratio);
  fade.addColorStop(0, colour);
  fade.addColorStop(1, `rgba(${r}, ${gr}, ${b}, 0)`);
  const half = (spread / 2) * (Math.PI / 180);
  g.globalAlpha = 0.9;
  g.fillStyle = fade;
  g.beginPath();
  g.moveTo(centre, centre);
  g.arc(centre, centre, radius * ratio, -Math.PI / 2 - half, -Math.PI / 2 + half);
  g.closePath();
  g.fill();
  return { image: g.getImageData(0, 0, size, size), ratio };
}

function bearing(from, to) {
  const rad = Math.PI / 180;
  const [lon1, lat1, lon2, lat2] = [from[0] * rad, from[1] * rad, to[0] * rad, to[1] * rad];
  const y = Math.sin(lon2 - lon1) * Math.cos(lat2);
  const x = Math.cos(lat1) * Math.sin(lat2) - Math.sin(lat1) * Math.cos(lat2) * Math.cos(lon2 - lon1);
  return ((Math.atan2(y, x) / rad) + 360) % 360;
}

function metresBetween(a, b) {
  const rad = Math.PI / 180;
  const x = (b[0] - a[0]) * rad * Math.cos(((a[1] + b[1]) / 2) * rad);
  const y = (b[1] - a[1]) * rad;
  return Math.sqrt(x * x + y * y) * 6371008.8;
}

function overlayLayers(c) {
  return [
    { id: "you-heading", type: "symbol", source: "you", filter: ["has", "cone"],
      layout: { "icon-image": ["get", "cone"], "icon-rotate": ["get", "heading"], "icon-rotation-alignment": "map", "icon-allow-overlap": true, "icon-ignore-placement": true },
      paint: { "icon-opacity": ["get", "sureness"] } },
    { id: "areas-fill", type: "fill", source: "areas", filter: ["!", ["get", "hatched"]], paint: { "fill-color": ["coalesce", ["get", "colour"], c.walk], "fill-opacity": ["case", ["get", "outline"], 0, 0.12] } },
    { id: "areas-hatch", type: "fill", source: "areas", filter: ["get", "hatched"], paint: { "fill-pattern": overlapHatch } },
    { id: "areas-edge", type: "line", source: "areas", filter: ["all", ["!", ["get", "dashed"]], ["!", ["get", "hatched"]]], paint: { "line-color": ["coalesce", ["get", "colour"], c.walk], "line-width": ["case", ["get", "outline"], 2.5, 1.5] } },
    { id: "areas-edge-dashed", type: "line", source: "areas", filter: ["get", "dashed"], paint: { "line-color": ["coalesce", ["get", "colour"], c.walk], "line-width": 2, "line-dasharray": [3, 2] } },
    { id: "chosen-edge", type: "line", source: "lines", filter: styleIs("chosen"), layout: { "line-cap": "round", "line-join": "round" },
      paint: { "line-color": c.ink, "line-width": byZoom([[12, 8], [15, 15], [18, 36]]) } },
    { id: "chosen", type: "line", source: "lines", filter: styleIs("chosen"), layout: { "line-cap": "round", "line-join": "round" },
      paint: { "line-color": c.surface, "line-width": byZoom([[12, 4], [15, 9], [18, 26]]) } },
    { id: "covered", type: "line", source: "lines", filter: styleIs("covered"), layout: { "line-cap": "round", "line-join": "round" },
      paint: { "line-color": ["interpolate-hcl", ["linear"], ["get", "age"], 0, c.walk, 1, c.land], "line-width": byZoom([[12, 1.5], [15, 3.5], [18, 9]]) } },
    { id: "walked", type: "line", source: "lines", filter: styleIs("walked"), layout: { "line-cap": "round", "line-join": "round" },
      paint: { "line-color": c.walked, "line-opacity": 0.45, "line-width": byZoom([[13, 2], [16, 5], [18, 12]]) } },
    { id: "walk-edge", type: "line", source: "lines", filter: styleIs("walk", "walk-done", "route"), layout: { "line-cap": "round", "line-join": "round" },
      paint: { "line-color": c.surface, "line-width": byZoom([[13, 4], [16, 9], [18, 18]]) } },
    { id: "walk-done", type: "line", source: "lines", filter: styleIs("walk-done"), layout: { "line-cap": "round", "line-join": "round" },
      paint: { "line-color": c.walked, "line-width": byZoom([[13, 2.5], [16, 6], [18, 13]]) } },
    { id: "walk", type: "line", source: "lines", filter: styleIs("walk"), layout: { "line-cap": "round", "line-join": "round" },
      paint: { "line-color": c.walk, "line-width": byZoom([[13, 2.5], [16, 6], [18, 13]]) } },
    { id: "route", type: "line", source: "lines", filter: styleIs("route"), layout: { "line-cap": "round", "line-join": "round" },
      paint: { "line-color": ["coalesce", ["get", "colour"], c.walk], "line-width": byZoom([[13, 2.5], [16, 6], [18, 13]]) } },
    { id: "connector", type: "line", source: "lines", filter: styleIs("connector"), layout: { "line-cap": "butt", "line-join": "round" },
      paint: { "line-color": c.walk, "line-width": byZoom([[13, 1.5], [16, 3], [18, 6]]), "line-dasharray": [2, 1.6] } },
    { id: "long-walk", type: "line", source: "lines", filter: styleIs("long"), layout: { "line-cap": "round", "line-join": "round" },
      paint: { "line-color": c.walk, "line-width": byZoom([[13, 2], [16, 4], [18, 7]]), "line-dasharray": [0.1, 2] } },
    { id: "walk-arrows", type: "symbol", source: "lines", filter: styleIs("walk"), minzoom: 15,
      layout: { "symbol-placement": "line", "symbol-spacing": 90, "text-field": "›", "text-font": ["montserrat-semibold"],
        "text-size": byZoom([[15, 14], [18, 22]]), "text-keep-upright": false, "text-allow-overlap": true, "text-rotation-alignment": "map" },
      paint: { "text-color": c.surface } },
    { id: "line-labels", type: "symbol", source: "lines", filter: ["has", "label"], minzoom: 14,
      layout: { "symbol-placement": "line-center", "text-field": ["get", "label"], "text-font": ["montserrat-semibold"], "text-size": 12 },
      paint: { "text-color": c.ink, "text-halo-color": c.surface, "text-halo-width": 2 } },
    { id: "markers", type: "circle", source: "markers", filter: kindIs("nojunkmail", "dontletterbox", "skip", "check"), minzoom: 15,
      paint: {
        "circle-radius": byZoom([[15, 3], [18, 7]]),
        "circle-color": ["match", ["get", "kind"], "dontletterbox", c.danger, "nojunkmail", c.ink, c.walked],
        "circle-stroke-color": c.surface, "circle-stroke-width": 1.5,
      } },
    { id: "marker-labels", type: "symbol", source: "markers", filter: ["all", kindIs("nojunkmail", "dontletterbox"), ["has", "label"]], minzoom: 17,
      layout: { "text-field": ["get", "label"], "text-font": ["montserrat-semibold"], "text-size": 11, "text-offset": [0, 1.1], "text-anchor": "top" },
      paint: { "text-color": c.ink, "text-halo-color": c.surface, "text-halo-width": 2 } },
    { id: "reports", type: "circle", source: "markers", filter: kindIs("report"),
      paint: { "circle-radius": byZoom([[12, 7], [17, 11]]), "circle-color": c.marker, "circle-stroke-color": c.onMarker, "circle-stroke-width": ["case", ["get", "chosen"], 3, 1.5] } },
    { id: "report-marks", type: "symbol", source: "markers", filter: kindIs("report"),
      layout: { "text-field": "!", "text-font": ["montserrat-semibold"], "text-size": byZoom([[12, 11], [17, 15]]), "text-allow-overlap": true, "text-ignore-placement": true },
      paint: { "text-color": c.onMarker } },
    { id: "marker-notes", type: "symbol", source: "markers", filter: ["all", kindIs("note"), shownNow], layout: noteLayout, paint: notePaint(c) },
    { id: "places", type: "circle", source: "markers", filter: kindIs("start", "pin", "meeting"),
      paint: {
        "circle-radius": 8,
        "circle-color": ["match", ["get", "kind"], "start", c.walk, c.primary],
        "circle-stroke-color": c.surface, "circle-stroke-width": 3,
      } },
    { id: "place-labels", type: "symbol", source: "markers", filter: ["all", kindIs("start", "pin", "meeting"), ["has", "label"]],
      layout: { "text-field": ["get", "label"], "text-font": ["montserrat-semibold"], "text-size": 12, "text-offset": [0, 1.3], "text-anchor": "top" },
      paint: { "text-color": c.ink, "text-halo-color": c.surface, "text-halo-width": 2 } },
    { id: "you-accuracy", type: "circle", source: "you",
      paint: { "circle-radius": ["get", "radius"], "circle-color": c.you, "circle-opacity": 0.16, "circle-pitch-alignment": "map" } },
    { id: "you", type: "circle", source: "you",
      paint: { "circle-radius": 8, "circle-color": c.you, "circle-stroke-color": c.surface, "circle-stroke-width": 3 } },
  ];
}

const shownNow = ["all", [">=", ["zoom"], ["coalesce", ["get", "from"], 0]], ["<", ["zoom"], ["coalesce", ["get", "until"], 99]]];
const inCorner = ["coalesce", ["get", "corner"], false];
const noteLayout = {
  "text-field": ["get", "label"], "text-font": ["montserrat-semibold"], "text-size": 11, "text-max-width": 7, "text-line-height": 1.15,
  "text-anchor": ["case", inCorner, "top-left", "center"], "text-justify": ["case", inCorner, "left", "center"], "text-padding": 6,
};
const notePaint = (c) => ({ "text-color": c.ink, "text-halo-color": c.surface, "text-halo-width": 1.5 });

const hatch = "reach-hatch";
const pinGutter = 16;
const closedHatch = "reach-closed-hatch";
const overlapHatch = "areas-overlap-hatch";
const deepestZoom = 19;
const overviewMinZoom = 3;
const regionsMinZoom = 9;
const overviewOverzoom = 2;
const overviewLayersLeftOut = ["street-names"];

function hatchImage(ground, stripe, alpha = 0.4) {
  const size = 16;
  const ratio = 2;
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = size * ratio;
  const draw = canvas.getContext("2d");
  draw.scale(ratio, ratio);
  if (ground) {
    draw.fillStyle = ground;
    draw.fillRect(0, 0, size, size);
  }
  draw.strokeStyle = stripe;
  draw.globalAlpha = alpha;
  draw.lineWidth = 1.5;
  draw.beginPath();
  for (const offset of [-size, 0, size]) {
    draw.moveTo(offset, size);
    draw.lineTo(offset + size, 0);
  }
  draw.stroke();
  return { image: draw.getImageData(0, 0, size * ratio, size * ratio), ratio };
}

function reachLayers(c, overview) {
  const beyond = overview
    ? [
        { id: "reach-beyond", type: "fill", source: "reach-beyond", paint: { "fill-color": c.veil } },
        { id: "reach-closed", type: "fill", source: "reach-closed", paint: { "fill-color": c.veil, "fill-opacity": 0.8 } },
        { id: "reach-closed-hatch", type: "fill", source: "reach-closed", paint: { "fill-pattern": closedHatch } },
      ]
    : [{ id: "reach-beyond", type: "fill", source: "reach-beyond", paint: { "fill-pattern": hatch } }];
  return [
    ...beyond,
    { id: "reach-others", type: "fill", source: "reach-others", paint: { "fill-color": ["coalesce", ["get", "colour"], c.walked], "fill-opacity": ["case", ["get", "outline"], 0, 0.1] } },
    { id: "reach-others-edge", type: "line", source: "reach-others", paint: { "line-color": ["coalesce", ["get", "colour"], c.walked], "line-width": 1.5, "line-dasharray": [3, 2] } },
    { id: "reach-edge", type: "line", source: "reach-edge", layout: { "line-cap": "round" }, paint: { "line-color": c.ink, "line-opacity": 0.55, "line-width": 1.5 } },
    { id: "reach-notes", type: "symbol", source: "reach-notes", filter: shownNow, layout: noteLayout, paint: notePaint(c) },
  ];
}

function withOverview(regions, overview) {
  regions.sources.overview = overview.sources.streets;
  const underneath = overview.layers
    .filter((layer) => !overviewLayersLeftOut.includes(layer.id))
    .map((layer) => (layer.source ? { ...layer, id: "overview-" + layer.id, source: "overview" } : layer));
  regions.layers = [...underneath, ...regions.layers.filter((layer) => layer.type !== "background")];
  return regions;
}

function withOverlays(base, data, overview) {
  const c = colours();
  const land = base.layers.find((layer) => layer.id.endsWith("land"))?.paint["fill-color"] ?? c.surface;
  base.hatch = { ground: land, stripe: c.walked };
  base.sources["reach-beyond"] = { type: "geojson", data: data.reach?.beyond ?? empty };
  base.sources["reach-closed"] = { type: "geojson", data: data.reach?.closed ?? empty };
  base.sources["reach-others"] = { type: "geojson", data: data.reach?.others ?? empty };
  base.sources["reach-edge"] = { type: "geojson", data: data.reach?.edge ?? empty };
  base.sources["reach-notes"] = { type: "geojson", data: data.reach?.notes ?? empty };
  base.layers.push(...reachLayers(c, overview));
  base.sources.lines = { type: "geojson", data: data.lines };
  base.sources.areas = { type: "geojson", data: data.areas };
  base.sources.markers = { type: "geojson", data: data.markers };
  base.sources.you = { type: "geojson", data: data.you };
  base.layers.push(...overlayLayers(c));
  return base;
}

const colourOf = (value) => (value?.startsWith("--") ? token(value) : value);
const line = (l) => ({ type: "Feature", properties: { style: l.style, ...(l.label ? { label: l.label } : {}), ...(l.colour ? { colour: colourOf(l.colour) } : {}), ...(l.age != null ? { age: l.age } : {}), id: l.id ?? "" },
  geometry: { type: "LineString", coordinates: l.points } });
let lastLineId = 0;
const numbered = (feature) => (value) => ({ ...feature(value), id: ++lastLineId });
const lineSet = (kindsJson, bytes) => {
  const kinds = JSON.parse(kindsJson);
  const values = new Float64Array(bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength));
  const lines = [];
  for (let at = 0; at < values.length; ) {
    const [kind, age, count] = [kinds[values[at]], values[at + 1], values[at + 2]];
    at += 3;
    const points = [];
    for (let i = 0; i < count; i++, at += 2) points.push([values[at], values[at + 1]]);
    lines.push({ ...kind, ...(Number.isNaN(age) ? {} : { age }), points });
  }
  return lines;
};
const area = (a) => ({ type: "Feature", properties: { ...(a.colour ? { colour: colourOf(a.colour) } : {}), dashed: !!a.dashed, outline: !!a.outline, hatched: !!a.hatched, id: a.id ?? "" }, geometry: { type: "Polygon", coordinates: [a.points, ...(a.holes ?? [])] } });
const marker = (m) => ({ type: "Feature", properties: { kind: m.kind, ...(m.label ? { label: m.label } : {}), id: m.id ?? "", chosen: !!m.chosen, corner: !!m.corner, from: m.from ?? 0, until: m.until ?? 99 }, geometry: { type: "Point", coordinates: m.point } });

function framePadding(b) {
  const padding = b.padding ?? 40;
  return { top: padding, right: padding, bottom: padding + (b.below ?? 0), left: padding + (b.start ?? 0) };
}

function metresToPixels(map, metres, lat) {
  const metresPerPixel = (40075016.686 * Math.cos((lat * Math.PI) / 180)) / Math.pow(2, map.getZoom() + 9);
  return metres / metresPerPixel;
}

export async function create(element, optionsJson, receiver) {
  loadStylesheet();
  const options = JSON.parse(optionsJson);
  const tiles = await tilesFor(options.source);
  const backdrop = options.backdrop ?? null;
  const backdropLimit = backdrop && [[backdrop.bounds.west, backdrop.bounds.south], [backdrop.bounds.east, backdrop.bounds.north]];
  const data = { lines: empty, areas: empty, markers: empty, you: empty, reach: null };
  let you = null;
  let following = !!options.follow;
  let dragged = false;
  let shownScheme = scheme();

  let hatchColours = null;
  const styleNow = () => {
    const attribution = options.source.attribution ?? "";
    const regions = mapStyle({ tiles, glyphs, scheme: shownScheme, attribution });
    const base = backdrop ? withOverview(regions, mapStyle({ tiles: "pmtiles://" + backdrop.url, glyphs, scheme: shownScheme, attribution })) : regions;
    const style = withOverlays(base, data, !!backdrop);
    hatchColours = style.hatch;
    delete style.hatch;
    return style;
  };
  const map = new maplibregl.Map({
    container: element,
    style: styleNow(),
    center: options.centre ?? [153.0, -27.35],
    zoom: options.zoom ?? 15,
    maxZoom: deepestZoom,
    minZoom: options.minZoom ?? (backdrop ? overviewMinZoom : regionsMinZoom),
    attributionControl: false,
    interactive: options.interactive !== false,
    dragRotate: false,
    pitchWithRotate: false,
    touchPitch: false,
    fadeDuration: 0,
  });
  map.touchZoomRotate.disableRotation();
  const addHatch = () => {
    if (map.hasImage(hatch)) return;
    const { image, ratio } = hatchImage(hatchColours.ground, hatchColours.stripe);
    map.addImage(hatch, image, { pixelRatio: ratio });
    const closed = hatchImage(null, hatchColours.stripe, shownScheme === "dark" ? 0.22 : 0.4);
    map.addImage(closedHatch, closed.image, { pixelRatio: closed.ratio });
    const overlap = hatchImage(null, token("--marker-ink"), 0.7);
    map.addImage(overlapHatch, overlap.image, { pixelRatio: overlap.ratio });
  };
  map.on("style.load", addHatch);
  map.on("styleimagemissing", (e) => [hatch, closedHatch, overlapHatch].includes(e.id) && addHatch());

  let covered = [];
  const insideCovered = ({ lng, lat }) => covered.some((box) => lat >= box.south && lat <= box.north && lng >= box.west && lng <= box.east);
  const freeZoom = () => {
    if (backdrop) map.setMaxZoom(deepestZoom);
  };
  const holdZoom = () => {
    if (!backdrop) return;
    const cap = insideCovered(map.getCenter()) ? deepestZoom : backdrop.maxZoom + overviewOverzoom;
    if (map.getZoom() > cap + 0.01) map.easeTo({ zoom: cap, duration: 500 });
    else map.setMaxZoom(cap);
  };

  const set = (name, value) => {
    data[name] = value;
    map.getSource(name)?.setData(value);
  };
  const lineSets = new Map();
  const showLines = (key, features) => {
    const gone = lineSets.get(key) ?? [];
    if (features) lineSets.set(key, features);
    else lineSets.delete(key);
    data.lines = { type: "FeatureCollection", features: [...lineSets.values()].flat() };
    map.getSource("lines")?.updateData({ remove: gone.map((feature) => feature.id), add: features ?? [] });
  };
  let compass = null;
  let course = null;
  let courseFrom = null;
  const coneName = (spread) => `you-cone-${spread}`;
  const addCone = (name) => {
    if (map.hasImage(name)) return;
    const { image, ratio } = coneImage(colours().you, +name.slice(coneName("").length));
    map.addImage(name, image, { pixelRatio: ratio });
  };
  map.on("styleimagemissing", (e) => e.id.startsWith(coneName("")) && addCone(e.id));
  const drawYou = () => {
    if (!you) return set("you", empty);
    const radius = Math.max(0, metresToPixels(map, you.accuracy ?? 0, you.point[1]));
    const facing = options.heading ? headingFrom(compass, course, Date.now()) : null;
    const cone = facing ? { cone: coneName(facing.spread), heading: facing.heading, sureness: facing.sureness } : {};
    if (facing && map.isStyleLoaded()) addCone(cone.cone);
    set("you", { type: "FeatureCollection", features: [{ type: "Feature", properties: { radius, ...cone }, geometry: { type: "Point", coordinates: you.point } }] });
  };
  let courseEnds = null;
  const followCourse = () => {
    if (!you) return (courseFrom = null);
    if (!courseFrom) return (courseFrom = you.point);
    if (metresBetween(courseFrom, you.point) < Math.max(movingMetres, you.accuracy ?? 0)) return;
    course = { heading: bearing(courseFrom, you.point), at: Date.now() };
    courseFrom = you.point;
    clearTimeout(courseEnds);
    courseEnds = setTimeout(() => !handle.gone && drawYou(), movingFor + 50);
  };
  let stopCompass = null;
  if (options.heading)
    import("../../GSBC.ChurchApp.Platform.Client/compass.js").then(({ watch }) => {
      if (handle.gone) return;
      stopCompass = watch((reading) => {
        compass = reading;
        drawYou();
      });
    });

  let framed = null;
  const fit = (b, animate = false) => {
    freeZoom();
    map.fitBounds([[b.west, b.south], [b.east, b.north]], { padding: framePadding(b), duration: animate ? 600 : 0, maxZoom: b.maxZoom ?? 17 });
  };
  const showYou = (animate = false) => {
    freeZoom();
    if (framed) fit(framed, animate);
    else map.easeTo({ center: you.point, zoom: Math.max(map.getZoom(), 17), duration: animate ? 600 : 0 });
  };
  const moved = () => {
    const b = map.getBounds();
    receiver?.invokeMethodAsync("OnMoved", b.getSouth(), b.getWest(), b.getNorth(), b.getEast());
  };
  map.on("moveend", moved);
  map.on("moveend", holdZoom);
  map.on("load", () => {
    Object.entries(data).filter(([name]) => name !== "reach").forEach(([name, value]) => map.getSource(name)?.setData(value));
    showReach();
    moved();
    if (framed) fit(framed);
    else if (following && you) showYou();
  });
  map.on("zoomend", drawYou);
  map.on("click", (e) => receiver?.invokeMethodAsync("OnTap", e.lngLat.lat, e.lngLat.lng));
  map.on("dragstart", () => {
    dragged = true;
    if (!following) return;
    following = false;
    receiver?.invokeMethodAsync("OnStoppedFollowing");
  });

  const watcher = new MutationObserver(() => {
    if (scheme() === shownScheme) return;
    shownScheme = scheme();
    if (map.hasImage(hatch)) map.removeImage(hatch);
    if (map.hasImage(closedHatch)) map.removeImage(closedHatch);
    if (map.hasImage(overlapHatch)) map.removeImage(overlapHatch);
    map.setStyle(styleNow());
  });
  watcher.observe(document.documentElement, { attributes: true, attributeFilter: ["data-scheme", "data-theme"] });

  const ready = new Promise((resolve) => map.once("load", resolve));

  const figure = element.parentElement;
  const placePins = () => {
    const width = figure.clientWidth;
    for (const pin of figure.querySelectorAll("[data-map-pin]")) {
      const [lat, lon] = pin.dataset.mapPin.split(",").map(Number);
      const at = map.project([lon, lat]);
      const half = (pin.firstElementChild?.offsetWidth ?? 0) / 2;
      const start = parseFloat(getComputedStyle(pin).getPropertyValue("--map-pin-start")) || pinGutter;
      const x = Math.max(start + half, Math.min(width - pinGutter - half, at.x));
      pin.style.transform = `translate(${Math.round(x)}px, ${Math.round(at.y)}px)`;
    }
  };
  map.on("move", placePins);
  map.on("resize", placePins);
  const pinsChanged = () => {
    placePins();
    map.fire("pinschanged");
  };
  map.once("load", pinsChanged);
  const pins = new MutationObserver(pinsChanged);
  pins.observe(figure, { childList: true, subtree: true, attributes: true, attributeFilter: ["data-map-pin"] });

  let limit = null;
  const showReach = () => {
    ["beyond", "closed", "others", "edge", "notes"].forEach((name) => map.getSource("reach-" + name)?.setData(data.reach?.[name] ?? empty));
    map.setMaxBounds(limit ?? backdropLimit);
    holdZoom();
  };
  const setReach = (json) => {
    const reach = json ? JSON.parse(json) : null;
    data.reach = reach && {
      beyond: { type: "FeatureCollection", features: [{ type: "Feature", properties: {}, geometry: { type: "MultiPolygon", coordinates: reach.beyond } }] },
      closed: reach.closed.length ? { type: "FeatureCollection", features: [{ type: "Feature", properties: {}, geometry: { type: "Polygon", coordinates: reach.closed } }] } : empty,
      edge: { type: "FeatureCollection", features: reach.edge.map((points) => ({ type: "Feature", properties: {}, geometry: { type: "LineString", coordinates: points } })) },
      others: { type: "FeatureCollection", features: reach.others.map(area) },
      notes: { type: "FeatureCollection", features: reach.notes.map(marker) },
    };
    covered = reach?.covered ?? [];
    limit = reach?.limit ? [[reach.limit.west, reach.limit.south], [reach.limit.east, reach.limit.north]] : null;
    if (map.isStyleLoaded()) showReach();
  };

  const handle = {
    map,
    gone: false,
    setReach,
    setLines: (json) => showLines(null, JSON.parse(json).map(numbered(line))),
    setLineSet: (key, kinds, points) => showLines(key, kinds == null ? null : lineSet(kinds, points).map(numbered(line))),
    setAreas: (json) => set("areas", { type: "FeatureCollection", features: JSON.parse(json).map(area) }),
    setMarkers: (json) => set("markers", { type: "FeatureCollection", features: JSON.parse(json).map(marker) }),
    setYou: (json) => {
      you = json ? JSON.parse(json) : null;
      followCourse();
      drawYou();
      if (following && you && !framed) showYou(true);
    },
    follow: (on) => {
      following = on;
      if (on) dragged = false;
      if (on && you) showYou(true);
    },
    fit: (json) => {
      framed = JSON.parse(json);
      map.resize();
      if (following || !dragged) fit(framed, map.loaded());
    },
    centre: (lat, lon, zoom) => {
      freeZoom();
      map.jumpTo({ center: [lon, lat], zoom: zoom ?? map.getZoom() });
    },
    resize: () => map.resize(),
    ready: () => ready,
    dispose: () => {
      handle.gone = true;
      clearTimeout(courseEnds);
      stopCompass?.();
      watcher.disconnect();
      pins.disconnect();
      map.remove();
    },
  };
  return handle;
}
