const startAfter = 5;
const glide = 220;
const settle = 130;
const leeway = 0.2;
const mostRows = 4;
const edgeZone = 72;
const fastestScroll = 9;
const glideCurve = 'cubic-bezier(0.2, 0.8, 0.2, 1)';
const dropCurve = 'linear(0, 0.3 8%, 0.62 17%, 0.88 27%, 1.03 40%, 1.012 55%, 0.998 72%, 1)';

const still = () => matchMedia('(prefers-reduced-motion: reduce)').matches;

export function pack(columns, spans) {
  const taken = [];
  const fits = (column, row, width, height) => {
    for (let y = row; y < row + height && y < taken.length; y++)
      for (let x = column; x < column + width; x++)
        if (taken[y][x]) return false;
    return true;
  };
  return spans.map(({ columns: wide, rows: tall }) => {
    const width = Math.max(1, Math.min(wide, columns));
    const height = Math.max(1, Math.min(tall, mostRows));
    for (let row = 0; ; row++) {
      for (let column = 0; column + width <= columns; column++) {
        if (!fits(column, row, width, height)) continue;
        while (taken.length < row + height) taken.push(new Array(columns).fill(false));
        for (let y = row; y < row + height; y++)
          for (let x = column; x < column + width; x++) taken[y][x] = true;
        return { column: column + 1, row: row + 1, columns: width, rows: height };
      }
    }
  });
}

