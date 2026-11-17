const dataLayer = () => window.Capacitor?.Plugins?.DataLayer;
const isNative = () => !!window.Capacitor?.isNativePlatform?.();

export function listen(dotnet) {
  if (isNative()) dataLayer().addListener('walkTap', () => dotnet.invokeMethodAsync('OnTap'));
}

export const show = (name, state, alert) => (isNative() ? dataLayer().walkActivityShow({ name, state, alert }) : Promise.resolve());
export const end = () => (isNative() ? dataLayer().walkActivityEnd() : Promise.resolve());
export const taps = (since) => (isNative() ? dataLayer().walkTaps({ since }).then((result) => result.json) : Promise.resolve('[]'));
export const clear = (upTo) => (isNative() ? dataLayer().walkTapsClear({ upTo }) : Promise.resolve());
