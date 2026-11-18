const leastFirstColumn = 200;

export function fitColumns(root) {
  let frame = 0;
  const fit = () => {
    frame = 0;
    const scroll = root.querySelector(':scope > .data-table__scroll');
    const table = scroll?.querySelector(':scope > table');
    if (!table?.querySelector('.optional')) return;
    delete root.dataset.tight;
    const first = table.querySelector('th');
    const shown = first?.getBoundingClientRect().width ?? 0;
    table.style.width = 'max-content';
    const natural = first?.getBoundingClientRect().width ?? 0;
    table.style.width = '';
    const tight = table.scrollWidth > scroll.clientWidth + 1 || shown < Math.min(natural, leastFirstColumn) - 1;
    if (tight) root.dataset.tight = '';
  };
  const schedule = () => { if (!frame) frame = requestAnimationFrame(fit); };
  const resized = new ResizeObserver(schedule);
  resized.observe(root);
  const changed = new MutationObserver(schedule);
  changed.observe(root, { childList: true, subtree: true, characterData: true });
  fit();
  return {
    dispose() {
      resized.disconnect();
      changed.disconnect();
      cancelAnimationFrame(frame);
    },
  };
}
