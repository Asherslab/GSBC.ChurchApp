const capacitor = () => window.Capacitor;
const isNative = () => !!capacitor()?.isNativePlatform?.();
const dataLayer = () => capacitor().Plugins.DataLayer;

let receiver = null;
const announce = () => receiver?.invokeMethodAsync('OnFix');

const native = {
  listen: () => { dataLayer().addListener('locationFix', announce); },
  start: (purpose) => dataLayer().locationStart({ purpose }).then((result) => result.state),
  stop: () => dataLayer().locationStop(),
  state: () => dataLayer().locationState().then((result) => JSON.stringify(result)),
  fixes: (since) => dataLayer().locationFixes({ since }).then((result) => result.json),
  clear: (upTo) => dataLayer().locationClear({ upTo }),
};

const web = (() => {
  const key = 'gsbc:location-session';
  const cap = 20000;
  const read = () => {
    try { return JSON.parse(sessionStorage.getItem(key)) ?? { latest: 0, running: false, fixes: [] }; } catch { return { latest: 0, running: false, fixes: [] }; }
  };
  const stored = read();
  const write = () => {
    try { sessionStorage.setItem(key, JSON.stringify(stored)); } catch { }
  };

  let watch = null;
  let lastAnnounced = 0;
  let announceTimer = null;
  const throttledAnnounce = () => {
    const wait = 1000 - (Date.now() - lastAnnounced);
    if (wait <= 0) {
      lastAnnounced = Date.now();
      announce();
    } else if (!announceTimer) {
      announceTimer = setTimeout(() => {
        announceTimer = null;
        lastAnnounced = Date.now();
        announce();
      }, wait);
    }
  };

  const record = (position) => {
    const { coords } = position;
    stored.latest += 1;
    stored.fixes.push({
      seq: stored.latest,
      at: Math.round(position.timestamp),
      lat: coords.latitude,
      lon: coords.longitude,
      accuracy: coords.accuracy,
      heading: Number.isFinite(coords.heading) ? coords.heading : -1,
      speed: Number.isFinite(coords.speed) ? coords.speed : -1,
    });
    if (stored.fixes.length > cap) stored.fixes.splice(0, stored.fixes.length - cap);
    write();
    throttledAnnounce();
  };

  const end = () => {
    if (watch !== null) navigator.geolocation.clearWatch(watch);
    watch = null;
    stored.running = false;
    write();
  };

  const begin = () => new Promise((resolve) => {
    if (!navigator.geolocation) return resolve('off');
    let answered = false;
    const answer = (state) => {
      if (answered) return;
      answered = true;
      resolve(state);
    };
    if (watch !== null) navigator.geolocation.clearWatch(watch);
    watch = navigator.geolocation.watchPosition(
      (position) => {
        stored.running = true;
        record(position);
        answer('running');
      },
      (error) => {
        if (error.code === error.PERMISSION_DENIED) { end(); answer('denied'); }
        else if (error.code === error.POSITION_UNAVAILABLE && !answered) { end(); answer('off'); }
        else { stored.running = true; write(); answer('running'); }
      },
      { enableHighAccuracy: true, maximumAge: 0 });
  });

  const authorization = async () => {
    if (!navigator.geolocation) return 'off';
    try {
      const status = await navigator.permissions.query({ name: 'geolocation' });
      return status.state === 'granted' ? 'whenInUse' : status.state === 'denied' ? 'denied' : 'notDetermined';
    } catch {
      return 'notDetermined';
    }
  };

  if (stored.running) begin();

  return {
    listen: () => { },
    start: (purpose) => (watch !== null && stored.running ? Promise.resolve('running') : begin()),
    stop: () => end(),
    state: async () => JSON.stringify({ running: stored.running, authorization: await authorization() }),
    fixes: (since) => JSON.stringify({ fixes: stored.fixes.filter((fix) => fix.seq > since), latest: stored.latest, running: stored.running }),
    clear: (upTo) => {
      stored.fixes = stored.fixes.filter((fix) => fix.seq > upTo);
      write();
    },
  };
})();

const session = () => (isNative() ? native : web);

export function listen(dotnet) {
  receiver = dotnet;
  session().listen();
}

export const isAvailable = () => isNative() || !!navigator.geolocation;
export const start = (purpose) => session().start(purpose);
export const stop = () => session().stop();
export const state = () => session().state();
export const fixes = (since) => session().fixes(since);
export const clear = (upTo) => session().clear(upTo);
