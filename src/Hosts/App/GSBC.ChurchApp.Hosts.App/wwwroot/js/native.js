window.gsbcNative = (() => {
  const capacitor = () => window.Capacitor;
  const isNative = () => !!capacitor()?.isNativePlatform?.();
  const dataLayer = () => capacitor().Plugins.DataLayer;

  return {
    isNative,
    unary: (method, request) => dataLayer().unary({ method, request }),
  };
})();
