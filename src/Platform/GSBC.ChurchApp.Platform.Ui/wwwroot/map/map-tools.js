const empty = { type: "FeatureCollection", features: [] };
const fullLabelsFrom = 15;
const token = (name) => getComputedStyle(document.documentElement).getPropertyValue(name).trim();
const closeEnoughPixels = 12;
const spacingPixels = 4;
const drawingEveryMs = 80;
const hoverEveryMs = 60;
const labelHeight = 24;
const labelSides = 18;
const labelGap = 8;
const labelShifts = [[0, 0], ...[16, 30, 46, 64, 84].flatMap((r) =>
  Array.from({ length: 8 }, (_, a) => [Math.round(Math.cos((a * Math.PI) / 4) * r), Math.round(Math.sin((a * Math.PI) / 4) * r * 0.7)]))];
let measurer;

function labelWidth(text) {
  measurer ??= document.createElement("canvas").getContext("2d");
  measurer.font = "600 12px Montserrat, sans-serif";
  return measurer.measureText(text).width + labelSides + labelGap;
}

const overlap = (a, b) =>
  Math.max(0, Math.min(a.x + a.w, b.x + b.w) - Math.max(a.x, b.x)) * Math.max(0, Math.min(a.y + a.h, b.y + b.h) - Math.max(a.y, b.y));

function placeLabels(map, labels) {
  const full = map.getZoom() >= fullLabelsFrom;
  const canvas = map.getCanvas();
  const shown = { x: 0, y: 0, w: canvas.clientWidth, h: canvas.clientHeight };
  const outside = (box) => box.w * box.h - overlap(box, shown);
  const from = canvas.getBoundingClientRect();
  const taken = [...document.querySelectorAll("[data-map-cover]")].map((cover) => {
    const at = cover.getBoundingClientRect();
    return { x: at.left - from.left, y: at.top - from.top, w: at.width, h: at.height };
  });
  const placed = new Array(labels.length);
  const topFirst = labels.map((label, i) => ({ label, i, order: label.order ?? i })).sort((a, b) => b.order - a.order);
  for (const { label, i } of topFirst) {
    const w = labelWidth(full ? label.text : label.short ?? label.text), h = labelHeight + labelGap;
    const spots = [label.point, ...(label.places ?? [])].map((point) => map.project(point));
    let best = null, least = Infinity;
    search: for (const [dx, dy] of labelShifts) {
      for (const spot of spots) {
        const box = { x: spot.x + dx - w / 2, y: spot.y + dy - h / 2, w, h };
        const clash = taken.reduce((sum, other) => sum + overlap(box, other), outside(box));
        if (clash < least) [best, least] = [box, clash];
        if (clash === 0) break search;
      }
    }
    taken.push(best);
    placed[i] = map.unproject([best.x + w / 2, best.y + h / 2]).toArray();
  }
  return placed;
}

function capsule(map, colour, filled) {
  const id = `tools-capsule-${colour}-${filled ? "filled" : "ring"}`;
  if (map.hasImage(id)) return id;
  const size = 40, scale = 2;
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = size * scale;
  const g = canvas.getContext("2d");
  g.scale(scale, scale);
  g.beginPath();
  g.roundRect(5, 8, size - 10, 24, 12);
  g.fillStyle = filled ? colour : token("--surface");
  g.fill();
  g.lineWidth = 2.5;
  g.strokeStyle = colour;
  g.stroke();
  map.addImage(id, g.getImageData(0, 0, size * scale, size * scale), {
    pixelRatio: scale, stretchX: [[18 * scale, 22 * scale]], stretchY: [[18 * scale, 22 * scale]], content: [12 * scale, 10 * scale, 28 * scale, 30 * scale],
  });
  return id;
}

const addLayer = (map, layer) => {
  if (map.getLayer(layer.id)) return;
  try {
    map.addLayer(layer);
  } catch {
  }
};

