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
  };
}

async function tilesFor(source) {
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
    { id: "areas-fill", type: "fill", source: "areas", paint: { "fill-color": ["coalesce", ["get", "colour"], c.walk], "fill-opacity": 0.12 } },
    { id: "areas-edge", type: "line", source: "areas", paint: { "line-color": ["coalesce", ["get", "colour"], c.walk], "line-width": 1.5 } },
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

function withOverlays(base, data) {
  const c = colours();
  base.sources.lines = { type: "geojson", data: data.lines };
  base.sources.areas = { type: "geojson", data: data.areas };
  base.sources.markers = { type: "geojson", data: data.markers };
  base.sources.you = { type: "geojson", data: data.you };
  base.layers.push(...overlayLayers(c));
  return base;
}

const line = (l) => ({ type: "Feature", properties: { style: l.style, ...(l.label ? { label: l.label } : {}), ...(l.colour ? { colour: l.colour } : {}), id: l.id ?? "" },
  geometry: { type: "LineString", coordinates: l.points } });
const area = (a) => ({ type: "Feature", properties: { ...(a.colour ? { colour: a.colour } : {}), id: a.id ?? "" }, geometry: { type: "Polygon", coordinates: [a.points] } });
const marker = (m) => ({ type: "Feature", properties: { kind: m.kind, ...(m.label ? { label: m.label } : {}), id: m.id ?? "" }, geometry: { type: "Point", coordinates: m.point } });

function metresToPixels(map, metres, lat) {
  const metresPerPixel = (40075016.686 * Math.cos((lat * Math.PI) / 180)) / Math.pow(2, map.getZoom() + 9);
  return metres / metresPerPixel;
}

export async function create(element, optionsJson, receiver) {
  loadStylesheet();
  const options = JSON.parse(optionsJson);
  const tiles = await tilesFor(options.source);
  const data = { lines: empty, areas: empty, markers: empty, you: empty };
  let you = null;
  let following = !!options.follow;
  let dragged = false;
  let shownScheme = scheme();

  const styleNow = () => withOverlays(mapStyle({ tiles, glyphs, scheme: shownScheme, attribution: options.source.attribution ?? "" }), data);
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
    map.fitBounds([[b.west, b.south], [b.east, b.north]], { padding: b.padding ?? 40, duration: animate ? 600 : 0, maxZoom: b.maxZoom ?? 17 });
  const showYou = (animate = false) => {
    if (framed) fit(framed, animate);
    else map.easeTo({ center: you.point, zoom: Math.max(map.getZoom(), 17), duration: animate ? 600 : 0 });
  };
  map.on("load", () => {
    Object.entries(data).forEach(([name, value]) => map.getSource(name)?.setData(value));
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
    map.setStyle(styleNow());
  });
  watcher.observe(document.documentElement, { attributes: true, attributeFilter: ["data-scheme", "data-theme"] });

  const ready = new Promise((resolve) => map.once("load", resolve));

  return {
    map,
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
