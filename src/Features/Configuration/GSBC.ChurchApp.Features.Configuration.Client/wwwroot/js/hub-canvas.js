const startAfter = 5;
const glide = 200;
const settle = 110;
const mostRows = 4;

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

  const measure = (cell) => {
    const grid = cell.parentElement;
    const style = getComputedStyle(grid);
    const columns = style.gridTemplateColumns.split(' ').length;
    const track = parseFloat(style.gridTemplateColumns.split(' ')[0]);
    const gap = parseFloat(style.columnGap) || 0;
    const span = Number(cell.dataset.columns);
    const scale = cell.getBoundingClientRect().width / (span * track + (span - 1) * gap) || 1;
    const row = Math.min(132, Math.max(88, track));
    return { columns, scale, columnStep: (track + gap) * scale, rowStep: (row + gap) * scale };
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
      cell.style.transition = `transform ${glide}ms cubic-bezier(0.2, 0.8, 0.2, 1)`;
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
    if (gesture.slot) {
      const own = spots[order.indexOf(gesture.key)];
      gesture.slot.style.gridColumn = `${own.column} / span ${own.columns}`;
      gesture.slot.style.gridRow = `${own.row} / span ${own.rows}`;
    }
    flip(before, gesture.metrics.scale);
  };

  const follow = (event) => {
    const cell = gesture.cell;
    cell.style.transform = 'none';
    const home = cell.getBoundingClientRect();
    const { scale } = gesture.metrics;
    cell.style.transform = `translate(${(event.clientX - gesture.grab.x - home.left) / scale}px, ${(event.clientY - gesture.grab.y - home.top) / scale}px)`;
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
    gesture.scroll = screen()?.scrollTop ?? 0;
    getSelection()?.removeAllRanges();
    canvas.classList.add(gesture.kind === 'move' ? 'hub-canvas--moving' : 'hub-canvas--sizing');
    cell.classList.add('hub-cell--lifted');
    if (gesture.kind === 'move') {
      const slot = document.createElement('div');
      slot.className = 'hub-slot';
      slot.setAttribute('aria-hidden', 'true');
      slot.style.gridColumn = cell.style.gridColumn;
      slot.style.gridRow = cell.style.gridRow;
      cell.parentElement.append(slot);
      gesture.slot = slot;
    }
    document.addEventListener('keydown', escape, true);
  };

  const retarget = (event) => {
    const over = document.elementsFromPoint(event.clientX, event.clientY)
      .map(cellOf).find((found) => found && found !== gesture.cell && canvas.contains(found));
    const candidate = over ? over.dataset.tile : null;
    if (candidate === gesture.waiting) return;
    clearTimeout(gesture.timer);
    gesture.waiting = candidate;
    if (!candidate) return;
    gesture.timer = setTimeout(() => {
      if (!gesture || gesture.waiting !== candidate) return;
      const to = gesture.order.indexOf(candidate);
      const order = gesture.order.filter((key) => key !== gesture.key);
      order.splice(to, 0, gesture.key);
      gesture.order = order;
      gesture.waiting = null;
      lay(order, gesture.spans, gesture.cell);
      if (gesture.last) follow(gesture.last);
    }, settle);
  };

  const scrollNear = (event) => {
    const view = screen();
    if (!view) return;
    const edge = view.getBoundingClientRect();
    const by = event.clientY < edge.top + 64 ? -16 : event.clientY > edge.bottom - 64 ? 16 : 0;
    if (by !== 0) view.scrollTop += by;
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
    gesture.last = event;
    if (gesture.kind === 'move') {
      scrollNear(event);
      follow(event);
      retarget(event);
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
    document.removeEventListener('keydown', escape, true);
    canvas.classList.remove('hub-canvas--moving', 'hub-canvas--sizing');
    done.cell.classList.remove('hub-cell--lifted');
    done.slot?.remove();
  };

  const settleInto = (cell) => {
    if (still()) {
      cell.style.transform = '';
      return;
    }
    cell.style.transition = `transform ${glide}ms cubic-bezier(0.2, 0.8, 0.2, 1), scale ${glide}ms cubic-bezier(0.2, 0.8, 0.2, 1)`;
    cell.style.transform = '';
  };

  const restore = (done) => {
    const before = boxes(done.cell);
    for (const [cell, style] of done.styles) {
      if (style === null) cell.removeAttribute('style');
      else cell.setAttribute('style', style);
    }
    flip(before, done.metrics.scale);
  };

  const abandon = () => {
    const done = gesture;
    gesture = null;
    tidy(done);
    if (!done.started) return;
    const view = screen();
    if (view) view.scrollTop = done.scroll;
    const box = done.cell.getBoundingClientRect();
    restore(done);
    if (!still()) {
      const home = done.cell.getBoundingClientRect();
      done.cell.style.transition = 'none';
      done.cell.style.transform = `translate(${(box.left - home.left) / done.metrics.scale}px, ${(box.top - home.top) / done.metrics.scale}px)`;
      done.cell.getBoundingClientRect();
    }
    settleInto(done.cell);
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
    gesture = null;
    tidy(done);
    settleInto(done.cell);
    if (done.kind === 'move') {
      const to = done.order.indexOf(done.key);
      if (to !== done.original.indexOf(done.key)) await receiver.invokeMethodAsync('Moved', done.key, to);
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
    const bottom = (tabs ? tabs.getBoundingClientRect().top : view.bottom) - 16;
    const scale = view.height / screen.clientHeight || 1;
    const top = view.top + 116 * scale;
    const current = screen.scrollTop;
    let next = box.bottom > bottom ? current + (box.bottom - bottom) / scale : current;
    const topAfter = box.top - (next - current) * scale;
    if (topAfter < top) next -= (top - topAfter) / scale;
    next = Math.max(0, next);
    if (next > 0 && next < 72) next = next < 36 ? 0 : 72;
    if (Math.abs(next - screen.scrollTop) > 1) screen.scrollTo({ top: next, behavior: still() ? 'auto' : 'smooth' });
  });
}

export function scheme() {
  return document.documentElement.dataset.scheme === 'dark' ? 'Dark' : 'Light';
}