function layers(map) {
  if (map.getLayer("tools-labels")) return;
  const accent = token("--accent"), surface = token("--surface");
  for (const id of ["tools-sketch", "tools-labels"]) {
    if (!map.getSource(id)) map.addSource(id, { type: "geojson", data: empty });
  }
  for (const id of ["tools-sketch-fill", "tools-sketch-edge", "tools-sketch-points"]) {
    if (map.getLayer(id)) map.removeLayer(id);
  }
  addLayer(map, { id: "tools-sketch-fill", type: "fill", source: "tools-sketch", filter: ["==", ["geometry-type"], "Polygon"],
    paint: { "fill-color": accent, "fill-opacity": 0.1 } });
  addLayer(map, { id: "tools-sketch-edge", type: "line", source: "tools-sketch", filter: ["!=", ["geometry-type"], "Point"],
    layout: { "line-cap": "round", "line-join": "round" },
    paint: { "line-color": ["coalesce", ["get", "colour"], accent], "line-width": ["coalesce", ["get", "width"], 2.5],
      "line-dasharray": ["case", ["==", ["get", "dashed"], true], ["literal", [2, 1.5]], ["literal", [1, 0]]] } });
  addLayer(map, { id: "tools-sketch-points", type: "circle", source: "tools-sketch", filter: ["==", ["geometry-type"], "Point"],
    paint: { "circle-radius": 5, "circle-color": surface, "circle-stroke-color": accent, "circle-stroke-width": 2 } });
  addLayer(map, { id: "tools-labels", type: "symbol", source: "tools-labels",
    layout: {
      "icon-image": ["get", "image"], "icon-text-fit": "both", "icon-text-fit-padding": [3, 9, 3, 9], "icon-allow-overlap": true, "text-allow-overlap": true, "symbol-z-order": "source",
      "text-field": ["step", ["zoom"], ["get", "short"], fullLabelsFrom, ["get", "text"]], "text-font": ["montserrat-semibold"], "text-size": 12, "symbol-sort-key": ["get", "order"],
    },
    paint: { "text-color": ["get", "ink"] } });
}

const pointOf = (e) => [e.lngLat.lng, e.lngLat.lat];

