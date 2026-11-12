export function listenForScreenshots(receiver) {
  const dataLayer = window.Capacitor?.Plugins?.DataLayer;
  if (!window.Capacitor?.isNativePlatform?.() || !dataLayer) return;
  dataLayer.addListener('screenshotTaken', () => receiver.invokeMethodAsync('OnScreenshotTaken'));
}
