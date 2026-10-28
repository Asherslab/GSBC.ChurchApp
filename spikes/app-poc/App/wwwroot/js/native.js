(() => {
  const cap = () => window.Capacitor;
  const isNative = () => !!cap()?.isNativePlatform?.();
  const platform = () => cap()?.getPlatform?.() ?? 'web';
  const plugins = new Map();
  const plugin = (name) => {
    if (!isNative() || !cap().isPluginAvailable?.(name)) return null;
    if (!plugins.has(name)) plugins.set(name, cap().registerPlugin(name));
    return plugins.get(name);
  };

  const storage = {
    get(key) {
      try { return localStorage.getItem(`gsbc:${key}`); } catch { return null; }
    },
    set(key, value) {
      try { localStorage.setItem(`gsbc:${key}`, value); } catch { }
    },
  };

  const darkQuery = window.matchMedia('(prefers-color-scheme: dark)');
  const applyScheme = () => {
    const root = document.documentElement;
    const appearance = root.dataset.appearance ?? 'System';
    const dark = appearance === 'Dark' || (appearance === 'System' && darkQuery.matches);
    root.dataset.scheme = dark ? 'dark' : 'light';
  };
  darkQuery.addEventListener('change', applyScheme);

  const scrollerOf = (element) => element?.closest('.page-scroll');
  const stuckHeight = (scroller) => {
    const sticky = scroller.querySelector('.section-tabs') ?? scroller.querySelector('.topbar');
    return sticky ? (parseFloat(getComputedStyle(sticky).top) || 0) + sticky.offsetHeight : 0;
  };

  const player = {
    audio: new Audio(),
    listener: null,
    id: null,
    report() {
      const a = player.audio;
      player.listener?.invokeMethodAsync('OnTick', player.id ?? '', a.currentTime || 0, a.duration || 0, !a.paused);
    },
  };
  player.audio.preload = 'metadata';
  ['timeupdate', 'play', 'pause', 'loadedmetadata', 'ended', 'error'].forEach((e) => player.audio.addEventListener(e, player.report));

  let installPrompt = null;
  window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault();
    installPrompt = e;
  });

  const maps = (() => {
    let loading = null;
    let position = null;
    let watcher = null;
    const watchers = new Set();
    const empty = { type: 'FeatureCollection', features: [] };
    const point = (lat, lon) => ({ type: 'FeatureCollection', features: lat == null ? [] : [{ type: 'Feature', geometry: { type: 'Point', coordinates: [lon, lat] }, properties: {} }] });

    const load = () => loading ??= new Promise((resolve, reject) => {
      if (window.maplibregl) { resolve(); return; }
      const css = document.createElement('link');
      css.rel = 'stylesheet';
      css.href = 'lib/maplibre/maplibre-gl.css';
      document.head.appendChild(css);
      const js = document.createElement('script');
      js.src = 'lib/maplibre/maplibre-gl.js';
      js.onload = () => resolve();
      js.onerror = () => { loading = null; reject(new Error('Map library failed to load')); };
      document.head.appendChild(js);
    });

    const token = (name) => getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    const styleUrl = () => `https://tiles.openfreemap.org/styles/${document.documentElement.dataset.scheme === 'dark' ? 'dark' : 'positron'}`;

    const arrowImage = () => {
      const size = 32;
      const canvas = document.createElement('canvas');
      canvas.width = canvas.height = size;
      const g = canvas.getContext('2d');
      g.strokeStyle = '#ffffff';
      g.lineWidth = 4;
      g.lineCap = g.lineJoin = 'round';
      g.beginPath();
      g.moveTo(11, 8);
      g.lineTo(21, 16);
      g.lineTo(11, 24);
      g.stroke();
      return g.getImageData(0, 0, size, size);
    };

    const walkThroughPaint = () => ({ 'line-width': 2.5, 'line-color': token('--ink-2'), 'line-opacity': 0.85, 'line-dasharray': [2, 2] });

    const addLayers = (el) => {
      const state = el.gsbcStreetMap;
      const map = state.map;
      if (!map.hasImage('gsbc-arrow')) map.addImage('gsbc-arrow', arrowImage(), { pixelRatio: 2 });
      map.addSource('gsbc-route', { type: 'geojson', data: state.route ?? empty });
      map.addSource('gsbc-start', { type: 'geojson', data: point(state.start?.[0], state.start?.[1]) });
      map.addSource('gsbc-me', { type: 'geojson', data: point(position?.[0], position?.[1]) });
      map.addSource('gsbc-marks', { type: 'geojson', data: state.marks ?? empty });
      const side = { 'line-offset': -4, 'line-width': 5 };
      map.addLayer({ id: 'gsbc-route-done', type: 'line', source: 'gsbc-route', filter: ['==', ['get', 'state'], 'done'], layout: { 'line-cap': 'round', 'line-join': 'round' }, paint: { ...side, 'line-color': token('--ink-3'), 'line-opacity': 0.55 } });
      map.addLayer({ id: 'gsbc-route-todo', type: 'line', source: 'gsbc-route', filter: ['==', ['get', 'state'], 'todo'], layout: { 'line-cap': 'round', 'line-join': 'round' }, paint: { ...side, 'line-color': token('--primary') } });
      map.addLayer({ id: 'gsbc-route-transit', type: 'line', source: 'gsbc-route', filter: ['==', ['get', 'state'], 'transit'], layout: { 'line-cap': 'butt' }, paint: walkThroughPaint() });
      map.addLayer({ id: 'gsbc-route-now', type: 'line', source: 'gsbc-route', filter: ['==', ['get', 'state'], 'now'], layout: { 'line-cap': 'round', 'line-join': 'round' }, paint: { 'line-offset': -4, 'line-width': 7, 'line-color': token('--marker') } });
      map.addLayer({ id: 'gsbc-route-arrows', type: 'symbol', source: 'gsbc-route', filter: ['in', ['get', 'state'], ['literal', ['todo', 'now']]], layout: { 'symbol-placement': 'line', 'symbol-spacing': 90, 'icon-image': 'gsbc-arrow', 'icon-size': 0.5, 'icon-offset': [0, -8], 'icon-rotation-alignment': 'map', 'icon-allow-overlap': true } });
      map.addLayer({ id: 'gsbc-skips', type: 'circle', source: 'gsbc-marks', filter: ['==', ['get', 'kind'], 'skip'], paint: { 'circle-radius': 5, 'circle-color': token('--ink-3'), 'circle-stroke-width': 2, 'circle-stroke-color': token('--bg') } });
      map.addLayer({ id: 'gsbc-dont', type: 'circle', source: 'gsbc-marks', filter: ['==', ['get', 'kind'], 'dont'], paint: { 'circle-radius': 8, 'circle-color': token('--danger'), 'circle-stroke-width': 3, 'circle-stroke-color': '#ffffff' } });
      map.addLayer({ id: 'gsbc-start', type: 'circle', source: 'gsbc-start', paint: { 'circle-radius': 8, 'circle-color': token('--primary'), 'circle-stroke-width': 3, 'circle-stroke-color': '#ffffff' } });
      map.addLayer({ id: 'gsbc-me-halo', type: 'circle', source: 'gsbc-me', paint: { 'circle-radius': 16, 'circle-color': token('--live'), 'circle-opacity': 0.2 } });
      map.addLayer({ id: 'gsbc-me', type: 'circle', source: 'gsbc-me', paint: { 'circle-radius': 7, 'circle-color': token('--live'), 'circle-stroke-width': 3, 'circle-stroke-color': '#ffffff' } });
    };

    const showMe = () => {
      watchers.forEach((el) => el.gsbcStreetMap?.map.getSource('gsbc-me')?.setData(point(position?.[0], position?.[1])));
      watchers.forEach((el) => {
        const state = el.gsbcStreetMap;
        if (state?.follow && position) state.map.easeTo({ center: [position[1], position[0]], duration: 600 });
      });
    };

    const startWatching = () => {
      if (watcher != null || !navigator.geolocation) return;
      watcher = navigator.geolocation.watchPosition((p) => { position = [p.coords.latitude, p.coords.longitude]; showMe(); }, () => { }, { enableHighAccuracy: true, maximumAge: 5000 });
    };

    const stopWatching = () => {
      if (watcher == null || watchers.size > 0) return;
      navigator.geolocation.clearWatch(watcher);
      watcher = null;
    };

    const createMap = (el, lat, lon, attribution) => {
      const map = new maplibregl.Map({
        container: el,
        style: styleUrl(),
        center: [lon, lat],
        zoom: 15,
        attributionControl: { compact: true, customAttribution: attribution },
        dragRotate: false,
        pitchWithRotate: false,
      });
      map.touchZoomRotate.disableRotation();
      map.once('load', () => el.querySelector('.maplibregl-ctrl-attrib')?.classList.remove('maplibregl-compact-show'));
      let scheme = document.documentElement.dataset.scheme;
      new MutationObserver(() => {
        if (scheme === document.documentElement.dataset.scheme) return;
        scheme = document.documentElement.dataset.scheme;
        map.setStyle(styleUrl());
      }).observe(document.documentElement, { attributes: true, attributeFilter: ['data-scheme'] });
      return map;
    };

    const street = {
      async attach(el, dotnet, lat, lon, attribution) {
        await load();
        const map = createMap(el, lat, lon, attribution);
        el.gsbcStreetMap = { map, dotnet, route: null, start: null, follow: false, pick: false };
        map.on('style.load', () => addLayers(el));
        map.on('click', (e) => { if (el.gsbcStreetMap.pick) dotnet.invokeMethodAsync('Picked', e.lngLat.lat, e.lngLat.lng); });
      },

      update(el, routeJson, startLat, startLon, fit, pick, follow, showLocation, marksJson) {
        const state = el.gsbcStreetMap;
        if (!state) return;
        state.route = routeJson ? JSON.parse(routeJson) : null;
        state.marks = marksJson ? JSON.parse(marksJson) : null;
        state.start = startLat == null ? null : [startLat, startLon];
        state.pick = pick;
        state.follow = follow;
        el.classList.toggle('street-map--picking', pick);
        const apply = () => {
          state.map.getSource('gsbc-route')?.setData(state.route ?? empty);
          state.map.getSource('gsbc-start')?.setData(point(state.start?.[0], state.start?.[1]));
          state.map.getSource('gsbc-marks')?.setData(state.marks ?? empty);
          if (fit) {
            const coords = (state.route?.features ?? []).flatMap((f) => f.geometry.coordinates);
            if (state.start) coords.push([state.start[1], state.start[0]]);
            if (coords.length > 1) {
              const bounds = coords.reduce((b, c) => b.extend(c), new maplibregl.LngLatBounds(coords[0], coords[0]));
              state.map.fitBounds(bounds, { padding: 36, maxZoom: 17, duration: 500 });
            } else if (coords.length === 1) {
              state.map.easeTo({ center: coords[0], zoom: 16, duration: 500 });
            }
          }
        };
        if (state.map.isStyleLoaded() && state.map.getSource('gsbc-route')) apply();
        else state.map.once('idle', apply);
        if (showLocation) { watchers.add(el); startWatching(); showMe(); }
        else { watchers.delete(el); stopWatching(); }
      },

      locate() {
        return new Promise((resolve) => {
          if (!navigator.geolocation) { resolve('unsupported'); return; }
          navigator.geolocation.getCurrentPosition(
            (p) => { position = [p.coords.latitude, p.coords.longitude]; showMe(); resolve(`${p.coords.latitude},${p.coords.longitude}`); },
            (e) => resolve(e.code === 1 ? 'denied' : 'unavailable'),
            { enableHighAccuracy: true, timeout: 10000, maximumAge: 30000 });
        });
      },

      position: () => position ? `${position[0]},${position[1]}` : '',

      detach(el) {
        watchers.delete(el);
        stopWatching();
        el.gsbcStreetMap?.map.remove();
        el.gsbcStreetMap = null;
      },
    };

    const textFont = (map) => map.getStyle().layers.find((l) => l.layout?.['text-font'])?.layout['text-font'] ?? ['Noto Sans Regular'];

    const addPlannerLayers = (el) => {
      const state = el.gsbcPlanner;
      const map = state.map;
      const data = state.data ?? {};
      map.addSource('planner-area', { type: 'geojson', data: data.area ?? empty });
      map.addSource('planner-streets', { type: 'geojson', data: data.streets ?? empty, promoteId: 'edge' });
      map.addSource('planner-starts', { type: 'geojson', data: data.starts ?? empty });
      map.addSource('planner-meeting', { type: 'geojson', data: data.meeting ?? empty });
      map.addSource('planner-zones', { type: 'geojson', data: data.zones ?? empty });
      map.addSource('planner-pins', { type: 'geojson', data: data.pins ?? empty });
      map.addSource('planner-gesture', { type: 'geojson', data: empty });
      map.addLayer({ id: 'planner-zones-fill', type: 'fill', source: 'planner-zones', paint: { 'fill-color': token('--danger'), 'fill-opacity': 0.14 } });
      map.addLayer({ id: 'planner-zones-line', type: 'line', source: 'planner-zones', paint: { 'line-color': token('--danger'), 'line-width': 2 } });
      map.addLayer({ id: 'planner-area-fill', type: 'fill', source: 'planner-area', filter: ['==', ['geometry-type'], 'Polygon'], paint: { 'fill-color': token('--accent'), 'fill-opacity': 0.08 } });
      map.addLayer({ id: 'planner-area-line', type: 'line', source: 'planner-area', filter: ['==', ['geometry-type'], 'LineString'], paint: { 'line-color': token('--accent'), 'line-width': 2, 'line-dasharray': [2, 1.5] } });
      map.addLayer({ id: 'planner-area-points', type: 'circle', source: 'planner-area', filter: ['==', ['geometry-type'], 'Point'], paint: { 'circle-radius': 5, 'circle-color': '#ffffff', 'circle-stroke-width': 2, 'circle-stroke-color': token('--accent') } });
      map.addLayer({ id: 'planner-rested', type: 'line', source: 'planner-streets', filter: ['==', ['get', 'kind'], 'rested'], layout: { 'line-cap': 'round' }, paint: { 'line-color': ['coalesce', ['feature-state', 'paint'], token('--ink-3')], 'line-width': 4, 'line-opacity': 0.6, 'line-dasharray': [1, 1.4] } });
      map.addLayer({ id: 'planner-through', type: 'line', source: 'planner-streets', filter: ['==', ['get', 'kind'], 'through'], layout: { 'line-cap': 'butt' }, paint: walkThroughPaint() });
      map.addLayer({ id: 'planner-halo', type: 'line', source: 'planner-streets', filter: ['==', ['get', 'kind'], 'section'], layout: { 'line-cap': 'round', 'line-join': 'round' }, paint: { 'line-color': ['get', 'color'], 'line-width': 16, 'line-opacity': 0, 'line-blur': 1 } });
      map.addLayer({ id: 'planner-streets', type: 'line', source: 'planner-streets', filter: ['!', ['in', ['get', 'kind'], ['literal', ['rested', 'through']]]], layout: { 'line-cap': 'round', 'line-join': 'round' }, paint: { 'line-color': ['coalesce', ['get', 'color'], token('--ink-3')], 'line-width': ['case', ['==', ['get', 'kind'], 'free'], 4, 6] } });
      map.addLayer({ id: 'planner-gesture-fill', type: 'fill', source: 'planner-gesture', filter: ['==', ['geometry-type'], 'Polygon'], paint: { 'fill-color': token('--ink'), 'fill-opacity': 0.06 } });
      map.addLayer({ id: 'planner-gesture-line', type: 'line', source: 'planner-gesture', filter: ['==', ['geometry-type'], 'LineString'], layout: { 'line-cap': 'round', 'line-join': 'round' }, paint: { 'line-color': token('--ink'), 'line-width': 2, 'line-dasharray': [2, 1.5] } });
      map.addLayer({ id: 'planner-meeting', type: 'circle', source: 'planner-meeting', paint: { 'circle-radius': 9, 'circle-color': token('--primary'), 'circle-stroke-width': 3, 'circle-stroke-color': '#ffffff' } });
      map.addLayer({ id: 'planner-starts', type: 'circle', source: 'planner-starts', paint: { 'circle-radius': 6, 'circle-color': ['get', 'color'], 'circle-stroke-width': 2.5, 'circle-stroke-color': '#ffffff' } });
      map.addLayer({ id: 'planner-pins', type: 'circle', source: 'planner-pins', paint: { 'circle-radius': ['match', ['get', 'kind'], 'report', 9, 7], 'circle-color': ['match', ['get', 'kind'], 'report', token('--marker'), 'pending', token('--accent'), token('--danger')], 'circle-stroke-width': 3, 'circle-stroke-color': '#ffffff' } });
      emphasise(el);
    };

    const drawingTools = ['scissors', 'lasso', 'brush', 'eraser'];
    const hitLayers = ['planner-streets', 'planner-rested', 'planner-through'];
    const flat = (points) => points.flatMap((p) => [p.lng, p.lat]);
    const fs = (key) => ['boolean', ['feature-state', key], false];

    const emphasise = (el) => {
      const state = el.gsbcPlanner;
      const map = state.map;
      if (!map.getLayer('planner-streets')) return;
      const any = !!state.data?.anySelected;
      const hovered = state.hover ?? -1;
      const selected = ['==', ['get', 'selected'], true];
      const hot = ['==', ['get', 'route'], hovered];
      const unassigned = ['==', ['get', 'kind'], 'unassigned'];
      const painted = ['!=', ['feature-state', 'paint'], null];
      const routeColour = any ? ['case', selected, ['get', 'color'], hot, ['get', 'color'], ['get', 'muted']] : ['get', 'color'];
      map.setPaintProperty('planner-streets', 'line-color', ['coalesce', ['feature-state', 'paint'], ['case', unassigned, token('--ink-3'), ['coalesce', routeColour, token('--ink-3')]]]);
      map.setPaintProperty('planner-streets', 'line-width', ['case', fs('hover'), 10, ['==', ['get', 'kind'], 'free'], 4, unassigned, 4, selected, 8, hot, 7.5, any ? 5 : 6]);
      map.setPaintProperty('planner-streets', 'line-opacity', ['case', fs('erase'), 0.15, painted, 1, fs('dim'), 0.22, unassigned, 0.4, selected, 1, hot, any ? 0.85 : 1, any ? 0.38 : hovered >= 0 ? 0.6 : 1]);
      map.setLayoutProperty('planner-streets', 'line-sort-key', ['case', selected, 2, hot, 1, 0]);
      map.setLayoutProperty('planner-halo', 'line-sort-key', ['case', selected, 2, hot, 1, 0]);
      map.setPaintProperty('planner-halo', 'line-opacity', ['case', fs('dim'), 0, selected, 0.38, hot, 0.22, 0]);
      map.setPaintProperty('planner-halo', 'line-width', ['case', selected, 18, 15]);
      map.setPaintProperty('planner-through', 'line-opacity', any ? 0.35 : 0.85);
      map.setPaintProperty('planner-starts', 'circle-opacity', any ? ['case', ['in', ['get', 'route'], ['literal', selectedRoutes(state)]], 1, 0.35] : 1);
      map.setPaintProperty('planner-starts', 'circle-stroke-opacity', any ? ['case', ['in', ['get', 'route'], ['literal', selectedRoutes(state)]], 1, 0.35] : 1);
    };

    const selectedRoutes = (state) => [...new Set((state.data?.streets?.features ?? []).filter((f) => f.properties.selected).map((f) => f.properties.route))];

    const setHover = (el, route) => {
      const state = el.gsbcPlanner;
      if (state.hover === route) return;
      state.hover = route;
      emphasise(el);
      state.overlay?.querySelectorAll('[data-route]').forEach((n) => n.toggleAttribute('data-hover', Number(n.dataset.route) === route));
    };

    const place = (el) => {
      const state = el.gsbcPlanner;
      if (!state?.overlay) return;
      const height = state.overlay.clientHeight;
      state.overlay.querySelectorAll('[data-lng]').forEach((n) => {
        const p = state.map.project([Number(n.dataset.lng), Number(n.dataset.lat)]);
        n.style.transform = `translate(${Math.round(p.x)}px, ${Math.round(p.y)}px)`;
        n.toggleAttribute('data-above', p.y > height * 0.6);
        n.toggleAttribute('data-below', p.y < 150);
      });
    };

    const clearPreview = (el) => {
      const state = el.gsbcPlanner;
      state.map.removeFeatureState({ source: 'planner-streets' });
      state.map.getSource('planner-gesture')?.setData(empty);
      state.tip.hidden = true;
      state.stroke = null;
      state.hoverEdge = null;
    };

    const showTip = (state, point, parts) => {
      state.tip.replaceChildren(...parts.map(([colour, text]) => {
        const span = document.createElement('span');
        if (colour) {
          const swatch = document.createElement('i');
          swatch.style.setProperty('--swatch', colour);
          span.append(swatch);
        }
        span.append(text);
        return span;
      }));
      state.tip.hidden = false;
      const { clientWidth: width, clientHeight: height } = state.tip.parentElement;
      const x = point.x + 16 + state.tip.offsetWidth > width - 8 ? point.x - 16 - state.tip.offsetWidth : point.x + 16;
      const y = point.y + 16 + state.tip.offsetHeight > height - 8 ? point.y - 16 - state.tip.offsetHeight : point.y + 16;
      state.tip.style.transform = `translate(${Math.round(x)}px, ${Math.round(y)}px)`;
    };

    const edgeAt = (map, point) => {
      const box = [[point.x - 6, point.y - 6], [point.x + 6, point.y + 6]];
      return map.queryRenderedFeatures(box, { layers: hitLayers })[0];
    };

    const routeAt = (map, point) => {
      const box = [[point.x - 8, point.y - 8], [point.x + 8, point.y + 8]];
      const hit = map.queryRenderedFeatures(box, { layers: ['planner-streets'] }).find((f) => f.properties.route != null);
      return hit ? Number(hit.properties.route) : -1;
    };

    const strokeAt = (el, point) => {
      const state = el.gsbcPlanner;
      const hit = edgeAt(state.map, point);
      if (!hit) return;
      const edge = Number(hit.properties.edge);
      if (state.stroke.has(edge)) return;
      state.stroke.add(edge);
      if (state.tool === 'eraser') {
        if (hit.properties.route != null) state.map.setFeatureState({ source: 'planner-streets', id: edge }, { erase: true });
      } else if (state.data.activeColor && Number(hit.properties.boxes) > 0) {
        state.map.setFeatureState({ source: 'planner-streets', id: edge }, { paint: state.data.activeColor });
      }
    };

    const previewCut = (el, point) => {
      const state = el.gsbcPlanner;
      const result = JSON.parse(state.dotnet.invokeMethod('CutPreview', flat(state.line)));
      state.map.removeFeatureState({ source: 'planner-streets' });
      const cutting = new Set(result.routes.map((r) => r.route));
      for (const f of state.data.streets?.features ?? []) {
        if (f.properties.route != null && !cutting.has(f.properties.route)) state.map.setFeatureState({ source: 'planner-streets', id: f.properties.edge }, { dim: true });
      }
      for (const r of result.routes) {
        r.kept.forEach((e) => state.map.setFeatureState({ source: 'planner-streets', id: e }, { paint: r.keptColor }));
        r.pieces.forEach((p) => p.edges.forEach((e) => state.map.setFeatureState({ source: 'planner-streets', id: e }, { paint: p.color })));
      }
      showTip(state, point, result.routes.length > 0
        ? result.routes.flatMap((r) => [[r.keptColor, `${r.keptBoxes}`], ...r.pieces.map((p) => [p.color, `${p.boxes}`]), [null, 'letterboxes']])
        : result.touches
          ? [[null, 'Still joined elsewhere: keep cutting across the streets between']]
          : [[null, 'Drag across the streets where the split goes']]);
    };

    const previewLasso = (el, point) => {
      const state = el.gsbcPlanner;
      const result = JSON.parse(state.dotnet.invokeMethod('LassoPreview', flat(state.line)));
      state.map.removeFeatureState({ source: 'planner-streets' });
      result.edges.forEach((e) => state.map.setFeatureState({ source: 'planner-streets', id: e }, { paint: result.color }));
      showTip(state, point, [[result.color, `${result.edges.length} streets, ${result.boxes} letterboxes into ${result.into}`]]);
    };

    const drawGesture = (state, closed) => {
      const coords = state.line.map((p) => [p.lng, p.lat]);
      const features = [{ type: 'Feature', properties: {}, geometry: { type: 'LineString', coordinates: closed && coords.length > 2 ? [...coords, coords[0]] : coords } }];
      if (closed && coords.length > 2) features.push({ type: 'Feature', properties: {}, geometry: { type: 'Polygon', coordinates: [[...coords, coords[0]]] } });
      state.map.getSource('planner-gesture')?.setData({ type: 'FeatureCollection', features });
    };

    const schedule = (el, work) => {
      const state = el.gsbcPlanner;
      state.pending = work;
      if (state.frame) return;
      state.frame = requestAnimationFrame(() => {
        state.frame = 0;
        const next = state.pending;
        state.pending = null;
        next?.();
      });
    };

    const panning = (state) => state.space || state.mode !== 'select' || !drawingTools.includes(state.tool);

    const bindGestures = (el) => {
      const state = el.gsbcPlanner;
      const map = state.map;
      const box = map.getCanvasContainer();
      const local = (e) => {
        const r = box.getBoundingClientRect();
        return new maplibregl.Point(e.clientX - r.left, e.clientY - r.top);
      };

      box.addEventListener('pointerdown', (e) => {
        if (e.button !== 0 || state.mode !== 'select' || !state.routes) return;
        const point = local(e);
        if (state.tool === 'start' && !state.space) {
          const pin = map.queryRenderedFeatures([[point.x - 10, point.y - 10], [point.x + 10, point.y + 10]], { layers: ['planner-starts'] })[0];
          if (!pin) return;
          state.drag = { route: Number(pin.properties.route) };
        } else if (panning(state)) {
          return;
        } else {
          state.gesture = state.tool;
          state.line = [map.unproject(point)];
          state.last = point;
          if (state.tool === 'brush' || state.tool === 'eraser') {
            state.stroke = new Set();
            strokeAt(el, point);
          }
        }
        e.preventDefault();
        e.stopPropagation();
        box.setPointerCapture(e.pointerId);
      }, true);

      box.addEventListener('pointermove', (e) => {
        const point = local(e);
        if (state.drag) {
          const lngLat = map.unproject(point);
          const snapped = state.dotnet.invokeMethod('SnapStart', state.drag.route, lngLat.lat, lngLat.lng);
          if (snapped.length === 2) {
            state.drag.at = snapped;
            const features = (state.data.starts?.features ?? []).map((f) => f.properties.route === state.drag.route ? { ...f, geometry: { type: 'Point', coordinates: snapped } } : f);
            map.getSource('planner-starts')?.setData({ type: 'FeatureCollection', features });
          }
          return;
        }
        if (!state.gesture) {
          if (state.mode !== 'select' || !state.routes || e.buttons) return;
          schedule(el, () => hoverAt(el, point));
          return;
        }
        if (state.gesture === 'brush' || state.gesture === 'eraser') {
          const steps = Math.max(1, Math.ceil(Math.hypot(point.x - state.last.x, point.y - state.last.y) / 5));
          for (let i = 1; i <= steps; i++) strokeAt(el, new maplibregl.Point(state.last.x + (point.x - state.last.x) * i / steps, state.last.y + (point.y - state.last.y) * i / steps));
          state.last = point;
          return;
        }
        if (Math.hypot(point.x - state.last.x, point.y - state.last.y) < 4) return;
        state.last = point;
        state.line.push(map.unproject(point));
        drawGesture(state, state.gesture === 'lasso');
        if (state.gesture === 'scissors' && state.line.length >= 2) schedule(el, () => state.gesture && previewCut(el, point));
        if (state.gesture === 'lasso' && state.line.length >= 3) schedule(el, () => state.gesture && previewLasso(el, point));
      });

      const finish = (e) => {
        if (state.drag) {
          const drag = state.drag;
          state.drag = null;
          if (drag.at) state.dotnet.invokeMethodAsync('StartMoved', drag.route, drag.at[1], drag.at[0]);
          return;
        }
        const gesture = state.gesture;
        if (!gesture || e.type === 'pointercancel') { state.gesture = null; clearPreview(el); return; }
        state.gesture = null;
        state.held = true;
        if (gesture === 'brush' || gesture === 'eraser') {
          state.dotnet.invokeMethodAsync('StrokeFinished', gesture, [...state.stroke]);
        } else if (gesture === 'scissors' && state.line.length >= 2) {
          state.dotnet.invokeMethodAsync('CutFinished', flat(state.line));
        } else if (gesture === 'lasso' && state.line.length >= 3) {
          state.dotnet.invokeMethodAsync('LassoFinished', flat(state.line));
        } else {
          clearPreview(el);
        }
      };
      box.addEventListener('pointerup', finish);
      box.addEventListener('pointercancel', finish);
      box.addEventListener('pointerleave', () => { if (!state.gesture && !state.drag) setHover(el, -1); });
    };

    const hoverAt = (el, point) => {
      const state = el.gsbcPlanner;
      const map = state.map;
      setHover(el, routeAt(map, point));
      if (state.tool !== 'brush' && state.tool !== 'eraser') return;
      const hit = edgeAt(map, point);
      const edge = hit ? Number(hit.properties.edge) : null;
      if (edge === state.hoverEdge) return;
      if (state.hoverEdge != null) map.setFeatureState({ source: 'planner-streets', id: state.hoverEdge }, { hover: false, paint: null, erase: false });
      state.hoverEdge = edge;
      if (edge == null) return;
      if (state.tool === 'brush' && state.data.activeColor && Number(hit.properties.boxes) > 0) map.setFeatureState({ source: 'planner-streets', id: edge }, { hover: true, paint: state.data.activeColor });
      if (state.tool === 'eraser' && hit.properties.route != null) map.setFeatureState({ source: 'planner-streets', id: edge }, { erase: true });
    };

    const isTyping = (target) => target instanceof HTMLElement && (target.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName));
    const toolKeys = { v: 'select', c: 'scissors', b: 'brush', l: 'lasso', e: 'eraser', s: 'start', f: 'fit' };

    const bindKeys = (el) => {
      const state = el.gsbcPlanner;
      const send = (key) => state.dotnet.invokeMethodAsync('Shortcut', key);
      state.onKey = (e) => {
        if (!state.routes || state.mode !== 'select' || isTyping(e.target)) return;
        const key = e.key.toLowerCase();
        if (e.type === 'keyup') {
          if (key === ' ') { state.space = false; el.classList.remove('planner-map--panning'); }
          return;
        }
        if (key === ' ') {
          e.preventDefault();
          state.space = true;
          el.classList.add('planner-map--panning');
        } else if (key === 'escape') {
          state.gesture = null;
          state.drag = null;
          clearPreview(el);
          send('escape');
        } else if ((e.metaKey || e.ctrlKey) && key === 'z') {
          e.preventDefault();
          send(e.shiftKey ? 'redo' : 'undo');
        } else if (e.ctrlKey && key === 'y') {
          e.preventDefault();
          send('redo');
        } else if (!e.metaKey && !e.ctrlKey && !e.altKey && toolKeys[key]) {
          send(toolKeys[key]);
        }
      };
      document.addEventListener('keydown', state.onKey);
      document.addEventListener('keyup', state.onKey);
    };

    const planner = {
      async attach(el, dotnet, lat, lon, attribution) {
        await load();
        const map = createMap(el, lat, lon, attribution);
        const tip = document.createElement('div');
        tip.className = 'planner-tip';
        tip.hidden = true;
        el.parentElement.append(tip);
        el.gsbcPlanner = { map, dotnet, data: null, mode: 'select', tool: 'select', points: 0, first: null, hover: -1, overlay: el.parentElement.querySelector('.planner-overlay'), tip };
        map.doubleClickZoom.disable();
        map.boxZoom.disable();
        map.on('style.load', () => addPlannerLayers(el));
        map.on('move', () => place(el));
        bindGestures(el);
        bindKeys(el);
        const overlay = el.gsbcPlanner.overlay;
        overlay?.addEventListener('pointerover', (e) => {
          const tag = e.target.closest?.('[data-route]');
          if (tag) setHover(el, Number(tag.dataset.route));
        });
        overlay?.addEventListener('pointerout', (e) => {
          if (e.target.closest?.('[data-route]') && !e.relatedTarget?.closest?.('[data-route]')) setHover(el, -1);
        });
        map.on('click', (e) => {
          const state = el.gsbcPlanner;
          if (state.mode === 'draw') {
            if (state.first && state.points >= 3) {
              const first = map.project(state.first);
              if (Math.hypot(first.x - e.point.x, first.y - e.point.y) < 14) { dotnet.invokeMethodAsync('DrawFinished'); return; }
            }
            dotnet.invokeMethodAsync('MapClicked', e.lngLat.lat, e.lngLat.lng);
          } else if (state.mode === 'meeting') {
            dotnet.invokeMethodAsync('MeetingSet', e.lngLat.lat, e.lngLat.lng);
          } else if (state.tool === 'select' && state.routes) {
            dotnet.invokeMethodAsync('RoutePicked', routeAt(map, e.point), e.originalEvent.shiftKey);
          }
        });
        map.on('dblclick', (e) => {
          if (el.gsbcPlanner.mode !== 'draw') return;
          e.preventDefault();
          dotnet.invokeMethodAsync('DrawFinished');
        });
      },

      update(el, json, mode, tool, fit) {
        const state = el.gsbcPlanner;
        if (!state) return;
        state.data = JSON.parse(json);
        state.mode = mode;
        if (state.tool !== tool) { state.gesture = null; clearPreview(el); }
        state.tool = tool;
        state.points = state.data.points ?? 0;
        state.first = state.data.first ?? null;
        state.routes = (state.data.starts?.features ?? []).length > 0;
        el.classList.toggle('planner-map--drawing', mode !== 'select');
        for (const t of ['scissors', 'lasso', 'brush', 'eraser', 'start']) el.classList.toggle(`planner-map--${t}`, mode === 'select' && state.routes && tool === t);
        const apply = () => {
          for (const name of ['area', 'streets', 'starts', 'meeting', 'zones', 'pins']) state.map.getSource(`planner-${name}`)?.setData(state.data[name] ?? empty);
          if (state.held) { state.held = false; clearPreview(el); }
          emphasise(el);
          place(el);
          state.overlay?.querySelectorAll('[data-route]').forEach((n) => n.toggleAttribute('data-hover', Number(n.dataset.route) === state.hover));
          if (state.data.focus && state.data.focusVersion !== state.focusVersion) {
            state.focusVersion = state.data.focusVersion;
            state.map.easeTo({ center: state.data.focus, zoom: 17, duration: 600 });
          }
          if (fit) {
            const coords = [...(state.data.streets?.features ?? []).flatMap((f) => f.geometry.coordinates), ...(state.data.area?.features ?? []).filter((f) => f.geometry.type === 'Point').map((f) => f.geometry.coordinates)];
            if (coords.length > 1) state.map.fitBounds(coords.reduce((b, c) => b.extend(c), new maplibregl.LngLatBounds(coords[0], coords[0])), { padding: { top: 72, bottom: 48, left: 88, right: 48 }, maxZoom: 17, duration: 500 });
          }
        };
        place(el);
        if (state.map.isStyleLoaded() && state.map.getSource('planner-area')) apply();
        else state.map.once('idle', apply);
      },

      detach(el) {
        const state = el.gsbcPlanner;
        if (!state) return;
        document.removeEventListener('keydown', state.onKey);
        document.removeEventListener('keyup', state.onKey);
        state.tip.remove();
        state.map.remove();
        el.gsbcPlanner = null;
      },
    };

    return { street, planner };
  })();

  let wakeLock = null;
  let wantAwake = false;
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible' && wantAwake) window.gsbc.keepAwake(true);
  });

  window.gsbc = {
    isNative,
    platform,
    isMac: () => /Mac|iPhone|iPad/.test(navigator.platform),
    streetMap: maps.street,
    plannerMap: maps.planner,

    async readPhoto(input, maxSize) {
      const file = input?.files?.[0];
      if (!file) return null;
      try {
        const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' });
        const scale = Math.min(1, maxSize / Math.max(bitmap.width, bitmap.height));
        const canvas = document.createElement('canvas');
        canvas.width = Math.round(bitmap.width * scale);
        canvas.height = Math.round(bitmap.height * scale);
        canvas.getContext('2d').drawImage(bitmap, 0, 0, canvas.width, canvas.height);
        bitmap.close();
        return canvas.toDataURL('image/jpeg', 0.8);
      } catch {
        return null;
      }
    },

    async keepAwake(on) {
      wantAwake = on;
      if (document.visibilityState === 'visible' && on && wakeLock?.released) wakeLock = null;
      try {
        if (on && !wakeLock && navigator.wakeLock) wakeLock = await navigator.wakeLock.request('screen');
        if (!on && wakeLock) { await wakeLock.release(); wakeLock = null; }
      } catch { wakeLock = null; }
    },

    displayMode() {
      if (isNative()) return 'native';
      if (matchMedia('(display-mode: standalone)').matches || navigator.standalone) return 'standalone';
      return /iPhone|iPad|iPod/.test(navigator.userAgent) || (navigator.maxTouchPoints > 1 && /Macintosh/.test(navigator.userAgent))
        ? 'browser-ios'
        : installPrompt ? 'browser-installable' : 'browser';
    },

    async install() {
      if (!installPrompt) return false;
      installPrompt.prompt();
      const { outcome } = await installPrompt.userChoice;
      installPrompt = null;
      return outcome === 'accepted';
    },

    async haptic(kind) {
      const haptics = plugin('Haptics');
      if (!haptics) return 'unavailable';
      try {
        if (kind === 'success' || kind === 'warning') await haptics.notification({ type: kind.toUpperCase() });
        else await haptics.impact({ style: kind === 'medium' ? 'MEDIUM' : 'LIGHT' });
        return 'ok';
      } catch (e) {
        return `error: ${e?.message ?? e}`;
      }
    },

    async share(title, text, url) {
      const share = plugin('Share');
      if (share) return share.share({ title, text, url, dialogTitle: title }).catch(() => { });
      if (navigator.share) return navigator.share({ title, text, url }).catch(() => { });
      return navigator.clipboard?.writeText(url);
    },

    async openInApp(url) {
      const browser = plugin('Browser');
      if (browser) return browser.open({ url, presentationStyle: 'popover' });
      window.open(url, '_blank', 'noopener');
    },

    openExternal(url) {
      if (isNative()) window.location.href = url;
      else window.open(url, '_blank', 'noopener');
    },

    async addToCalendar(title, startMs, endMs, location, notes) {
      const calendar = plugin('CapacitorCalendar');
      if (!calendar) return 'web';
      try {
        const result = await calendar.createEventWithPrompt({ title, startDate: startMs, endDate: endMs, location, description: notes, alerts: [-60] });
        return result?.id ? 'added' : 'cancelled';
      } catch (e) {
        return `error: ${e?.message ?? e}`;
      }
    },

    async scheduleNotification(title, body, secondsFromNow) {
      const notifications = plugin('LocalNotifications');
      if (!notifications) return 'web';
      try {
        const permission = await notifications.requestPermissions();
        if (permission.display !== 'granted') return `permission: ${permission.display}`;
        await notifications.schedule({
          notifications: [{ id: Date.now() % 2147483647, title, body, schedule: { at: new Date(Date.now() + secondsFromNow * 1000) } }],
        });
        return 'scheduled';
      } catch (e) {
        return `error: ${e?.message ?? e}`;
      }
    },

    get: storage.get,
    set: storage.set,

    applyTheme(theme, appearance, layout) {
      const root = document.documentElement;
      root.dataset.theme = theme === 'Year2026' ? 'year' : 'brand';
      root.dataset.appearance = appearance;
      root.dataset.layout = layout;
      applyScheme();
      document.querySelector('meta[name="theme-color"]')?.setAttribute('content', getComputedStyle(root).getPropertyValue('--masthead').trim());
    },

    markFirstRender() {
      window.__gsbcFirstRender ??= performance.now();
      plugin('SplashScreen')?.hide({ fadeOutDuration: 200 });
      plugin('StatusBar')?.setStyle({ style: 'DARK' });
    },

    back(fallback) {
      if (history.state?._index > 0) history.back();
      else window.Blazor.navigateTo(fallback);
    },

    scrollToSection(id) {
      const target = document.getElementById(id);
      const scroller = scrollerOf(target);
      if (!scroller) return;
      const top = scroller.scrollTop + target.getBoundingClientRect().top - scroller.getBoundingClientRect().top - stuckHeight(scroller) - 12;
      scroller.scrollTo({ top, behavior: 'smooth' });
    },

    scrollToTop() {
      document.querySelector('.page-scroll')?.scrollTo({ top: 0, behavior: 'smooth' });
    },

    spySections(nav) {
      const scroller = scrollerOf(nav);
      if (!scroller) return;
      const buttons = [...nav.querySelectorAll('button[data-target]')];
      const ink = nav.querySelector('.section-tabs__ink');
      let last = null;
      const update = () => {
        const line = nav.getBoundingClientRect().bottom + 24;
        let current = buttons[0];
        for (const b of buttons) {
          const target = document.getElementById(b.dataset.target);
          if (target && target.getBoundingClientRect().top <= line) current = b;
        }
        if (scroller.scrollTop > 0 && scroller.scrollTop + scroller.clientHeight >= scroller.scrollHeight - 4) current = buttons[buttons.length - 1];
        if (current === last) return;
        last = current;
        buttons.forEach((b) => b.classList.toggle('on', b === current));
        if (ink) {
          ink.style.width = `${current.offsetWidth}px`;
          ink.style.transform = `translateX(${current.offsetLeft}px)`;
        }
        nav.scrollTo({ left: current.offsetLeft - (nav.clientWidth - current.offsetWidth) / 2, behavior: 'smooth' });
      };
      scroller.addEventListener('scroll', update, { passive: true });
      update();
    },

    positions() {
      const out = {};
      try {
        for (let i = 0; i < localStorage.length; i++) {
          const key = localStorage.key(i);
          if (!key?.startsWith('gsbc:position:')) continue;
          const id = key.slice(14);
          const position = Number(localStorage.getItem(key)) || 0;
          const length = Number(localStorage.getItem(`gsbc:length:${id}`)) || 0;
          if (position > 30 && length > 0 && position < length - 30) out[id] = position / length;
        }
      } catch { }
      return JSON.stringify(out);
    },

    player: {
      listen(dotnet) { player.listener = dotnet; },
      load(id, src, title, artist, artwork, startAt) {
        const a = player.audio;
        if (player.id !== id) {
          player.id = id;
          a.src = src;
          a.addEventListener('loadedmetadata', () => { if (startAt > 0) a.currentTime = startAt; }, { once: true });
        }
        if ('mediaSession' in navigator) {
          navigator.mediaSession.metadata = new MediaMetadata({ title, artist, album: 'Good Shepherd Baptist Church', artwork: artwork ? [{ src: artwork, sizes: '1920x1080' }] : [] });
          navigator.mediaSession.setActionHandler('play', () => a.play());
          navigator.mediaSession.setActionHandler('pause', () => a.pause());
          navigator.mediaSession.setActionHandler('seekbackward', () => { a.currentTime = Math.max(0, a.currentTime - 15); });
          navigator.mediaSession.setActionHandler('seekforward', () => { a.currentTime += 30; });
        }
        return a.play().then(() => true).catch(() => false);
      },
      toggle() {
        const a = player.audio;
        if (!player.id) return Promise.resolve(false);
        if (a.paused) return a.play().then(() => true).catch(() => false);
        a.pause();
        return Promise.resolve(false);
      },
      seek(seconds) { player.audio.currentTime = Math.max(0, Math.min(player.audio.duration || 0, seconds)); },
      skip(delta) { player.audio.currentTime = Math.max(0, player.audio.currentTime + delta); },
    },

    map: {
      attach(viewport, dotnet) {
        const canvas = viewport.querySelector('.site-map__canvas');
        const view = { scale: 1, x: 0, y: 0 };
        const pointers = new Map();
        let gesture = null;
        let moved = false;
        let lastTap = 0;

        const minScale = () => Math.max(1, viewport.clientHeight / canvas.offsetHeight);
        const bound = (scale) => Math.min(minScale() * 4, Math.max(minScale(), scale));
        const clamp = () => {
          view.scale = bound(view.scale);
          const w = viewport.clientWidth, h = viewport.clientHeight;
          const cw = canvas.offsetWidth * view.scale, ch = canvas.offsetHeight * view.scale;
          view.x = cw <= w ? (w - cw) / 2 : Math.min(0, Math.max(w - cw, view.x));
          view.y = ch <= h ? (h - ch) / 2 : Math.min(0, Math.max(h - ch, view.y));
        };
        const apply = (animate) => {
          clamp();
          canvas.style.transition = animate ? 'transform 320ms cubic-bezier(0.22, 1, 0.36, 1)' : 'none';
          canvas.style.transform = `translate(${view.x}px, ${view.y}px) scale(${view.scale})`;
          canvas.style.setProperty('--inv-zoom', Math.min(1, 1.5 / view.scale));
        };
        const local = (e) => {
          const r = viewport.getBoundingClientRect();
          return { x: e.clientX - r.left, y: e.clientY - r.top };
        };
        const zoomAt = (point, scale, animate) => {
          const contentX = (point.x - view.x) / view.scale, contentY = (point.y - view.y) / view.scale;
          view.scale = bound(scale);
          view.x = point.x - contentX * view.scale;
          view.y = point.y - contentY * view.scale;
          apply(animate);
        };
        const start = () => {
          const [a, b] = [...pointers.values()];
          gesture = b
            ? { mid: { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 }, distance: Math.hypot(a.x - b.x, a.y - b.y), ...view }
            : { origin: { ...a }, ...view };
        };
        const tap = (e) => {
          const target = document.elementFromPoint(e.clientX, e.clientY);
          const mark = target?.closest('[data-mark]');
          const space = target?.closest('[data-space]');
          if (mark) dotnet.invokeMethodAsync('Tapped', 'mark', mark.dataset.mark);
          else if (space) dotnet.invokeMethodAsync('Tapped', 'space', space.dataset.space);
        };

        viewport.addEventListener('pointerdown', (e) => {
          viewport.setPointerCapture(e.pointerId);
          pointers.set(e.pointerId, local(e));
          if (pointers.size === 1) moved = false;
          start();
        });
        viewport.addEventListener('pointermove', (e) => {
          if (!pointers.has(e.pointerId)) return;
          pointers.set(e.pointerId, local(e));
          const [a, b] = [...pointers.values()];
          if (b && gesture.distance) {
            const mid = { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 };
            const scale = bound(gesture.scale * Math.hypot(a.x - b.x, a.y - b.y) / gesture.distance);
            const contentX = (gesture.mid.x - gesture.x) / gesture.scale, contentY = (gesture.mid.y - gesture.y) / gesture.scale;
            Object.assign(view, { scale, x: mid.x - contentX * scale, y: mid.y - contentY * scale });
            moved = true;
          } else if (gesture.origin) {
            const dx = a.x - gesture.origin.x, dy = a.y - gesture.origin.y;
            if (Math.hypot(dx, dy) > 6) moved = true;
            Object.assign(view, { x: gesture.x + dx, y: gesture.y + dy });
          }
          apply(false);
        });
        const end = (e) => {
          if (!pointers.delete(e.pointerId)) return;
          if (pointers.size > 0) { start(); return; }
          if (moved || e.type === 'pointercancel') return;
          const now = performance.now();
          if (now - lastTap < 300) {
            lastTap = 0;
            zoomAt(local(e), view.scale > minScale() * 1.8 ? minScale() : view.scale * 2.2, true);
            return;
          }
          lastTap = now;
          tap(e);
        };
        viewport.addEventListener('pointerup', end);
        viewport.addEventListener('pointercancel', end);
        viewport.addEventListener('wheel', (e) => {
          e.preventDefault();
          zoomAt(local(e), view.scale * Math.exp(-e.deltaY / 300), false);
        }, { passive: false });
        new ResizeObserver(() => apply(false)).observe(viewport);
        viewport.gsbcMap = { view, apply, zoomAt };
        view.scale = minScale();
        view.x = (viewport.clientWidth - canvas.offsetWidth * view.scale) / 2;
        apply(false);
      },

      focus(viewport, fx, fy, scale) {
        const map = viewport.gsbcMap;
        if (!map) return;
        const canvas = viewport.querySelector('.site-map__canvas');
        map.view.scale = scale;
        map.view.x = viewport.clientWidth / 2 - fx * canvas.offsetWidth * scale;
        map.view.y = viewport.clientHeight / 2 - fy * canvas.offsetHeight * scale;
        map.apply(true);
      },
    },

    updates: {
      waiting: false,
      listener: null,
      lastCheck: 0,
      watch(registration) {
        const updates = window.gsbc.updates;
        updates.lastCheck = Date.now();
        const installed = () => navigator.serviceWorker.controller && updates.ready();
        if (registration.waiting) installed();
        registration.addEventListener('updatefound', () => {
          const next = registration.installing;
          next?.addEventListener('statechange', () => { if (next.state === 'installed') installed(); });
        });
        const check = () => {
          if (Date.now() - updates.lastCheck < 30_000) return;
          updates.lastCheck = Date.now();
          registration.update().catch(() => { });
        };
        document.addEventListener('visibilitychange', () => { if (document.visibilityState === 'visible') check(); });
        setInterval(check, 5 * 60_000);
      },
      ready() {
        const updates = window.gsbc.updates;
        if (updates.waiting) return;
        updates.waiting = true;
        updates.listener?.invokeMethodAsync('UpdateReady');
      },
      onReady(listener) {
        const updates = window.gsbc.updates;
        updates.listener = listener;
        if (updates.waiting) listener.invokeMethodAsync('UpdateReady');
      },
      async apply() {
        try {
          const registration = await navigator.serviceWorker?.getRegistration();
          const next = registration?.waiting ?? registration?.installing;
          if (next) {
            const switched = new Promise((resolve) => {
              navigator.serviceWorker.addEventListener('controllerchange', resolve, { once: true });
              setTimeout(resolve, 10_000);
            });
            if (next.state === 'installing') {
              await new Promise((resolve) => next.addEventListener('statechange', () => next.state !== 'installing' && resolve()));
            }
            next.postMessage('skip-waiting');
            await switched;
          }
        } catch { }
        location.reload();
      },
    },

    async probe(name) {
      try {
        switch (name) {
          case 'runtime':
            return `${isNative() ? `Capacitor ${platform()}` : 'Browser'}`;
          case 'plugins': {
            const names = ['Haptics', 'Share', 'Browser', 'LocalNotifications', 'CapacitorCalendar', 'StatusBar', 'SplashScreen'];
            if (!isNative()) return 'Browser: native plugins not present';
            const missing = names.filter((n) => !cap().isPluginAvailable?.(n));
            return missing.length ? `Missing: ${missing.join(', ')}` : `All ${names.length} available`;
          }
          case 'startup': {
            const nav = performance.getEntriesByType('navigation')[0];
            const firstPaint = window.__gsbcFirstRender ?? 0;
            return `${Math.round(firstPaint)} ms to first screen${nav ? ` · ${Math.round(nav.duration)} ms page load` : ''}`;
          }
          case 'safe-area': {
            const probe = document.createElement('div');
            probe.style.cssText = 'position:fixed;top:0;padding-top:env(safe-area-inset-top);padding-bottom:env(safe-area-inset-bottom);visibility:hidden';
            document.body.appendChild(probe);
            const style = getComputedStyle(probe);
            const result = `top ${style.paddingTop} · bottom ${style.paddingBottom} · window ${innerHeight} of ${screen.height} · ${window.gsbc.displayMode()}`;
            probe.remove();
            return result;
          }
          case 'storage': {
            const estimate = await navigator.storage?.estimate?.();
            const persisted = await navigator.storage?.persisted?.();
            return estimate ? `${(estimate.usage / 1048576).toFixed(1)} MB used · persisted: ${persisted}` : 'No StorageManager';
          }
          default:
            return 'Unknown probe';
        }
      } catch (e) {
        return `error: ${e?.message ?? e}`;
      }
    },
  };

  document.addEventListener('scroll', (e) => {
    const scroller = e.target;
    if (!(scroller instanceof Element) || !scroller.classList.contains('page-scroll')) return;
    scroller.querySelector('.topbar')?.classList.toggle('is-stuck', scroller.scrollTop > (scroller.dataset.stuckAt ?? 44));
  }, { capture: true, passive: true });

  window.addEventListener('DOMContentLoaded', () => {
    plugin('App')?.addListener('backButton', () => history.length > 1 && history.back());
  });

  const fillStandaloneScreen = () => {
    const portrait = matchMedia('(orientation: portrait)').matches;
    const [short, long] = [screen.width, screen.height].sort((a, b) => a - b);
    document.documentElement.style.setProperty('--screen-h', `${portrait ? long : short}px`);
  };
  if (window.gsbc.displayMode() === 'standalone') {
    fillStandaloneScreen();
    addEventListener('resize', fillStandaloneScreen);
  }

  if (!isNative() && 'serviceWorker' in navigator && location.protocol === 'https:') {
    navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' })
      .then((registration) => window.gsbc.updates.watch(registration))
      .catch(() => { });
  }
})();
