export function attach(list, receiver) {
  const onDown = (event) => {
    const grip = event.target.closest('[data-reorder-grip]');
    if (!grip || !list.contains(grip) || event.button > 0) return;
    const rows = [...list.querySelectorAll(':scope > [data-reorder-row]')];
    const row = grip.closest('[data-reorder-row]');
    const from = rows.indexOf(row);
    if (from < 0) return;
    event.preventDefault();
    grip.setPointerCapture(event.pointerId);
    const startY = event.clientY;
    const boxes = rows.map((each) => each.getBoundingClientRect());
    const height = boxes[from].height;
    let to = from;
    row.classList.add('reorder__row--lifted');

    const onMove = (moving) => {
      const dy = moving.clientY - startY;
      row.style.transform = `translateY(${dy}px)`;
      const centre = boxes[from].top + height / 2 + dy;
      const next = boxes.filter((box, index) => index !== from && box.top + box.height / 2 < centre).length;
      if (next === to) return;
      to = next;
      rows.forEach((each, index) => {
        if (index === from) return;
        const shift = from < to && index > from && index <= to ? -height : from > to && index >= to && index < from ? height : 0;
        each.style.transform = shift ? `translateY(${shift}px)` : '';
      });
      receiver.invokeMethodAsync('Preview', from, to);
    };

    const onEnd = () => {
      grip.removeEventListener('pointermove', onMove);
      grip.removeEventListener('pointerup', onEnd);
      grip.removeEventListener('pointercancel', onEnd);
      if (to === from) {
        row.classList.remove('reorder__row--lifted');
        rows.forEach((each) => { each.style.transform = ''; });
        return;
      }
      const dropped = row.getBoundingClientRect().top;
      const settle = () => {
        observer.disconnect();
        clearTimeout(fallback);
        rows.forEach((each) => { each.style.transition = 'none'; each.style.transform = ''; });
        row.classList.remove('reorder__row--lifted');
        const offset = dropped - row.getBoundingClientRect().top;
        row.style.transform = offset ? `translateY(${offset}px)` : '';
        row.getBoundingClientRect();
        rows.forEach((each) => { each.style.transition = ''; });
        requestAnimationFrame(() => { row.style.transform = ''; });
      };
      const observer = new MutationObserver(settle);
      observer.observe(list, { childList: true });
      const fallback = setTimeout(settle, 1500);
      receiver.invokeMethodAsync('Moved', from, to);
    };

    grip.addEventListener('pointermove', onMove);
    grip.addEventListener('pointerup', onEnd);
    grip.addEventListener('pointercancel', onEnd);
  };
  list.addEventListener('pointerdown', onDown);
  return { dispose: () => list.removeEventListener('pointerdown', onDown) };
}
