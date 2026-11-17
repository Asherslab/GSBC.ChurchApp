const fullKey = "map-workspace.full";
const sideKey = "map-workspace.side";
const busyOnScreen = ".menu__panel, .sheet, [role=dialog], [role=alertdialog], .popover, .map-draw-bar, .plan-draw-bar";

const read = (store, key) => {
  try {
    return store().getItem(key);
  } catch {
    return null;
  }
};

const write = (store, key, value) => {
  try {
    if (value === null) store().removeItem(key);
    else store().setItem(key, value);
  } catch {
  }
};

const fullAttribute = "data-full-map";
const swapGrace = 250;
let attached = 0;

const session = () => sessionStorage;
const kept = () => localStorage;

export function attach(root, workspace) {
  let dragging = null;
  let full = false;
  attached++;

  const escape = (event) => {
    if (event.key !== "Escape" || !root.classList.contains("map-workspace--full")) return;
    if (dragging) {
      endDrag(false);
      return;
    }
    if (event.defaultPrevented || document.querySelector(busyOnScreen)) return;
    if (document.activeElement?.matches("input, textarea, select, [contenteditable=true]")) return;
    workspace.invokeMethodAsync("LeaveFullAsync");
  };

  const sideNow = () => (root.classList.contains("map-workspace--panel-right") ? "right" : "left");

  const sideAt = (panel) => {
    const area = root.getBoundingClientRect();
    const box = panel.getBoundingClientRect();
    return box.left + box.width / 2 > area.left + area.width / 2 ? "right" : "left";
  };

  const startDrag = (event) => {
    if (event.button !== 0 || !root.classList.contains("map-workspace--full")) return;
    const grip = event.target.closest("[data-panel-grip]");
    if (!grip || !root.contains(grip) || event.target.closest("button, a, input, select, textarea, [role=menu]")) return;
    const panel = root.querySelector(".map-workspace__drawer");
    if (!panel) return;
    event.preventDefault();
    grip.setPointerCapture(event.pointerId);
    dragging = { grip, panel, pointer: event.pointerId, from: event.clientX, side: sideNow() };
    root.classList.add("map-workspace--dragging");
  };

  const moveDrag = (event) => {
    if (!dragging || event.pointerId !== dragging.pointer) return;
    const area = root.getBoundingClientRect();
    const box = dragging.panel.getBoundingClientRect();
    const shift = event.clientX - dragging.from;
    const current = new DOMMatrixReadOnly(getComputedStyle(dragging.panel).transform).m41;
    const restLeft = box.left - current;
    const reach = Math.min(Math.max(shift, area.left - restLeft), area.right - box.width - restLeft);
    dragging.panel.style.transform = `translateX(${reach}px)`;
    const target = sideAt(dragging.panel);
    if (target === dragging.side) root.removeAttribute("data-drop");
    else root.setAttribute("data-drop", target);
  };

  const endDrag = (keep) => {
    if (!dragging) return;
    const { grip, panel, pointer, side } = dragging;
    const target = sideAt(panel);
    dragging = null;
    if (grip.hasPointerCapture(pointer)) grip.releasePointerCapture(pointer);
    panel.style.transform = "";
    root.classList.remove("map-workspace--dragging");
    root.removeAttribute("data-drop");
    if (keep && target !== side) workspace.invokeMethodAsync("PanelSideAsync", target === "right");
  };

  const upDrag = (event) => {
    if (dragging && event.pointerId === dragging.pointer) endDrag(true);
  };
  const cancelDrag = (event) => {
    if (dragging && event.pointerId === dragging.pointer) endDrag(false);
  };

  window.addEventListener("keydown", escape, true);
  root.addEventListener("pointerdown", startDrag);
  root.addEventListener("pointermove", moveDrag);
  root.addEventListener("pointerup", upDrag);
  root.addEventListener("pointercancel", cancelDrag);

  return {
    remembered: () => `${read(session, fullKey) === "1" ? "full" : "normal"} ${read(kept, sideKey) === "right" ? "right" : "left"}`,
    full: (on) => {
      full = on;
      document.documentElement.toggleAttribute(fullAttribute, on);
      write(session, fullKey, on ? "1" : null);
    },
    side: (right) => write(kept, sideKey, right ? "right" : null),
    dispose: () => {
      window.removeEventListener("keydown", escape, true);
      root.removeEventListener("pointerdown", startDrag);
      root.removeEventListener("pointermove", moveDrag);
      root.removeEventListener("pointerup", upDrag);
      root.removeEventListener("pointercancel", cancelDrag);
      attached--;
      if (full) setTimeout(() => attached === 0 && document.documentElement.removeAttribute(fullAttribute), swapGrace);
    },
  };
}