export function attach(canvas, receiver) {
  let gesture = null;
  let landing = null;
  let pending = null;
  let healing = false;

  const cellOf = (element) => element?.closest?.('.hub-cell[data-tile]') ?? null;
  const cells = () => [...canvas.querySelectorAll('.hub-cell[data-tile]')];
  const byKey = (key) => canvas.querySelector(`.hub-cell[data-tile="${CSS.escape(key)}"]`);
  const screen = () => canvas.querySelector('.phone-frame__screen');
  const frame = () => canvas.querySelector('.phone-frame');
  const grid = () => canvas.querySelector('.hub-grid');
  const tracks = (text) => text.split(' ').map(parseFloat).filter((value) => !Number.isNaN(value));
  const spanOf = (cell) => ({ columns: Number(cell.dataset.columns) || 1, rows: Number(cell.dataset.rows) || 1 });

  const measure = (cell) => {
    const style = getComputedStyle(cell.parentElement);
    const across = tracks(style.gridTemplateColumns);
    const track = across[0];
    const gap = parseFloat(style.columnGap) || 0;
    const span = Number(cell.dataset.columns);
    const scale = cell.getBoundingClientRect().width / (span * track + (span - 1) * gap) || 1;
    const row = Math.min(132, Math.max(88, track));
    return { columns: across.length, scale, track, row, gap, columnStep: (track + gap) * scale, rowStep: (row + gap) * scale };
  };

  const home = (cell) => {
    const transform = cell.style.transform;
    if (transform) cell.style.transform = 'none';
    const box = cell.getBoundingClientRect();
    if (transform) cell.style.transform = transform;
    return box;
  };

  const boxes = (except) => new Map(cells().filter((cell) => cell !== except).map((cell) => [cell, cell.getBoundingClientRect()]));

  const flip = (before, scale) => {
    if (still()) return;
    for (const [cell, was] of before) {
      const now = home(cell);
      const dx = (was.left - now.left) / scale;
      const dy = (was.top - now.top) / scale;
      if (Math.abs(dx) < 0.5 && Math.abs(dy) < 0.5) continue;
      cell.style.transition = 'none';
      cell.style.transform = `translate(${dx}px, ${dy}px)`;
      cell.getBoundingClientRect();
      cell.style.transition = `transform ${glide}ms ${glideCurve}`;
      cell.style.transform = '';
    }
  };

  const place = (cell, spot) => {
    const column = `${spot.column} / span ${spot.columns}`;
    const row = `${spot.row} / span ${spot.rows}`;
    if (cell.style.gridColumn !== column) cell.style.gridColumn = column;
    if (cell.style.gridRow !== row) cell.style.gridRow = row;
    if (cell.style.getPropertyValue('--tile-rows') !== String(spot.rows)) cell.style.setProperty('--tile-rows', spot.rows);
  };

  const lay = (order, spans, except, animate = true) => {
    const before = animate ? boxes(except) : null;
    const spots = pack(gesture.metrics.columns, order.map((key) => spans.get(key)));
    order.forEach((key, index) => {
      const cell = byKey(key);
      if (cell) place(cell, spots[index]);
    });
    if (before) flip(before, gesture.metrics.scale);
  };

  const settle = () => {
    if (pending && performance.now() > pending.until) pending = null;
    const found = pending ? pending.order.map(byKey).filter(Boolean) : cells();
    const columns = tracks(getComputedStyle(grid() ?? canvas).gridTemplateColumns).length || 2;
    const spots = pack(columns, found.map((cell) => pending?.spans.get(cell.dataset.tile) ?? spanOf(cell)));
    found.forEach((cell, index) => {
      place(cell, spots[index]);
      if (!landing || landing.cell !== cell) cell.classList.remove('hub-cell--lifted', 'hub-cell--settling');
      if (cell.style.transform && !cell.style.transition) cell.style.transform = '';
      if (cell.style.opacity) cell.style.opacity = '';
    });
    canvas.classList.remove('hub-canvas--moving', 'hub-canvas--sizing');
    const layer = frame()?.querySelector('.hub-drag-layer');
    if (layer && !landing && layer.childElementCount) layer.replaceChildren();
  };

  const heal = () => {
    if (healing) return;
    healing = true;
    try {
      if (gesture?.started) {
        if (gesture.kind === 'move') {
          gesture.spans = new Map(cells().map((found) => [found.dataset.tile, gesture.spans.get(found.dataset.tile) ?? spanOf(found)]));
          gesture.order = gesture.order.filter((key) => byKey(key));
          for (const found of cells()) if (!gesture.order.includes(found.dataset.tile)) gesture.order.push(found.dataset.tile);
          gesture.original = gesture.original.filter((key) => byKey(key));
          for (const found of cells()) if (!gesture.original.includes(found.dataset.tile)) gesture.original.push(found.dataset.tile);
          if (!byKey(gesture.key)) {
            abandon();
            return;
          }
          lay(gesture.order, gesture.spans, gesture.cell, false);
          gesture.cell.classList.add('hub-cell--lifted');
          canvas.classList.add('hub-canvas--moving');
        }
      } else {
        settle();
      }
    } finally {
      healing = false;
    }
  };

  let healSoon = null;
  const healLater = () => {
    if (healSoon) return;
    healSoon = requestAnimationFrame(() => {
      healSoon = null;
      heal();
    });
  };
  const watcher = new MutationObserver((records) => {
    if (pending && records.some((record) => record.type === 'childList' && record.target === grid())) pending = null;
    healLater();
  });
  let watched = null;
  const watch = () => {
    const target = grid();
    if (target === watched) return;
    watcher.disconnect();
    watched = target;
    if (target) watcher.observe(target, { childList: true, subtree: true, attributes: true, attributeFilter: ['style', 'class'] });
    healLater();
  };
  const rewatch = new MutationObserver(watch);
  rewatch.observe(canvas, { childList: true, subtree: true });
  watch();

  const inFrame = (x, y, scale) => {
    const box = frame().getBoundingClientRect();
    return { x: (x - box.left) / scale, y: (y - box.top) / scale };
  };

  const follow = () => {
    const { ghost, grab, last, metrics } = gesture;
    if (!ghost || !last) return;
    const { x, y } = inFrame(last.x - grab.x, last.y - grab.y, metrics.scale);
    ghost.style.transform = `translate(${x}px, ${y}px)`;
  };

  const raise = (cell, box) => {
    let layer = frame().querySelector('.hub-drag-layer');
    if (!layer) {
      layer = document.createElement('div');
      layer.className = 'hub-drag-layer';
      frame().append(layer);
    }
    const ghost = cell.cloneNode(true);
    ghost.classList.add('hub-drag');
    ghost.classList.remove('hub-cell--lifted', 'hub-cell--settling');
    ghost.removeAttribute('data-tile');
    ghost.setAttribute('inert', '');
    for (const image of ghost.querySelectorAll('img')) {
      const source = cell.querySelector(`img[src="${CSS.escape(image.getAttribute('src') ?? '')}"]`);
      image.loading = 'eager';
      image.decoding = 'sync';
      if (source?.currentSrc) image.src = source.currentSrc;
    }
    ghost.style.width = `${box.width / gesture.metrics.scale}px`;
    ghost.style.height = `${box.height / gesture.metrics.scale}px`;
    ghost.style.transition = 'none';
    layer.replaceChildren(ghost);
    return ghost;
  };

  const lift = (event) => {
    const cell = gesture.cell;
    const box = cell.getBoundingClientRect();
    gesture.started = true;
    gesture.metrics = measure(cell);
    gesture.order = cells().map((found) => found.dataset.tile);
    gesture.original = [...gesture.order];
    gesture.spans = new Map(cells().map((found) => [found.dataset.tile, spanOf(found)]));
    gesture.styles = new Map(cells().map((found) => [found, found.getAttribute('style')]));
    gesture.grab = { x: event.clientX - box.left, y: event.clientY - box.top };
    gesture.last = { x: event.clientX, y: event.clientY };
    gesture.scroll = screen()?.scrollTop ?? 0;
    try {
      gesture.handle.setPointerCapture(gesture.pointer);
    } catch {
    }
    gesture.handle.addEventListener('lostpointercapture', lost);
    getSelection()?.removeAllRanges();
    canvas.classList.add(gesture.kind === 'move' ? 'hub-canvas--moving' : 'hub-canvas--sizing');
    cell.classList.add('hub-cell--lifted');
    cell.style.transition = '';
    document.addEventListener('keydown', escape, true);
    addEventListener('blur', interrupted);
    document.addEventListener('visibilitychange', interrupted);
    if (gesture.kind === 'move') {
      gesture.ghost = raise(cell, box);
      follow();
      gesture.ghost.getBoundingClientRect();
      gesture.ghost.style.transition = '';
      gesture.ghost.classList.add('hub-drag--up');
      gesture.target = cellAt(anchor());
      gesture.frame = requestAnimationFrame(creep);
    }
  };

  const cellAt = ({ x, y }) => {
    const parent = gesture.cell.parentElement;
    const style = getComputedStyle(parent);
    const box = parent.getBoundingClientRect();
    const { scale } = gesture.metrics;
    const across = tracks(style.gridTemplateColumns);
    const down = tracks(style.gridTemplateRows);
    const gapX = (parseFloat(style.columnGap) || 0) * scale;
    const gapY = (parseFloat(style.rowGap) || 0) * scale;
    const find = (start, sizes, gap, at) => {
      let edge = start;
      for (let index = 0; index < sizes.length; index++) {
        const size = sizes[index] * scale;
        if (at < edge + size + gap / 2) return { index, from: edge, size };
        edge += size + gap;
      }
      const last = sizes.length ? sizes[sizes.length - 1] * scale : 0;
      return { index: sizes.length, from: edge, size: last || 1 };
    };
    const column = find(box.left, across, gapX, x);
    const row = find(box.top, down, gapY, y);
    return { column: Math.min(column.index, across.length - 1), row: row.index, box: { left: column.from, top: row.from, width: column.size, height: row.size } };
  };

  const anchor = () => {
    const { last, grab, metrics } = gesture;
    return { x: last.x - grab.x + (metrics.track * metrics.scale) / 2, y: last.y - grab.y + (metrics.row * metrics.scale) / 2 };
  };

  const ask = (asked, here) => {
    const spans = asked.original.map((each) => asked.spans.get(each));
    const args = ['Plan', asked.metrics.columns, spans.map((span) => span.columns), spans.map((span) => span.rows), asked.original.indexOf(asked.key), here.column, here.row];
    if (typeof receiver.invokeMethod === 'function') {
      try {
        return Promise.resolve(receiver.invokeMethod(...args));
      } catch {
      }
    }
    return receiver.invokeMethodAsync(...args);
  };

  const plan = async (asked, here) => {
    const indices = await ask(asked, here);
    if (gesture !== asked) return;
    asked.target = here;
    const order = indices.map((index) => asked.original[index]).filter((key) => key !== undefined);
    if (order.join() === asked.order.join()) return;
    asked.order = order;
    lay(order, asked.spans, asked.cell);
  };

  const retarget = () => {
    const here = cellAt(anchor());
    const held = gesture.target;
    if (held && held.column === here.column && held.row === here.row) {
      clearTimeout(gesture.timer);
      gesture.waiting = null;
      return;
    }
    if (held) {
      const { x, y } = anchor();
      const margin = { x: held.box.width * leeway, y: held.box.height * leeway };
      const inside = x > held.box.left - margin.x && x < held.box.left + held.box.width + margin.x
        && y > held.box.top - margin.y && y < held.box.top + held.box.height + margin.y;
      if (inside) return;
    }
    const wanted = `${here.column}:${here.row}`;
    if (gesture.waiting === wanted) return;
    clearTimeout(gesture.timer);
    gesture.waiting = wanted;
    gesture.timer = setTimeout(() => {
      if (!gesture || gesture.waiting !== wanted) return;
      gesture.waiting = null;
      const now = cellAt(anchor());
      if (`${now.column}:${now.row}` !== wanted) return;
      const asked = gesture;
      asked.planning = plan(asked, now).catch(() => {}).then(() => {
        if (gesture === asked) asked.target = cellAt(anchor());
      });
    }, settle);
  };

  const creep = () => {
    if (!gesture?.started || gesture.kind !== 'move') return;
    gesture.frame = requestAnimationFrame(creep);
    const view = screen();
    if (!view) return;
    const edge = view.getBoundingClientRect();
    const { scale } = gesture.metrics;
    const tabs = canvas.querySelector('.phone-frame .tabbar')?.getBoundingClientRect();
    const top = edge.top + ((parseFloat(getComputedStyle(view).getPropertyValue('--sat')) || 0) + 44) * scale;
    const bottom = tabs ? tabs.top : edge.bottom;
    const zone = edgeZone * scale;
    const y = gesture.last.y;
    let push = 0;
    if (y < top + zone) push = -Math.min(1, (top + zone - y) / zone);
    else if (y > bottom - zone) push = Math.min(1, (y - (bottom - zone)) / zone);
    const rest = () => {
      if (!gesture.scrolling) return;
      gesture.scrolling = false;
      retarget();
    };
    if (push === 0) {
      gesture.armed = true;
      rest();
      return;
    }
    if (!gesture.armed) {
      if (Math.abs(y - gesture.y) < gesture.metrics.rowStep / 2) return;
      gesture.armed = true;
    }
    const by = Math.sign(push) * fastestScroll * Math.abs(push) ** 1.6;
    const was = view.scrollTop;
    const box = gesture.cell.parentElement.getBoundingClientRect();
    const most = was + (box.bottom + (parseFloat(getComputedStyle(view).paddingBottom) || 0) * scale - edge.bottom) / scale;
    view.scrollTop = Math.max(0, Math.min(most, was + by));
    if (view.scrollTop === was) {
      rest();
      return;
    }
    gesture.scrolling = true;
    clearTimeout(gesture.timer);
    gesture.waiting = null;
  };

  const down = (event) => {
    if (event.button !== 0 || gesture) return;
    const handle = event.target.closest('[data-arrange]');
    if (!handle || !canvas.contains(handle)) return;
    const cell = cellOf(handle);
    if (!cell) return;
    gesture = {
      kind: event.target.closest('[data-grip]') ? 'resize' : 'move',
      cell,
      key: cell.dataset.tile,
      x: event.clientX,
      y: event.clientY,
      started: false,
      pointer: event.pointerId,
      handle,
    };
    if (gesture.kind === 'resize') event.preventDefault();
  };

  const move = (event) => {
    if (!gesture || event.pointerId !== gesture.pointer || gesture.ending) return;
    const dx = event.clientX - gesture.x;
    const dy = event.clientY - gesture.y;
    if (!gesture.started) {
      if (Math.hypot(dx, dy) < startAfter) return;
      lift(event);
    }
    gesture.last = { x: event.clientX, y: event.clientY };
    if (gesture.kind === 'move') {
      follow();
      if (!gesture.scrolling) retarget();
      return;
    }
    const { columns, columnStep, rowStep } = gesture.metrics;
    const was = gesture.spans.get(gesture.key);
    const start = gesture.size ?? was;
    gesture.size = start;
    const wide = Math.max(1, Math.min(columns, Math.round(start.columns + dx / columnStep)));
    const tall = Math.max(1, Math.min(mostRows, Math.round(start.rows + dy / rowStep)));
    if (gesture.next && gesture.next.columns === wide && gesture.next.rows === tall) return;
    gesture.next = { columns: wide, rows: tall };
    const spans = new Map(gesture.spans);
    spans.set(gesture.key, gesture.next);
    lay(gesture.order, spans, null);
    const badge = gesture.cell.querySelector('[data-size]');
    if (badge) badge.textContent = `${wide} wide, ${tall} tall`;
  };

  const tidy = (done) => {
    clearTimeout(done.timer);
    cancelAnimationFrame(done.frame);
    document.removeEventListener('keydown', escape, true);
    removeEventListener('blur', interrupted);
    document.removeEventListener('visibilitychange', interrupted);
    done.handle?.removeEventListener('lostpointercapture', lost);
    canvas.classList.remove('hub-canvas--moving', 'hub-canvas--sizing');
  };

  const land = (done, scrollTo) => new Promise((resolve) => {
    const { cell, ghost, metrics } = done;
    const view = screen();
    const finish = () => {
      ghost?.remove();
      cell.classList.remove('hub-cell--lifted');
      if (landing?.cell === cell) landing = null;
      resolve();
    };
    if (!ghost || !ghost.isConnected || !frame() || still()) {
      if (view && scrollTo !== undefined) view.scrollTop = scrollTo;
      finish();
      return;
    }
    landing = { cell };
    const box = home(cell);
    const { x, y } = inFrame(box.left, box.top, metrics.scale);
    const shift = view && scrollTo !== undefined ? scrollTo - view.scrollTop : 0;
    ghost.classList.remove('hub-drag--up');
    ghost.classList.add('hub-drag--landing');
    ghost.style.transition = `transform ${glide}ms ${scrollTo === undefined ? dropCurve : glideCurve}, scale ${glide}ms ${glideCurve}`;
    ghost.style.transform = `translate(${x}px, ${y - shift}px)`;
    if (view && scrollTo !== undefined) view.scrollTo({ top: scrollTo, behavior: 'smooth' });
    const began = performance.now();
    const settled = () => {
      const stillScrolling = view && scrollTo !== undefined && Math.abs(view.scrollTop - scrollTo) > 1;
      const elapsed = performance.now() - began;
      if (ghost.isConnected && (elapsed < glide || (stillScrolling && elapsed < 800))) requestAnimationFrame(settled);
      else finish();
    };
    requestAnimationFrame(settled);
  });

  const abandon = () => {
    const done = gesture;
    if (!done) return;
    gesture = null;
    tidy(done);
    if (!done.started) {
      done.cell.classList.remove('hub-cell--lifted');
      return;
    }
    const before = boxes(done.cell);
    for (const [cell, style] of done.styles) {
      if (!cell.isConnected) continue;
      if (style === null) cell.removeAttribute('style');
      else cell.setAttribute('style', style);
    }
    flip(before, done.metrics.scale);
    if (done.kind === 'move') land(done, done.scroll).then(healLater);
    else {
      done.cell.classList.remove('hub-cell--lifted');
      healLater();
    }
    const badge = done.cell.querySelector('[data-size]');
    if (badge && done.size) badge.textContent = `${done.size.columns} wide, ${done.size.rows} tall`;
  };

  function escape(event) {
    if (event.key !== 'Escape' || !gesture) return;
    event.preventDefault();
    event.stopPropagation();
    abandon();
  }

  function interrupted() {
    if (gesture?.started && !gesture.ending && (document.visibilityState === 'hidden' || !document.hasFocus())) abandon();
  }

  function lost(event) {
    if (gesture?.started && !gesture.ending && event.pointerId === gesture.pointer) abandon();
  }

  const outside = (event) => {
    const view = screen()?.getBoundingClientRect();
    return !view || event.clientX < view.left || event.clientX > view.right || event.clientY < view.top || event.clientY > view.bottom;
  };

  const finish = async (event) => {
    if (!gesture || event.pointerId !== gesture.pointer || gesture.ending) return;
    if (!gesture.started) {
      const key = gesture.key;
      gesture = null;
      await receiver.invokeMethodAsync('Chosen', key);
      return;
    }
    if (gesture.kind === 'move' && outside(event)) {
      abandon();
      return;
    }
    const done = gesture;
    done.ending = true;
    try {
      if (done.kind === 'move') {
        clearTimeout(done.timer);
        if (done.planning) await done.planning;
        if (gesture !== done) return;
        if (done.waiting || done.scrolling) {
          done.waiting = null;
          await plan(done, cellAt(anchor()));
          if (gesture !== done) return;
        }
      }
    } catch {
    }
    if (gesture !== done) return;
    gesture = null;
    tidy(done);
    if (done.kind === 'move') {
      const changed = done.order.join() !== done.original.join();
      if (changed) pending = { order: [...done.order], spans: done.spans, until: performance.now() + 1500 };
      await land(done);
      if (changed) {
        try {
          await receiver.invokeMethodAsync('Arranged', done.order, done.key);
        } catch {
          pending = null;
        }
      }
    } else {
      done.cell.classList.remove('hub-cell--lifted');
      const was = done.spans.get(done.key);
      if (done.next && (done.next.columns !== was.columns || done.next.rows !== was.rows)) {
        try {
          await receiver.invokeMethodAsync('Resized', done.key, done.next.columns, done.next.rows);
        } catch {
        }
      }
    }
    healLater();
    setTimeout(heal, 400);
  };

  const cancel = (event) => {
    if (!gesture || event.pointerId !== gesture.pointer) return;
    abandon();
  };

  const menu = (event) => {
    if (gesture?.started) event.preventDefault();
  };

  const keys = { ArrowLeft: [-1, 0], ArrowUp: [0, -1], ArrowRight: [1, 0], ArrowDown: [0, 1] };
  const key = async (event) => {
    const handle = event.target.closest('[data-arrange]');
    if (!handle || !canvas.contains(handle)) return;
    const tile = cellOf(handle)?.dataset.tile;
    if (!tile) return;
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      await receiver.invokeMethodAsync('Chosen', tile);
    } else if (keys[event.key]) {
      event.preventDefault();
      if (gesture) return;
      const [x, y] = keys[event.key];
      const scale = measure(cellOf(handle)).scale;
      const before = boxes(null);
      await receiver.invokeMethodAsync('Nudged', tile, x, y, event.shiftKey);
      requestAnimationFrame(() => {
        heal();
        flip(new Map([...before].filter(([cell]) => cell.isConnected)), scale);
        byKey(tile)?.querySelector('[data-arrange]')?.focus();
      });
    }
  };

  const ended = (event) => {
    if (event.propertyName === 'transform' && event.target.matches?.('.hub-cell')) event.target.style.transition = '';
  };

  canvas.addEventListener('keydown', key);
  canvas.addEventListener('pointerdown', down);
  canvas.addEventListener('pointermove', move);
  canvas.addEventListener('pointerup', finish);
  canvas.addEventListener('pointercancel', cancel);
  canvas.addEventListener('contextmenu', menu);
  canvas.addEventListener('transitionend', ended);
  return {
    dispose() {
      watcher.disconnect();
      rewatch.disconnect();
      cancelAnimationFrame(healSoon);
      canvas.removeEventListener('keydown', key);
      canvas.removeEventListener('pointerdown', down);
      canvas.removeEventListener('pointermove', move);
      canvas.removeEventListener('pointerup', finish);
      canvas.removeEventListener('pointercancel', cancel);
      canvas.removeEventListener('contextmenu', menu);
      canvas.removeEventListener('transitionend', ended);
      document.removeEventListener('keydown', escape, true);
      removeEventListener('blur', interrupted);
      document.removeEventListener('visibilitychange', interrupted);
      frame()?.querySelector('.hub-drag-layer')?.remove();
    },
  };
}

