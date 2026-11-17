const capacitor = () => window.Capacitor;
const isNative = () => !!capacitor()?.isNativePlatform?.();
const dataLayer = () => capacitor().Plugins.DataLayer;

const watchers = new Set();
let listening = null;

export function watch(onHeading) {
  if (!isNative()) return () => {};
  watchers.add(onHeading);
  if (!listening) {
    listening = dataLayer().addListener("compass", (reading) => watchers.forEach((watcher) => watcher(reading)));
    dataLayer().compassStart();
  }
  return async () => {
    watchers.delete(onHeading);
    if (watchers.size > 0 || !listening) return;
    const handle = listening;
    listening = null;
    dataLayer().compassStop();
    (await handle).remove();
  };
}