export function attach(handle, receiver) {
  const map = handle.map;
  const canvas = map.getCanvasContainer();
  let tool = "none";
  let points = [];
  let pressed = false;
  let lastDrawing = 0;
  let lastHover = 0;
  let labels = empty;
  let shownLabels = [];
  const labelFeatures = () => {
    const placed = placeLabels(map, shownLabels);
    return { type: "FeatureCollection", features: shownLabels.map((l, i) => ({
      type: "Feature",
      properties: { text: l.text, short: l.short ?? l.text, image: capsule(map, l.colour, l.filled), ink: l.filled ? "#ffffff" : token("--ink"), order: l.order ?? i },
      geometry: { type: "Point", coordinates: placed[i] } })) };
  };
  let relabelling = 0;
  const relabel = () => {
    relabelling = 0;
    if (!map.getSource("tools-labels") || shownLabels.length === 0) return;
    labels = labelFeatures();
    map.getSource("tools-labels").setData(labels);
  };
  const relabelSoon = () => { if (!relabelling) relabelling = requestAnimationFrame(relabel); };
  map.on("zoom", relabelSoon);
  map.on("zoomend", relabel);

  const sketch = (features) => map.getSource("tools-sketch")?.setData({ type: "FeatureCollection", features });
  const ensure = () => {
    if (!map.getLayer("areas-fill")) return;
    layers(map);
    labels = labelFeatures();
    map.getSource("tools-labels")?.setData(labels);
  };
  map.on("style.load", ensure);
  map.on("styledata", ensure);
  handle.ready().then(ensure);
  ensure();

  const reset = () => {
    points = [];
    pressed = false;
    sketch([]);
  };

  const drawPolygon = (hover) => {
    const ring = hover ? [...points, hover] : points;
    const features = points.map((p) => ({ type: "Feature", properties: {}, geometry: { type: "Point", coordinates: p } }));
    if (ring.length >= 3) features.unshift({ type: "Feature", properties: {}, geometry: { type: "Polygon", coordinates: [[...ring, ring[0]]] } });
    else if (ring.length === 2) features.unshift({ type: "Feature", properties: { dashed: true }, geometry: { type: "LineString", coordinates: ring } });
    sketch(features);
  };

  const drawStroke = () => {
    if (points.length < 2) return sketch([]);
    const closed = tool === "lasso";
    const brush = tool === "brush";
    sketch([{ type: "Feature", properties: { dashed: !brush, width: brush ? 9 : 2.5, colour: brush ? handle.brushColour ?? null : null },
      geometry: closed && points.length >= 3 ? { type: "Polygon", coordinates: [[...points, points[0]]] } : { type: "LineString", coordinates: points } }]);
  };

  const finish = (kind, coordinates) => {
    reset();
    receiver.invokeMethodAsync("OnDrawn", kind, JSON.stringify(coordinates));
  };

  const nearFirst = (e) => {
    if (points.length < 3) return false;
    const first = map.project(points[0]);
    return Math.hypot(first.x - e.point.x, first.y - e.point.y) <= closeEnoughPixels;
  };

  const onClick = (e) => {
    if (tool === "pin") return finish("pin", [pointOf(e)]);
    if (tool !== "polygon") return;
    if (nearFirst(e)) return finish("polygon", points);
    points.push(pointOf(e));
    drawPolygon();
  };

  const onDoubleClick = (e) => {
    if (tool !== "polygon") return;
    e.preventDefault();
    if (points.length >= 3) finish("polygon", points);
  };

  const onDown = (e) => {
    if (!["lasso", "line", "brush"].includes(tool)) return;
    if (e.originalEvent.button !== undefined && e.originalEvent.button !== 0) return;
    e.preventDefault();
    pressed = true;
    points = [pointOf(e)];
  };

  const onMove = (e) => {
    const now = performance.now();
    if (tool === "none" && now - lastHover > hoverEveryMs) {
      lastHover = now;
      receiver.invokeMethodAsync("OnHover", e.lngLat.lat, e.lngLat.lng);
      return;
    }
    if (tool === "polygon" && points.length > 0) return drawPolygon(pointOf(e));
    if (!pressed) return;
    const last = map.project(points[points.length - 1]);
    if (Math.hypot(last.x - e.point.x, last.y - e.point.y) < spacingPixels) return;
    points.push(pointOf(e));
    drawStroke();
    if (tool !== "lasso" && now - lastDrawing > drawingEveryMs) {
      lastDrawing = now;
      receiver.invokeMethodAsync("OnDrawing", tool, JSON.stringify(points));
    }
  };

  const onUp = () => {
    if (!pressed) return;
    pressed = false;
    if (points.length >= (tool === "lasso" ? 3 : 2)) finish(tool, points);
    else reset();
  };

  const onLeave = () => {
    if (tool === "none") receiver.invokeMethodAsync("OnHoverEnded");
  };

  const onKey = (e) => {
    if (tool === "none" || e.target?.closest?.("input, textarea, [contenteditable]")) return;
    if (e.key === "Escape" && (points.length > 0 || pressed)) {
      e.stopPropagation();
      reset();
      receiver.invokeMethodAsync("OnCancelled");
    } else if (e.key === "Backspace" && tool === "polygon" && points.length > 0) {
      e.preventDefault();
      points.pop();
      drawPolygon();
    } else if (e.key === "Enter" && tool === "polygon" && points.length >= 3) {
      finish("polygon", points);
    }
  };

  map.on("click", onClick);
  map.on("dblclick", onDoubleClick);
  map.on("mousedown", onDown);
  map.on("touchstart", onDown);
  map.on("mousemove", onMove);
  map.on("touchmove", onMove);
  map.on("mouseup", onUp);
  map.on("touchend", onUp);
  map.on("mouseout", onLeave);
  document.addEventListener("keydown", onKey, true);

  return {
    setTool: (next) => {
      tool = next;
      reset();
      const drags = ["lasso", "line", "brush"].includes(next);
      if (drags) map.dragPan.disable(); else map.dragPan.enable();
      if (next === "polygon") map.doubleClickZoom.disable(); else map.doubleClickZoom.enable();
      canvas.dataset.tool = next;
    },
    setBrushColour: (colour) => { handle.brushColour = colour; },
    setLabels: (json) => {
      shownLabels = JSON.parse(json);
      if (!map.getSource("tools-labels")) return;
      labels = labelFeatures();
      map.getSource("tools-labels").setData(labels);
    },
    preview: (json) => {
      const lines = JSON.parse(json);
      sketch(lines.map((l) => ({ type: "Feature", properties: { colour: l.colour, width: l.width ?? 7 },
        geometry: { type: "LineString", coordinates: l.points } })));
    },
    dispose: () => {
      document.removeEventListener("keydown", onKey, true);
      for (const [name, fn] of [["click", onClick], ["dblclick", onDoubleClick], ["mousedown", onDown], ["touchstart", onDown], ["mousemove", onMove],
        ["touchmove", onMove], ["mouseup", onUp], ["touchend", onUp], ["mouseout", onLeave], ["styledata", ensure], ["style.load", ensure],
        ["zoom", relabelSoon], ["zoomend", relabel]]) map.off(name, fn);
      cancelAnimationFrame(relabelling);
      map.dragPan.enable();
      map.doubleClickZoom.enable();
    },
  };
}
