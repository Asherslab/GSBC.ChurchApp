const capacitor = () => window.Capacitor;
const isNative = () => !!capacitor()?.isNativePlatform?.();
const dataLayer = () => capacitor().Plugins.DataLayer;

const bytesOf = (base64) => {
  if (Uint8Array.fromBase64) return Uint8Array.fromBase64(base64);
  const text = atob(base64);
  const bytes = new Uint8Array(text.length);
  for (let index = 0; index < text.length; index += 1) bytes[index] = text.charCodeAt(index);
  return bytes;
};

export const isAvailable = () => isNative();

export async function readKept(id, offset = 0, length = -1) {
  if (!isNative()) throw new Error('Kept files are only on the phone.');
  const result = await dataLayer().fileRead({ id, offset, length });
  return bytesOf(result.base64);
}

export function listen(dotnet) {
  if (isNative()) dataLayer().addListener('keptFilesChanged', () => dotnet.invokeMethodAsync('OnKeptFilesChanged'));
}

export const list = () => (isNative() ? dataLayer().keptFiles().then((result) => result.json) : '[]');
export const keep = (id, url, title) => dataLayer().fileKeep({ id, url, title });
export const remove = (id) => dataLayer().fileRemove({ id });
