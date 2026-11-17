const capacitor = () => window.Capacitor;
const hasCompass = () => capacitor()?.getPlatform?.() === "ios";
const dataLayer = () => capacitor().Plugins.DataLayer;

const watchers = new Set();
let listening = null;

export function watch(onHeading) {
  if (!hasCompass()) return () => {};
  watchers.add(onHeading);
  if (!listening) {
    listening = dataLayer().addListener("compass", (reading) => watchers.forEach((watcher) => watcher(reading)));
    dataLayer().compassStart().catch(() => {});
  }
  return async () => {
    watchers.delete(onHeading);
    if (watchers.size > 0 || !listening) return;
    const handle = listening;
    listening = null;
    dataLayer().compassStop().catch(() => {});
    (await handle).remove();
  };
}
