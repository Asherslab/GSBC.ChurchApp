const startAfter = 5;

export function attach(canvas, receiver) {
  let gesture = null;

  const cellOf = (element) => element.closest('.hub-cell');
  const cells = () => [...canvas.querySelectorAll('.hub-cell[data-tile]')];

  const measure = (cell) => {
    const grid = cell.parentElement;
    const style = getComputedStyle(grid);
    const columns = style.gridTemplateColumns.split(' ').length;
    const track = parseFloat(style.gridTemplateColumns.split(' ')[0]);
    const gap = parseFloat(style.columnGap) || 0;
    const span = Number(cell.dataset.columns);
    const scale = cell.getBoundingClientRect().width / (span * track + (span - 1) * gap);
    const row = Math.min(132, Math.max(88, track));
    return { columns, scale, columnStep: (track + gap) * scale, rowStep: (row + gap) * scale };
  };

  const down = (event) => {
    if (event.button !== 0) return;
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
      size: { columns: Number(cell.dataset.columns), rows: Number(cell.dataset.rows) },
    };
    if (gesture.kind === 'resize') event.preventDefault();
  };

  const move = (event) => {
    if (!gesture || event.pointerId !== gesture.pointer) return;
    const dx = event.clientX - gesture.x;
    const dy = event.clientY - gesture.y;
    if (!gesture.started) {
      if (Math.hypot(dx, dy) < startAfter) return;
      gesture.started = true;
      gesture.handle.setPointerCapture(gesture.pointer);
      gesture.metrics = measure(gesture.cell);
      canvas.classList.add(gesture.kind === 'move' ? 'hub-canvas--moving' : 'hub-canvas--sizing');
      gesture.cell.classList.add('hub-cell--lifted');
    }
    if (gesture.kind === 'move') {
      const { scale } = gesture.metrics;
      const screen = canvas.querySelector('.phone-frame__screen');
      if (screen) {
        const edge = screen.getBoundingClientRect();
        const by = event.clientY < edge.top + 64 ? -16 : event.clientY > edge.bottom - 64 ? 16 : 0;
        if (by !== 0) {
          screen.scrollTop += by;
          gesture.scrolled = (gesture.scrolled ?? 0) + by;
        }
      }
      gesture.cell.style.transform = `translate(${dx / scale}px, ${(dy + (gesture.scrolled ?? 0)) / scale}px)`;
      const over = document.elementsFromPoint(event.clientX, event.clientY)
        .map(cellOf).find((found) => found && found !== gesture.cell && canvas.contains(found));
      cells().forEach((cell) => cell.classList.toggle('hub-cell--target', cell === over));
      gesture.over = over ? cells().indexOf(over) : null;
    } else {
      const { columns, columnStep, rowStep } = gesture.metrics;
      const wide = Math.max(1, Math.min(columns, Math.round(gesture.size.columns + dx / columnStep)));
      const tall = Math.max(1, Math.min(4, Math.round(gesture.size.rows + dy / rowStep)));
      gesture.next = { columns: wide, rows: tall };
      gesture.cell.style.gridColumnEnd = `span ${wide}`;
      gesture.cell.style.gridRowEnd = `span ${tall}`;
      gesture.cell.style.setProperty('--tile-rows', tall);
      const badge = gesture.cell.querySelector('[data-size]');
      if (badge) badge.textContent = `${wide} × ${tall}`;
    }
  };

  const finish = async (event) => {
    if (!gesture || event.pointerId !== gesture.pointer) return;
    const done = gesture;
    gesture = null;
    canvas.classList.remove('hub-canvas--moving', 'hub-canvas--sizing');
    cells().forEach((cell) => cell.classList.remove('hub-cell--target'));
    done.cell.classList.remove('hub-cell--lifted');
    done.cell.style.transform = '';
    if (!done.started) {
      await receiver.invokeMethodAsync('Chosen', done.key);
      return;
    }
    if (done.kind === 'move' && done.over != null) {
      await receiver.invokeMethodAsync('Moved', done.key, done.over);
    } else if (done.kind === 'resize' && done.next) {
      done.cell.style.gridColumnEnd = '';
      done.cell.style.gridRowEnd = '';
      await receiver.invokeMethodAsync('Resized', done.key, done.next.columns, done.next.rows);
    }
  };

  const cancel = (event) => {
    if (!gesture || event.pointerId !== gesture.pointer) return;
    gesture.cell.style.transform = '';
    gesture.cell.style.gridColumnEnd = '';
    gesture.cell.style.gridRowEnd = '';
    gesture.cell.classList.remove('hub-cell--lifted');
    canvas.classList.remove('hub-canvas--moving', 'hub-canvas--sizing');
    cells().forEach((cell) => cell.classList.remove('hub-cell--target'));
    gesture = null;
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
      await receiver.invokeMethodAsync('Nudged', tile, x, y, event.shiftKey);
      canvas.querySelector(`.hub-cell[data-tile="${CSS.escape(tile)}"] [data-arrange]`)?.focus();
    }
  };

  canvas.addEventListener('keydown', key);
  canvas.addEventListener('pointerdown', down);
  canvas.addEventListener('pointermove', move);
  canvas.addEventListener('pointerup', finish);
  canvas.addEventListener('pointercancel', cancel);
  return {
    dispose() {
      canvas.removeEventListener('keydown', key);
      canvas.removeEventListener('pointerdown', down);
      canvas.removeEventListener('pointermove', move);
      canvas.removeEventListener('pointerup', finish);
      canvas.removeEventListener('pointercancel', cancel);
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
