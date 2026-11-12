export function place(panel) {
  if (!panel) return;
  const bar = document.querySelector('.tabbar');
  const floor = window.innerHeight - (bar ? bar.getBoundingClientRect().height : 0);
  const box = panel.getBoundingClientRect();
  const trigger = panel.parentElement.getBoundingClientRect();
  if (box.bottom > floor && trigger.top - box.height > 0)
    panel.classList.add('menu__panel--up');
}
