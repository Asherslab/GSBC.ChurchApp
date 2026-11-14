const empty = { type: "FeatureCollection", features: [] };
const token = (name) => getComputedStyle(document.documentElement).getPropertyValue(name).trim();
const closeEnoughPixels = 12;
const spacingPixels = 4;
const drawingEveryMs = 80;
const hoverEveryMs = 60;

function capsule(map) {
  if (map.hasImage("tools-capsule")) return;
  const size = 40, radius = 13, scale = 2;
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = size * scale;
  const g = canvas.getContext("2d");
  g.scale(scale, scale);
  g.fillStyle = "#000";
  g.beginPath();
  g.roundRect(4, 7, size - 8, 26, radius);
  g.fill();
  map.addImage("tools-capsule", g.getImageData(0, 0, size * scale, size * scale), {
    pixelRatio: scale, sdf: true, stretchX: [[18 * scale, 22 * scale]], stretchY: [[18 * scale, 22 * scale]], content: [12 * scale, 9 * scale, 28 * scale, 31 * scale],
  });
}

function layers(map) {
  if (map.getSource("tools-sketch")) return;
  const accent = token("--accent"), surface = token("--surface");
  capsule(map);
  map.addSource("tools-sketch", { type: "geojson", data: empty });
  map.addSource("tools-labels", { type: "geojson", data: empty });
  map.addLayer({ id: "tools-sketch-fill", type: "fill", source: "tools-sketch", filter: ["==", ["geometry-type"], "Polygon"],
    paint: { "fill-color": accent, "fill-opacity": 0.1 } });
  map.addLayer({ id: "tools-sketch-edge", type: "line", source: "tools-sketch", filter: ["!=", ["geometry-type"], "Point"],
    layout: { "line-cap": "round", "line-join": "round" },
    paint: { "line-color": ["coalesce", ["get", "colour"], accent], "line-width": ["coalesce", ["get", "width"], 2.5],
      "line-dasharray": ["case", ["==", ["get", "dashed"], true], ["literal", [2, 1.5]], ["literal", [1, 0]]] } });
  map.addLayer({ id: "tools-sketch-points", type: "circle", source: "tools-sketch", filter: ["==", ["geometry-type"], "Point"],
    paint: { "circle-radius": 5, "circle-color": surface, "circle-stroke-color": accent, "circle-stroke-width": 2 } });
  map.addLayer({ id: "tools-labels", type: "symbol", source: "tools-labels",
    layout: {
      "icon-image": "tools-capsule", "icon-text-fit": "both", "icon-text-fit-padding": [3, 9, 3, 9], "icon-allow-overlap": true, "text-allow-overlap": true,
      "text-field": ["get", "text"], "text-font": ["montserrat-semibold"], "text-size": 12, "symbol-sort-key": ["get", "order"],
    },
    paint: { "icon-color": ["get", "colour"], "text-color": ["coalesce", ["get", "ink"], "#ffffff"], "icon-halo-color": surface, "icon-halo-width": 2 } });
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

  const sketch = (features) => map.getSource("tools-sketch")?.setData({ type: "FeatureCollection", features });
  const ensure = () => {
    if (!map.isStyleLoaded()) return;
    layers(map);
    map.getSource("tools-labels")?.setData(labels);
  };
  map.on("styledata", ensure);
  if (map.isStyleLoaded()) ensure(); else map.once("load", ensure);

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
      labels = { type: "FeatureCollection", features: JSON.parse(json).map((l, i) => ({
        type: "Feature", properties: { text: l.text, colour: l.colour, ink: l.ink ?? "#ffffff", order: l.order ?? i },
        geometry: { type: "Point", coordinates: l.point } })) };
      map.getSource("tools-labels")?.setData(labels);
    },
    preview: (json) => {
      const lines = JSON.parse(json);
      sketch(lines.map((l) => ({ type: "Feature", properties: { colour: l.colour, width: l.width ?? 7 },
        geometry: { type: "LineString", coordinates: l.points } })));
    },
    dispose: () => {
      document.removeEventListener("keydown", onKey, true);
      for (const [name, fn] of [["click", onClick], ["dblclick", onDoubleClick], ["mousedown", onDown], ["touchstart", onDown], ["mousemove", onMove],
        ["touchmove", onMove], ["mouseup", onUp], ["touchend", onUp], ["mouseout", onLeave], ["styledata", ensure]]) map.off(name, fn);
      map.dragPan.enable();
      map.doubleClickZoom.enable();
    },
  };
}
