window.gsbcNative = (() => {
  const capacitor = () => window.Capacitor;
  const isNative = () => !!capacitor()?.isNativePlatform?.();
  const dataLayer = () => capacitor().Plugins.DataLayer;
  const outcome = (result) => (result.ok ? null : result.error || 'Something went wrong. Try again.');

  return {
    isNative,
    unary: (method, request) => dataLayer().unary({ method, request }),
    startSync: (feeds, privateFeeds) => dataLayer().startSync({ feeds, privateFeeds }),
    pull: () => dataLayer().pull(),
    status: () => dataLayer().status().then((result) => result.state),
    isDevelopmentBuild: () => isNative() && dataLayer().status().then((result) => !!result.developmentBuild),
    isSignedIn: () => isNative() && dataLayer().status().then((result) => !!result.signedIn),
    device: () => (isNative() ? dataLayer().status().then((result) => ({ platform: result.platform || null, osMajor: result.osMajor || 0 })) : { platform: null, osMajor: 0 }),
    appVersion: () => (isNative() ? dataLayer().status().then((result) => ({ version: result.version || null, build: result.build || null })) : { version: null, build: null }),
    preference: (key) => {
      if (isNative()) return dataLayer().preference({ key }).then((result) => result.value ?? null);
      try { return localStorage.getItem(`gsbc:${key}`); } catch { return null; }
    },
    setPreference: (key, value) => {
      if (isNative()) return dataLayer().setPreference({ key, value });
      try { value === null ? localStorage.removeItem(`gsbc:${key}`) : localStorage.setItem(`gsbc:${key}`, value); } catch { }
      return null;
    },
    readPage: (feed, order, descending, limit, offset, groupKey) =>
      dataLayer().readPage({ feed, order, descending, limit, offset, groupKey }).then((result) => result.json),
    storeInfo: () => dataLayer().storeInfo().then((result) => result.json),
    enqueue: (method, request, quiet) => dataLayer().enqueue({ method, request, quiet }).then((result) => result.id),
    discardQuietOutbox: () => dataLayer().discardQuietOutbox(),
    takeCrashes: () => (isNative() ? dataLayer().takeCrashes().then((result) => result.json) : '[]'),
    outbox: () => dataLayer().outbox().then((result) => result.json),
    retryOutbox: (id) => dataLayer().retryOutbox({ id }),
    discardOutbox: (id) => dataLayer().discardOutbox({ id }),
    cachedMedia: (key, isPrivate) =>
      dataLayer().cachedMedia({ key, isPrivate }).then((result) => (result.path ? capacitor().convertFileSrc(result.path) : null)),
    fetchMedia: (key, url, isPrivate) =>
      dataLayer().fetchMedia({ key, url, isPrivate }).then((result) => capacitor().convertFileSrc(result.path)),
    prefetchMedia: (json) => dataLayer().prefetchMedia({ items: JSON.parse(json) }).then((result) => result.fetched),
    clearPrivateMedia: () => dataLayer().clearPrivateMedia(),
    signIn: () => dataLayer().signIn().then(outcome),
    registerPasskey: (ticket) => dataLayer().registerPasskey({ ticket }).then(outcome),
    signInWithCode: (email, code) => dataLayer().signInWithCode({ email, code }).then(outcome),
    signOut: () => dataLayer().signOut(),
    proveInstall: () => dataLayer().proveInstall().then((result) => !!result.proven),
    pushPermission: () => (isNative() ? dataLayer().pushPermission().then((result) => result.permission) : 'unavailable'),
    registerPush: () => dataLayer().registerPush().then((result) => ({
      permission: result.permission, token: result.token || null, environment: result.environment || null, appId: result.appId || null,
    })),
    setBadge: (count) => (isNative() ? dataLayer().setBadge({ count }) : null),
    listenSession: (receiver) => {
      dataLayer().addListener('sessionChanged', (event) => receiver.invokeMethodAsync('OnSessionChanged', !!event.signedIn));
    },
    listenLinks: (receiver) => {
      dataLayer().addListener('linkOpened', (event) => receiver.invokeMethodAsync('OnLinkOpened', event.path));
    },
    listenCrashes: (receiver) => {
      dataLayer().addListener('crashesArrived', () => receiver.invokeMethodAsync('OnCrashesArrived'));
    },
    listenAppState: (receiver) => {
      dataLayer().addListener('appStateChanged', (event) => receiver.invokeMethodAsync('OnAppStateChanged', !!event.active));
    },
    listenOutbox: (receiver) => {
      dataLayer().addListener('outboxChanged', () => receiver.invokeMethodAsync('OnOutboxChanged'));
    },
    listen: (receiver) => {
      dataLayer().addListener('feedsChanged', (event) => receiver.invokeMethodAsync('OnFeedsChanged', event.feeds));
      dataLayer().addListener('syncState', (event) => receiver.invokeMethodAsync('OnSyncState', event.state));
    },
  };
})();
