const capacitor = () => window.Capacitor;
const isNative = () => !!capacitor()?.isNativePlatform?.();
const dataLayer = () => capacitor().Plugins.DataLayer;

let receiver = null;
const send = (state) => receiver?.invokeMethodAsync('OnAudioState', JSON.stringify(state));

const native = {
  async start() {
    dataLayer().addListener('audioState', (event) => send(event.state));
    return (await dataLayer().audioState()).state;
  },
  load: (item, startAt, play) => dataLayer().audioLoad({ item: JSON.parse(item), startAt, play }),
  play: () => dataLayer().audioPlay(),
  pause: () => dataLayer().audioPause(),
  seek: (to) => dataLayer().audioSeek({ to }),
  skip: (by) => dataLayer().audioSkip({ by }),
  rate: (rate) => dataLayer().audioRate({ rate }),
  sleep: (minutes, endOfItem) => dataLayer().audioSleep({ minutes, endOfItem }),
  upNext: (items) => dataLayer().audioUpNext({ items: JSON.parse(items) }),
  stop: () => dataLayer().audioStop(),
  mark: (id, mark, at) => dataLayer().audioMark({ id, mark, at }),
  journal: (since) => dataLayer().audioJournal({ since }).then((result) => result.json),
};

const journalKey = 'gsbc:audio-journal';
const currentKey = 'gsbc:audio-current';
const saveEvery = 5000;

const web = (() => {
  const audio = new Audio();
  audio.preload = 'metadata';
  let item = null;
  let upNext = [];
  let sleepAt = null;
  let sleepAtEnd = false;
  let sleepTimer = null;
  let ended = false;
  let problem = null;
  let lastSaved = 0;

  const read = (key, fallback) => {
    try { return JSON.parse(localStorage.getItem(key)) ?? fallback; } catch { return fallback; }
  };
  const write = (key, value) => {
    try { localStorage.setItem(key, JSON.stringify(value)); } catch { }
  };

  const record = (id, change) => {
    const journal = read(journalKey, { seq: 0, records: {} });
    journal.seq += 1;
    const before = journal.records[id] ?? { id, position: 0, duration: 0, heardAt: 0, marks: {} };
    journal.records[id] = { ...before, ...change(before), seq: journal.seq };
    write(journalKey, journal);
  };

  const save = () => {
    if (!item) return;
    const position = audio.currentTime || 0;
    const duration = Number.isFinite(audio.duration) ? audio.duration : item.duration;
    record(item.id, () => ({ position, duration, heardAt: Date.now() }));
    write(currentKey, { item, position });
    lastSaved = Date.now();
  };

  const state = () => ({
    item,
    playing: !audio.paused && !audio.ended,
    buffering: !audio.paused && audio.readyState < 3,
    position: audio.currentTime || 0,
    duration: Number.isFinite(audio.duration) ? audio.duration : item?.duration ?? 0,
    rate: audio.playbackRate,
    sleepAt,
    sleepAtEnd,
    ended,
    problem,
  });
  const emit = () => send(state());

  const clearSleep = () => {
    clearTimeout(sleepTimer);
    sleepTimer = null;
    sleepAt = null;
    sleepAtEnd = false;
  };

  const loadItem = (next, startAt, play) => {
    item = next;
    ended = false;
    problem = null;
    audio.src = next.url;
    audio.currentTime = 0;
    const start = () => {
      if (startAt > 0) audio.currentTime = startAt;
      if (play) audio.play().catch(() => emit());
    };
    if (audio.readyState >= 1) start();
    else audio.addEventListener('loadedmetadata', start, { once: true });
    write(currentKey, { item, position: startAt });
    emit();
  };

  audio.addEventListener('timeupdate', () => {
    if (!audio.paused && Date.now() - lastSaved >= saveEvery) save();
    emit();
  });
  ['play', 'pause', 'waiting', 'playing', 'ratechange', 'durationchange'].forEach((name) => audio.addEventListener(name, () => {
    if (name === 'pause') save();
    emit();
  }));
  audio.addEventListener('seeked', () => { save(); emit(); });
  audio.addEventListener('error', () => { problem = 'This sermon couldn’t be played.'; emit(); });
  audio.addEventListener('ended', () => {
    save();
    if (sleepAtEnd) {
      clearSleep();
      ended = true;
      emit();
      return;
    }
    const next = upNext.shift();
    if (next) loadItem(next, 0, true);
    else { ended = true; emit(); }
  });

  return {
    start() {
      const current = read(currentKey, null);
      if (current?.item && !item) {
        item = current.item;
        audio.src = item.url;
        audio.addEventListener('loadedmetadata', () => { audio.currentTime = current.position || 0; emit(); }, { once: true });
        return { ...state(), position: current.position || 0 };
      }
      return state();
    },
    load: (next, startAt, play) => loadItem(JSON.parse(next), startAt, play),
    play: () => audio.play().catch(() => emit()),
    pause: () => audio.pause(),
    seek: (to) => { audio.currentTime = Math.max(0, to); },
    skip: (by) => { audio.currentTime = Math.max(0, (audio.currentTime || 0) + by); },
    rate: (rate) => { audio.playbackRate = rate; },
    sleep: (minutes, endOfItem) => {
      clearSleep();
      if (endOfItem) sleepAtEnd = true;
      else if (minutes > 0) {
        sleepAt = Date.now() + minutes * 60000;
        sleepTimer = setTimeout(() => { audio.pause(); clearSleep(); emit(); }, minutes * 60000);
      }
      emit();
    },
    upNext: (items) => { upNext = JSON.parse(items); },
    stop: () => {
      save();
      audio.pause();
      audio.removeAttribute('src');
      item = null;
      clearSleep();
      try { localStorage.removeItem(currentKey); } catch { }
      emit();
    },
    mark: (id, mark, at) => record(id, (before) => ({ marks: { ...before.marks, [mark]: at } })),
    journal: (since) => {
      const journal = read(journalKey, { seq: 0, records: {} });
      return JSON.stringify({ records: Object.values(journal.records).filter((entry) => entry.seq > since), latestSeq: journal.seq });
    },
  };
})();

const player = () => (isNative() ? native : web);

export async function start(dotnet) {
  receiver = dotnet;
  return JSON.stringify(await player().start());
}

export const load = (item, startAt, play) => player().load(item, startAt, play);
export const play = () => player().play();
export const pause = () => player().pause();
export const seek = (to) => player().seek(to);
export const skip = (by) => player().skip(by);
export const rate = (value) => player().rate(value);
export const sleep = (minutes, endOfItem) => player().sleep(minutes, endOfItem);
export const upNext = (items) => player().upNext(items);
export const stop = () => player().stop();
export const mark = (id, name, at) => player().mark(id, name, at);
export const journal = (since) => player().journal(since);

export function listenDownloads(dotnet) {
  if (isNative()) dataLayer().addListener('downloadsChanged', () => dotnet.invokeMethodAsync('OnDownloadsChanged'));
}
export const downloads = () => (isNative() ? dataLayer().audioDownloads().then((result) => result.json) : '[]');
export const download = (id, url, title) => dataLayer().audioDownload({ id, url, title });
export const removeDownload = (id) => dataLayer().audioRemoveDownload({ id });
