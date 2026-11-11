import { hand } from '../../GSBC.ChurchApp.Platform.Ui/js/file-pick.js';

const brands = [[/Edg\/(\d+)/, 'Edge'], [/OPR\/(\d+)/, 'Opera'], [/Firefox\/(\d+)/, 'Firefox'], [/Chrome\/(\d+)/, 'Chrome'], [/Version\/(\d+)[\d.]*.*Safari/, 'Safari']];
const systems = [[/iPhone|iPad/, 'iOS'], [/Android/, 'Android'], [/Mac OS X|Macintosh/, 'macOS'], [/Windows/, 'Windows'], [/CrOS/, 'ChromeOS'], [/Linux/, 'Linux']];

function named(list, text, withVersion) {
  for (const [pattern, name] of list) {
    const found = text.match(pattern);
    if (found) return withVersion && found[1] ? `${name} ${found[1]}` : name;
  }
  return '';
}

export function describe() {
  const agent = navigator.userAgent;
  return {
    browser: named(brands, agent, true) || 'A browser',
    system: named(systems, agent, false) || navigator.platform || '',
    window: `${innerWidth} × ${innerHeight}`,
    colourScheme: document.documentElement.dataset.scheme || (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'),
    title: document.title.split(' · ')[0],
  };
}

export function canCapture() {
  return !!navigator.mediaDevices?.getDisplayMedia;
}

const settle = 600;

function frameOrTimeout(video) {
  return new Promise((resolve) => {
    const done = setTimeout(resolve, settle);
    const next = () => {
      clearTimeout(done);
      resolve();
    };
    if (video.requestVideoFrameCallback) video.requestVideoFrameCallback(next);
    else requestAnimationFrame(() => requestAnimationFrame(next));
  });
}

export async function capture(input, layer, name) {
  if (!canCapture()) return 'unsupported';
  let stream;
  try {
    stream = await navigator.mediaDevices.getDisplayMedia({
      video: { displaySurface: 'browser' }, audio: false, preferCurrentTab: true, selfBrowserSurface: 'include', surfaceSwitching: 'exclude',
    });
  } catch {
    return 'refused';
  }
  const dialog = layer.closest('dialog') ?? layer;
  dialog.classList.add('is-capturing');
  try {
    const video = document.createElement('video');
    video.muted = true;
    video.srcObject = stream;
    await Promise.race([video.play(), new Promise((resolve) => setTimeout(resolve, settle))]);
    await frameOrTimeout(video);
    await frameOrTimeout(video);
    if (!video.videoWidth) return 'failed';
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d').drawImage(video, 0, 0);
    const blob = await new Promise((resolve) => canvas.toBlob(resolve, 'image/png'));
    if (!blob) return 'failed';
    hand(input, [new File([blob], name, { type: 'image/png' })]);
    return '';
  } finally {
    stream.getTracks().forEach((track) => track.stop());
    dialog.classList.remove('is-capturing');
  }
}
