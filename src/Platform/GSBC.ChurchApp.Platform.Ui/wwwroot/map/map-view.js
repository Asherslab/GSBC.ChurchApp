import * as maplibregl from "./vendor/maplibre-gl.mjs";
import { PMTiles, Protocol } from "./vendor/pmtiles.mjs";
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
    ink: token("--ink"),
    surface: token("--surface"),
    danger: token("--danger"),
    primary: token("--primary"),
    land: token("--map-land"),
  };
}

async function tilesFor(source) {
  if (source.kind === "urls") return source.urls.map((url) => "pmtiles://" + url);
  if (source.kind !== "kept") return "pmtiles://" + source.url;
  if (!keptSources.has(source.id)) {
    const { readKept } = await import("../../GSBC.ChurchApp.Platform.Client/kept-files.js");
    keptSources.set(source.id, true);
    protocol.add(new PMTiles({
      getKey: () => source.id,
      getBytes: async (offset, length) => ({ data: (await readKept(source.id, offset, length)).buffer }),
    }));
  }
  return "pmtiles://" + source.id;
}

function overlayLayers(c) {
  return [
    { id: "areas-fill", type: "fill", source: "areas", paint: { "fill-color": ["coalesce", ["get", "colour"], c.walk], "fill-opacity": ["case", ["get", "outline"], 0, 0.12] } },
    { id: "areas-edge", type: "line", source: "areas", filter: ["!", ["get", "dashed"]], paint: { "line-color": ["coalesce", ["get", "colour"], c.walk], "line-width": 1.5 } },
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

const hatch = "reach-hatch";

function hatchImage(ground, stripe) {
  const size = 16;
  const ratio = 2;
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = size * ratio;
  const draw = canvas.getContext("2d");
  draw.scale(ratio, ratio);
  draw.fillStyle = ground;
  draw.fillRect(0, 0, size, size);
  draw.strokeStyle = stripe;
  draw.globalAlpha = 0.4;
  draw.lineWidth = 1.5;
  draw.beginPath();
  for (const offset of [-size, 0, size]) {
    draw.moveTo(offset, size);
    draw.lineTo(offset + size, 0);
  }
  draw.stroke();
  return { image: draw.getImageData(0, 0, size * ratio, size * ratio), ratio };
}

function reachLayers(c) {
  return [
    { id: "reach-beyond", type: "fill", source: "reach-beyond", paint: { "fill-pattern": hatch } },
    { id: "reach-others", type: "fill", source: "reach-others", paint: { "fill-color": ["coalesce", ["get", "colour"], c.walked], "fill-opacity": ["case", ["get", "outline"], 0, 0.1] } },
    { id: "reach-others-edge", type: "line", source: "reach-others", paint: { "line-color": ["coalesce", ["get", "colour"], c.walked], "line-width": 1.5, "line-dasharray": [3, 2] } },
    { id: "reach-edge", type: "line", source: "reach-edge", layout: { "line-cap": "round" }, paint: { "line-color": c.ink, "line-opacity": 0.55, "line-width": 1.5 } },
    { id: "reach-notes", type: "symbol", source: "reach-notes",
      layout: { "text-field": ["get", "label"], "text-font": ["montserrat-semibold"], "text-size": 12, "text-max-width": 9 },
      paint: { "text-color": c.ink, "text-halo-color": c.surface, "text-halo-width": 2 } },
  ];
}

function withOverlays(base, data) {
  const c = colours();
  const land = base.layers.find((layer) => layer.id === "land")?.paint["fill-color"] ?? c.surface;
  base.hatch = { ground: land, stripe: c.walked };
  base.sources["reach-beyond"] = { type: "geojson", data: data.reach?.beyond ?? empty };
  base.sources["reach-others"] = { type: "geojson", data: data.reach?.others ?? empty };
  base.sources["reach-edge"] = { type: "geojson", data: data.reach?.edge ?? empty };
  base.sources["reach-notes"] = { type: "geojson", data: data.reach?.notes ?? empty };
  base.layers.push(...reachLayers(c));
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
const area = (a) => ({ type: "Feature", properties: { ...(a.colour ? { colour: colourOf(a.colour) } : {}), dashed: !!a.dashed, outline: !!a.outline, id: a.id ?? "" }, geometry: { type: "Polygon", coordinates: [a.points] } });
const marker = (m) => ({ type: "Feature", properties: { kind: m.kind, ...(m.label ? { label: m.label } : {}), id: m.id ?? "" }, geometry: { type: "Point", coordinates: m.point } });

function framePadding(b) {
  const padding = b.padding ?? 40;
  return { top: padding, right: padding, bottom: padding, left: padding + (b.start ?? 0) };
}

function metresToPixels(map, metres, lat) {
  const metresPerPixel = (40075016.686 * Math.cos((lat * Math.PI) / 180)) / Math.pow(2, map.getZoom() + 9);
  return metres / metresPerPixel;
}

export async function create(element, optionsJson, receiver) {
  loadStylesheet();
  const options = JSON.parse(optionsJson);
  const tiles = await tilesFor(options.source);
  const data = { lines: empty, areas: empty, markers: empty, you: empty, reach: null };
  let you = null;
  let following = !!options.follow;
  let dragged = false;
  let shownScheme = scheme();

  let hatchColours = null;
  const styleNow = () => {
    const style = withOverlays(mapStyle({ tiles, glyphs, scheme: shownScheme, attribution: options.source.attribution ?? "" }), data);
    hatchColours = style.hatch;
    delete style.hatch;
    return style;
  };
  const map = new maplibregl.Map({
    container: element,
    style: styleNow(),
    center: options.centre ?? [153.0, -27.35],
    zoom: options.zoom ?? 15,
    maxZoom: 19,
    minZoom: 9,
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
  };
  map.on("style.load", addHatch);
  map.on("styleimagemissing", (e) => e.id === hatch && addHatch());

  const set = (name, value) => {
    data[name] = value;
    map.getSource(name)?.setData(value);
  };
  const drawYou = () => {
    if (!you) return set("you", empty);
    const radius = Math.max(0, metresToPixels(map, you.accuracy ?? 0, you.point[1]));
    set("you", { type: "FeatureCollection", features: [{ type: "Feature", properties: { radius }, geometry: { type: "Point", coordinates: you.point } }] });
  };

  let framed = null;
  const fit = (b, animate = false) =>
    map.fitBounds([[b.west, b.south], [b.east, b.north]], { padding: framePadding(b), duration: animate ? 600 : 0, maxZoom: b.maxZoom ?? 17 });
  const showYou = (animate = false) => {
    if (framed) fit(framed, animate);
    else map.easeTo({ center: you.point, zoom: Math.max(map.getZoom(), 17), duration: animate ? 600 : 0 });
  };
  const moved = () => {
    const b = map.getBounds();
    receiver?.invokeMethodAsync("OnMoved", b.getSouth(), b.getWest(), b.getNorth(), b.getEast());
  };
  map.on("moveend", moved);
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
    map.setStyle(styleNow());
  });
  watcher.observe(document.documentElement, { attributes: true, attributeFilter: ["data-scheme", "data-theme"] });

  const ready = new Promise((resolve) => map.once("load", resolve));

  let limit = null;
  const showReach = () => {
    ["beyond", "others", "edge", "notes"].forEach((name) => map.getSource("reach-" + name)?.setData(data.reach?.[name] ?? empty));
    map.setMaxBounds(limit);
  };
  const setReach = (json) => {
    const reach = json ? JSON.parse(json) : null;
    data.reach = reach && {
      beyond: { type: "FeatureCollection", features: [{ type: "Feature", properties: {}, geometry: { type: "MultiPolygon", coordinates: reach.beyond } }] },
      edge: { type: "FeatureCollection", features: reach.edge.map((points) => ({ type: "Feature", properties: {}, geometry: { type: "LineString", coordinates: points } })) },
      others: { type: "FeatureCollection", features: reach.others.map(area) },
      notes: { type: "FeatureCollection", features: reach.notes.map(marker) },
    };
    limit = reach?.limit ? [[reach.limit.west, reach.limit.south], [reach.limit.east, reach.limit.north]] : null;
    if (map.isStyleLoaded()) showReach();
  };

  return {
    map,
    setReach,
    setLines: (json) => set("lines", { type: "FeatureCollection", features: JSON.parse(json).map(line) }),
    setAreas: (json) => set("areas", { type: "FeatureCollection", features: JSON.parse(json).map(area) }),
    setMarkers: (json) => set("markers", { type: "FeatureCollection", features: JSON.parse(json).map(marker) }),
    setYou: (json) => {
      you = json ? JSON.parse(json) : null;
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
    centre: (lat, lon, zoom) => map.jumpTo({ center: [lon, lat], zoom: zoom ?? map.getZoom() }),
    resize: () => map.resize(),
    ready: () => ready,
    dispose: () => {
      watcher.disconnect();
      map.remove();
    },
  };
}
