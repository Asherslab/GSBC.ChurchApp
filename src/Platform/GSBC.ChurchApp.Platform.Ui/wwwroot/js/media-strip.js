export function watch(track, receiver) {
  let shown = 0;
  const onScroll = () => {
    const index = Math.round(track.scrollLeft / Math.max(1, track.clientWidth));
    if (index === shown) return;
    shown = index;
    receiver.invokeMethodAsync('Shown', index);
  };
  track.addEventListener('scroll', onScroll, { passive: true });
  return { dispose: () => track.removeEventListener('scroll', onScroll) };
}
