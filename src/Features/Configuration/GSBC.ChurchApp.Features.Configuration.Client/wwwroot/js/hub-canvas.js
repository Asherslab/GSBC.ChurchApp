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

  const cellOf = (element) => element?.closest?.('.hub-cell[data-tile]') ?? null;
  const cells = () => [...canvas.querySelectorAll('.hub-cell[data-tile]')];
  const byKey = (key) => canvas.querySelector(`.hub-cell[data-tile="${CSS.escape(key)}"]`);
  const screen = () => canvas.querySelector('.phone-frame__screen');
  const tracks = (text) => text.split(' ').map(parseFloat).filter((value) => !Number.isNaN(value));

  const measure = (cell) => {
    const grid = cell.parentElement;
    const style = getComputedStyle(grid);
    const across = tracks(style.gridTemplateColumns);
    const track = across[0];
    const gap = parseFloat(style.columnGap) || 0;
    const span = Number(cell.dataset.columns);
    const scale = cell.getBoundingClientRect().width / (span * track + (span - 1) * gap) || 1;
    const row = Math.min(132, Math.max(88, track));
    return { columns: across.length, scale, track, row, gap, columnStep: (track + gap) * scale, rowStep: (row + gap) * scale };
  };

  const boxes = (except) => new Map(cells().filter((cell) => cell !== except).map((cell) => [cell, cell.getBoundingClientRect()]));

  const flip = (before, scale) => {
    if (still()) return;
    for (const [cell, was] of before) {
      const now = cell.getBoundingClientRect();
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
    cell.style.gridColumn = `${spot.column} / span ${spot.columns}`;
    cell.style.gridRow = `${spot.row} / span ${spot.rows}`;
    cell.style.setProperty('--tile-rows', spot.rows);
  };

  const lay = (order, spans, except) => {
    const before = boxes(except);
    const spots = pack(gesture.metrics.columns, order.map((key) => spans.get(key)));
    order.forEach((key, index) => {
      const cell = byKey(key);
      if (cell) place(cell, spots[index]);
    });
    flip(before, gesture.metrics.scale);
  };

  const rest = () => {
    const cell = gesture.cell;
    const transform = cell.style.transform;
    cell.style.transform = 'none';
    const box = cell.getBoundingClientRect();
    cell.style.transform = transform;
    gesture.home = { left: box.left, top: box.top, width: box.width, height: box.height };
  };

  const follow = () => {
    const { home, grab, last, metrics } = gesture;
    if (!last) return;
    gesture.cell.style.transform = `translate(${(last.x - grab.x - home.left) / metrics.scale}px, ${(last.y - grab.y - home.top) / metrics.scale}px)`;
  };

  const lift = (event) => {
    const cell = gesture.cell;
    const box = cell.getBoundingClientRect();
    gesture.started = true;
    gesture.handle.setPointerCapture(gesture.pointer);
    gesture.metrics = measure(cell);
    gesture.order = cells().map((found) => found.dataset.tile);
    gesture.original = [...gesture.order];
    gesture.spans = new Map(cells().map((found) => [found.dataset.tile, { columns: Number(found.dataset.columns), rows: Number(found.dataset.rows) }]));
    gesture.styles = new Map(cells().map((found) => [found, found.getAttribute('style')]));
    gesture.grab = { x: event.clientX - box.left, y: event.clientY - box.top };
    gesture.last = { x: event.clientX, y: event.clientY };
    gesture.scroll = screen()?.scrollTop ?? 0;
    getSelection()?.removeAllRanges();
    canvas.classList.add(gesture.kind === 'move' ? 'hub-canvas--moving' : 'hub-canvas--sizing');
    cell.classList.remove('hub-cell--settling');
    cell.classList.add('hub-cell--lifted');
    cell.style.transition = '';
    rest();
    document.addEventListener('keydown', escape, true);
    if (gesture.kind === 'move') {
      gesture.target = cellAt(anchor());
      gesture.frame = requestAnimationFrame(creep);
    }
  };

  const cellAt = ({ x, y }) => {
    const grid = gesture.cell.parentElement;
    const style = getComputedStyle(grid);
    const box = grid.getBoundingClientRect();
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

  const plan = async (asked, here) => {
    const spans = asked.original.map((each) => asked.spans.get(each));
    const indices = await receiver.invokeMethodAsync('Plan', asked.metrics.columns, spans.map((span) => span.columns), spans.map((span) => span.rows), asked.original.indexOf(asked.key), here.column, here.row);
    if (gesture !== asked) return false;
    asked.target = here;
    const order = indices.map((index) => asked.original[index]);
    if (order.join() === asked.order.join()) return false;
    asked.order = order;
    lay(order, asked.spans, asked.cell);
    rest();
    follow();
    return true;
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
    gesture.timer = setTimeout(async () => {
      if (!gesture || gesture.waiting !== wanted) return;
      gesture.waiting = null;
      const now = cellAt(anchor());
      if (`${now.column}:${now.row}` !== wanted) return;
      const asked = gesture;
      await plan(asked, now);
      if (gesture === asked) asked.target = cellAt(anchor());
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
    const top = edge.top + (parseFloat(getComputedStyle(view).getPropertyValue('--sat')) || 0) * scale + 44 * scale;
    const bottom = (tabs ? tabs.top : edge.bottom);
    const zone = edgeZone * scale;
    const y = gesture.last.y;
    let push = 0;
    if (y < top + zone) push = -Math.min(1, (top + zone - y) / zone);
    else if (y > bottom - zone) push = Math.min(1, (y - (bottom - zone)) / zone);
    if (push === 0) return;
    const by = Math.sign(push) * fastestScroll * Math.abs(push) ** 1.6;
    const was = view.scrollTop;
    const grid = gesture.cell.parentElement.getBoundingClientRect();
    const most = was + (grid.bottom + (parseFloat(getComputedStyle(view).paddingBottom) || 0) * scale - edge.bottom) / scale;
    view.scrollTop = Math.max(0, Math.min(most, was + by));
    if (view.scrollTop === was) return;
    rest();
    follow();
    retarget();
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
    if (!gesture || event.pointerId !== gesture.pointer) return;
    const dx = event.clientX - gesture.x;
    const dy = event.clientY - gesture.y;
    if (!gesture.started) {
      if (Math.hypot(dx, dy) < startAfter) return;
      lift(event);
    }
    gesture.last = { x: event.clientX, y: event.clientY };
    if (gesture.kind === 'move') {
      follow();
      retarget();
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
    canvas.classList.remove('hub-canvas--moving', 'hub-canvas--sizing');
  };

  const settleInto = (cell, scaled) => {
    cell.classList.remove('hub-cell--lifted');
    if (still()) {
      cell.style.transform = '';
      return;
    }
    cell.classList.add('hub-cell--settling');
    cell.style.transition = `transform ${glide}ms ${scaled ? dropCurve : glideCurve}, scale ${glide}ms ${glideCurve}`;
    cell.style.transform = '';
    setTimeout(() => cell.classList.remove('hub-cell--settling'), glide + 60);
  };

  const abandon = () => {
    const done = gesture;
    gesture = null;
    tidy(done);
    if (!done.started) {
      done.cell.classList.remove('hub-cell--lifted');
      return;
    }
    const before = boxes(done.cell);
    const box = done.cell.getBoundingClientRect();
    const view = screen();
    if (view) view.scrollTop = done.scroll;
    for (const [cell, style] of done.styles) {
      if (style === null) cell.removeAttribute('style');
      else cell.setAttribute('style', style);
    }
    flip(before, done.metrics.scale);
    if (!still()) {
      const home = done.cell.getBoundingClientRect();
      done.cell.style.transition = 'none';
      done.cell.style.transform = `translate(${(box.left - home.left) / done.metrics.scale}px, ${(box.top - home.top) / done.metrics.scale}px)`;
      done.cell.getBoundingClientRect();
    }
    settleInto(done.cell, false);
    const badge = done.cell.querySelector('[data-size]');
    if (badge && done.size) badge.textContent = `${done.size.columns} wide, ${done.size.rows} tall`;
  };

  function escape(event) {
    if (event.key !== 'Escape' || !gesture) return;
    event.preventDefault();
    event.stopPropagation();
    abandon();
  }

  const outside = (event) => {
    const view = screen()?.getBoundingClientRect();
    return !view || event.clientX < view.left || event.clientX > view.right || event.clientY < view.top || event.clientY > view.bottom;
  };

  const finish = async (event) => {
    if (!gesture || event.pointerId !== gesture.pointer) return;
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
    if (done.kind === 'move' && done.waiting) {
      clearTimeout(done.timer);
      done.waiting = null;
      await plan(done, cellAt(anchor()));
      if (gesture !== done) return;
    }
    gesture = null;
    tidy(done);
    settleInto(done.cell, done.kind === 'move');
    if (done.kind === 'move') {
      const to = done.order.indexOf(done.key);
      if (to === done.original.indexOf(done.key)) return;
      if (!still()) await new Promise((resolve) => setTimeout(resolve, glide));
      await receiver.invokeMethodAsync('Moved', done.key, to);
    } else if (done.next) {
      const was = done.spans.get(done.key);
      if (done.next.columns !== was.columns || done.next.rows !== was.rows)
        await receiver.invokeMethodAsync('Resized', done.key, done.next.columns, done.next.rows);
    }
  };

  const cancel = (event) => {
    if (!gesture || event.pointerId !== gesture.pointer) return;
    abandon();
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
      const [x, y] = keys[event.key];
      const scale = measure(cellOf(handle)).scale;
      const before = boxes(null);
      await receiver.invokeMethodAsync('Nudged', tile, x, y, event.shiftKey);
      requestAnimationFrame(() => {
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
  canvas.addEventListener('transitionend', ended);
  return {
    dispose() {
      canvas.removeEventListener('keydown', key);
      canvas.removeEventListener('pointerdown', down);
      canvas.removeEventListener('pointermove', move);
      canvas.removeEventListener('pointerup', finish);
      canvas.removeEventListener('pointercancel', cancel);
      canvas.removeEventListener('transitionend', ended);
      document.removeEventListener('keydown', escape, true);
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
