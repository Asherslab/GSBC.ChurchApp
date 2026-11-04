const fileOf = (input) => input?.files?.[0] ?? null;

export function describe(input) {
  const file = fileOf(input);
  if (!file) return Promise.resolve(null);
  return new Promise((resolve) => {
    const url = URL.createObjectURL(file);
    const audio = new Audio();
    const done = (duration) => {
      URL.revokeObjectURL(url);
      resolve({ name: file.name, size: file.size, type: file.type || (file.name.toLowerCase().endsWith('.m4a') ? 'audio/mp4' : 'audio/mpeg'), duration });
    };
    audio.preload = 'metadata';
    audio.onloadedmetadata = () => done(Number.isFinite(audio.duration) ? Math.round(audio.duration) : 0);
    audio.onerror = () => done(0);
    audio.src = url;
  });
}

export function put(input, url, contentType, receiver) {
  const file = fileOf(input);
  return new Promise((resolve) => {
    if (!file) return resolve('Choose the audio file again.');
    const request = new XMLHttpRequest();
    request.open('PUT', url);
    request.setRequestHeader('Content-Type', contentType);
    let last = 0;
    request.upload.onprogress = (event) => {
      if (!event.lengthComputable || Date.now() - last < 250) return;
      last = Date.now();
      receiver.invokeMethodAsync('OnUploadProgress', event.loaded / event.total);
    };
    request.onload = () => resolve(request.status >= 200 && request.status < 300 ? null : `The audio couldn't be sent (${request.status}).`);
    request.onerror = () => resolve("The audio couldn't be sent. Check the connection and try again.");
    request.send(file);
  });
}
