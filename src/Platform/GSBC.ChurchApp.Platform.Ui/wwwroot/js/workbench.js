export function fitRecord(workbench) {
  let frame = 0;
  const fit = () => {
    frame = 0;
    const record = workbench.querySelector(':scope > .workbench__detail');
    if (!record) return;
    const top = Math.max(0, workbench.getBoundingClientRect().top);
    record.style.setProperty('--record-top', `${Math.round(top)}px`);
  };
  const schedule = () => { if (!frame) frame = requestAnimationFrame(fit); };
  const resized = new ResizeObserver(schedule);
  resized.observe(workbench);
  addEventListener('scroll', schedule, { passive: true, capture: true });
  addEventListener('resize', schedule);
  new MutationObserver(schedule).observe(workbench, { childList: true });
  fit();
  return {
    dispose() {
      resized.disconnect();
      removeEventListener('scroll', schedule, { capture: true });
      removeEventListener('resize', schedule);
      cancelAnimationFrame(frame);
    },
  };
}
