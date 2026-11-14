export function listen(frame, receiver) {
  const onMessage = (event) => {
    if (event.source !== frame.contentWindow || event.data?.source !== 'gsbc-youtube') return;
    receiver.invokeMethodAsync('OnVideoState', event.data.state, event.data.code ?? null);
  };
  window.addEventListener('message', onMessage);
  return { dispose: () => window.removeEventListener('message', onMessage) };
}

const youTubeId = /^https:\/\/(?:www\.|m\.)?(?:youtu\.be\/|youtube\.com\/(?:watch\?(?:.*&)?v=|shorts\/|live\/|embed\/))([\w-]{11})/i;
const vimeoId = /^https:\/\/(?:www\.)?vimeo\.com\/(\d{1,12})(?:[/?#]|$)/i;
const thumbnails = new Map();

const vimeoThumbnail = async id => {
  try {
    const answer = await fetch(`https://vimeo.com/api/oembed.json?url=${encodeURIComponent(`https://vimeo.com/${id}`)}&width=640`);
    if (!answer.ok) return null;
    const { thumbnail_url: url } = await answer.json();
    return typeof url === 'string' && url.startsWith('https://') ? url : null;
  } catch {
    return null;
  }
};

export function thumbnail(link) {
  const youTube = youTubeId.exec(link);
  if (youTube) return Promise.resolve(`https://i.ytimg.com/vi/${youTube[1]}/hqdefault.jpg`);
  const vimeo = vimeoId.exec(link);
  if (!vimeo) return Promise.resolve(null);
  if (!thumbnails.has(vimeo[1])) thumbnails.set(vimeo[1], vimeoThumbnail(vimeo[1]));
  return thumbnails.get(vimeo[1]);
}

export const withFirstFrame = url => (url.includes('#') ? url : `${url}#t=0.1`);