export function contrast(element) {
  const parse = (text) => {
    const values = (text.replace(/^color\(srgb/, '').match(/[\d.]+/g) || []).slice(0, 3).map(Number);
    return text.startsWith('color(srgb') ? values.map((value) => value * 255) : values;
  };
  const luminance = ([r, g, b]) => {
    const channel = (value) => {
      const c = value / 255;
      return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
    };
    return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
  };
  const style = getComputedStyle(element);
  const ink = luminance(parse(style.color));
  const ground = luminance(parse(style.backgroundColor));
  const ratio = (Math.max(ink, ground) + 0.05) / (Math.min(ink, ground) + 0.05);
  return Math.round(ratio * 10) / 10;
}

export async function readText(input, most) {
  const file = input.files && input.files[0];
  if (!file) return null;
  if (file.size > most) return { name: file.name, text: '', tooBig: true };
  return { name: file.name, text: await file.text(), tooBig: false };
}

export function contrastOf(selector) {
  const element = document.querySelector(selector);
  return element ? contrast(element) : 0;
}

export function reveal(selector) {
  const element = document.querySelector(selector);
  if (!element) return;
  element.scrollTop = 0;
  const box = element.getBoundingClientRect();
  if (box.top > innerHeight * 0.6) element.scrollIntoView({ behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block: 'start' });
}

export function show(canvas, key) {
  requestAnimationFrame(() => {
    const cell = canvas.querySelector(`.hub-cell[data-tile="${CSS.escape(key)}"]`);
    const screen = canvas.querySelector('.phone-frame__screen');
    const tabs = canvas.querySelector('.phone-frame .tabbar');
    if (!cell || !screen) return;
    const box = cell.getBoundingClientRect();
    const view = screen.getBoundingClientRect();
    const scale = view.height / screen.clientHeight || 1;
    const bottom = (tabs ? tabs.getBoundingClientRect().top : view.bottom) - 12 * scale;
    const top = view.top + ((parseFloat(getComputedStyle(screen).getPropertyValue('--sat')) || 0) + 44 + 12) * scale;
    const current = screen.scrollTop;
    let next = current;
    if (box.bottom > bottom) next = current + (box.bottom - bottom) / scale;
    if (box.top - (next - current) * scale < top) next -= (top - (box.top - (next - current) * scale)) / scale;
    next = Math.max(0, next);
    if (Math.abs(next - current) > 2) screen.scrollTo({ top: next, behavior: still() ? 'auto' : 'smooth' });
  });
}

export function scheme() {
  return document.documentElement.dataset.scheme === 'dark' ? 'Dark' : 'Light';
}
