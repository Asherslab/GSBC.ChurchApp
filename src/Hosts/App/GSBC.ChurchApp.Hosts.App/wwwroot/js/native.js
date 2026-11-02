window.gsbcNative = (() => {
  const capacitor = () => window.Capacitor;
  const isNative = () => !!capacitor()?.isNativePlatform?.();
  const dataLayer = () => capacitor().Plugins.DataLayer;

  return {
    isNative,
    unary: (method, request) => dataLayer().unary({ method, request }),
    startSync: (feeds) => dataLayer().startSync({ feeds }),
    pull: () => dataLayer().pull(),
    status: () => dataLayer().status().then((result) => result.state),
    isDevelopmentBuild: () => isNative() && dataLayer().status().then((result) => !!result.developmentBuild),
    readPage: (feed, order, descending, limit, offset, groupKey) =>
      dataLayer().readPage({ feed, order, descending, limit, offset, groupKey }).then((result) => result.json),
    storeInfo: () => dataLayer().storeInfo().then((result) => result.json),
    enqueue: (method, request) => dataLayer().enqueue({ method, request }).then((result) => result.id),
    outbox: () => dataLayer().outbox().then((result) => result.json),
    retryOutbox: (id) => dataLayer().retryOutbox({ id }),
    discardOutbox: (id) => dataLayer().discardOutbox({ id }),
    cachedMedia: (key, isPrivate) =>
      dataLayer().cachedMedia({ key, isPrivate }).then((result) => (result.path ? capacitor().convertFileSrc(result.path) : null)),
    fetchMedia: (key, url, isPrivate) =>
      dataLayer().fetchMedia({ key, url, isPrivate }).then((result) => capacitor().convertFileSrc(result.path)),
    prefetchMedia: (json) => dataLayer().prefetchMedia({ items: JSON.parse(json) }).then((result) => result.fetched),
    clearPrivateMedia: () => dataLayer().clearPrivateMedia(),
    listenOutbox: (receiver) => {
      dataLayer().addListener('outboxChanged', () => receiver.invokeMethodAsync('OnOutboxChanged'));
    },
    listen: (receiver) => {
      dataLayer().addListener('feedsChanged', (event) => receiver.invokeMethodAsync('OnFeedsChanged', event.feeds));
      dataLayer().addListener('syncState', (event) => receiver.invokeMethodAsync('OnSyncState', event.state));
    },
  };
})();
