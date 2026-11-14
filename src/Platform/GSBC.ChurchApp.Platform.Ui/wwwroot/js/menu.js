const gap = 6;
const placed = ['position', 'top', 'left', 'bottom', 'right', 'width', 'maxHeight'];

const clipOf = (element) => {
  for (let at = element.parentElement; at && at !== document.body; at = at.parentElement) {
    if (getComputedStyle(at).overflowY !== 'visible') return at;
  }
  return null;
};

const windowBox = (panel) => {
  const bar = panel.closest('.sheet-layer') ? null : document.querySelector('.tabbar');
  const barTop = bar && bar.offsetParent ? bar.getBoundingClientRect().top : Infinity;
  const visible = window.visualViewport ? window.visualViewport.height : window.innerHeight;
  const safeTop = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--sat')) || 0;
  return { top: safeTop, bottom: Math.min(window.innerHeight, visible, barTop) };
};

const inWindow = (panel, trigger, height, room) => {
  const width = panel.getBoundingClientRect().width;
  const below = room.bottom - trigger.bottom - gap * 2;
  const above = trigger.top - room.top - gap * 2;
  const down = height <= below || (height > above && below >= above);
  const top = down ? trigger.bottom + gap : Math.max(room.top + gap, trigger.top - gap - Math.min(height, above));
  const left = panel.classList.contains('menu__panel--start') || panel.closest('.menu--block')
    ? trigger.left
    : trigger.right - width;
  const x = Math.min(Math.max(gap, left), window.innerWidth - width - gap);
  Object.assign(panel.style, { position: 'fixed', top: `${top}px`, left: `${x}px`, bottom: 'auto', right: 'auto', width: `${width}px` });
  if (height > Math.max(below, above)) panel.style.maxHeight = `${Math.max(below, above)}px`;
  const shown = panel.getBoundingClientRect();
  panel.style.top = `${top + (top - shown.top)}px`;
  panel.style.left = `${x + (x - shown.left)}px`;
  const follow = (event) => {
    if (event.target instanceof Node && panel.contains(event.target)) return;
    if (!panel.isConnected) {
      removeEventListener('scroll', follow, true);
      removeEventListener('resize', follow);
      return;
    }
    place(panel);
  };
  if (!panel.dataset.follows) {
    panel.dataset.follows = 'true';
    addEventListener('scroll', follow, { capture: true, passive: true });
    addEventListener('resize', follow, { passive: true });
  }
};

export function place(panel) {
  if (!panel) return;
  panel.classList.remove('menu__panel--up');
  placed.forEach((property) => { panel.style[property] = ''; });
  const room = windowBox(panel);
  const clip = clipOf(panel.parentElement)?.getBoundingClientRect();
  const top = Math.max(room.top, clip?.top ?? -Infinity);
  const bottom = Math.min(room.bottom, clip?.bottom ?? Infinity);
  const trigger = panel.parentElement.getBoundingClientRect();
  const height = panel.getBoundingClientRect().height;

  if (height <= bottom - trigger.bottom - gap * 2) return;
  if (height <= trigger.top - top - gap * 2) {
    panel.classList.add('menu__panel--up');
    return;
  }
  inWindow(panel, trigger, height, room);
}
