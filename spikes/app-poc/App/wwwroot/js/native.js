(() => {
  const cap = () => window.Capacitor;
  const isNative = () => !!cap()?.isNativePlatform?.();
  const platform = () => cap()?.getPlatform?.() ?? 'web';
  const plugins = new Map();
  const plugin = (name) => {
    if (!isNative() || !cap().isPluginAvailable?.(name)) return null;
    if (!plugins.has(name)) plugins.set(name, cap().registerPlugin(name));
    return plugins.get(name);
  };

  const storage = {
    get(key) {
      try { return localStorage.getItem(`gsbc:${key}`); } catch { return null; }
    },
    set(key, value) {
      try { localStorage.setItem(`gsbc:${key}`, value); } catch { }
    },
  };

  const darkQuery = window.matchMedia('(prefers-color-scheme: dark)');
  const applyScheme = () => {
    const root = document.documentElement;
    const appearance = root.dataset.appearance ?? 'System';
    const dark = appearance === 'Dark' || (appearance === 'System' && darkQuery.matches);
    root.dataset.scheme = dark ? 'dark' : 'light';
  };
  darkQuery.addEventListener('change', applyScheme);

  const scrollerOf = (element) => element?.closest('.page-scroll');
  const stuckHeight = (scroller) => {
    const sticky = scroller.querySelector('.section-tabs') ?? scroller.querySelector('.topbar');
    return sticky ? (parseFloat(getComputedStyle(sticky).top) || 0) + sticky.offsetHeight : 0;
  };

  const player = {
    audio: new Audio(),
    listener: null,
    id: null,
    report() {
      const a = player.audio;
      player.listener?.invokeMethodAsync('OnTick', player.id ?? '', a.currentTime || 0, a.duration || 0, !a.paused);
    },
  };
  player.audio.preload = 'metadata';
  ['timeupdate', 'play', 'pause', 'loadedmetadata', 'ended', 'error'].forEach((e) => player.audio.addEventListener(e, player.report));

  let installPrompt = null;
  window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault();
    installPrompt = e;
  });

  window.gsbc = {
    isNative,
    platform,

    displayMode() {
      if (isNative()) return 'native';
      if (matchMedia('(display-mode: standalone)').matches || navigator.standalone) return 'standalone';
      return /iPhone|iPad|iPod/.test(navigator.userAgent) || (navigator.maxTouchPoints > 1 && /Macintosh/.test(navigator.userAgent))
        ? 'browser-ios'
        : installPrompt ? 'browser-installable' : 'browser';
    },

    async install() {
      if (!installPrompt) return false;
      installPrompt.prompt();
      const { outcome } = await installPrompt.userChoice;
      installPrompt = null;
      return outcome === 'accepted';
    },

    async haptic(kind) {
      const haptics = plugin('Haptics');
      if (!haptics) return 'unavailable';
      try {
        if (kind === 'success' || kind === 'warning') await haptics.notification({ type: kind.toUpperCase() });
        else await haptics.impact({ style: kind === 'medium' ? 'MEDIUM' : 'LIGHT' });
        return 'ok';
      } catch (e) {
        return `error: ${e?.message ?? e}`;
      }
    },

    async share(title, text, url) {
      const share = plugin('Share');
      if (share) return share.share({ title, text, url, dialogTitle: title }).catch(() => { });
      if (navigator.share) return navigator.share({ title, text, url }).catch(() => { });
      return navigator.clipboard?.writeText(url);
    },

    async openInApp(url) {
      const browser = plugin('Browser');
      if (browser) return browser.open({ url, presentationStyle: 'popover' });
      window.open(url, '_blank', 'noopener');
    },

    openExternal(url) {
      if (isNative()) window.location.href = url;
      else window.open(url, '_blank', 'noopener');
    },

    async addToCalendar(title, startMs, endMs, location, notes) {
      const calendar = plugin('CapacitorCalendar');
      if (!calendar) return 'web';
      try {
        const result = await calendar.createEventWithPrompt({ title, startDate: startMs, endDate: endMs, location, description: notes, alerts: [-60] });
        return result?.id ? 'added' : 'cancelled';
      } catch (e) {
        return `error: ${e?.message ?? e}`;
      }
    },

    async scheduleNotification(title, body, secondsFromNow) {
      const notifications = plugin('LocalNotifications');
      if (!notifications) return 'web';
      try {
        const permission = await notifications.requestPermissions();
        if (permission.display !== 'granted') return `permission: ${permission.display}`;
        await notifications.schedule({
          notifications: [{ id: Date.now() % 2147483647, title, body, schedule: { at: new Date(Date.now() + secondsFromNow * 1000) } }],
        });
        return 'scheduled';
      } catch (e) {
        return `error: ${e?.message ?? e}`;
      }
    },

    get: storage.get,
    set: storage.set,

    applyTheme(theme, appearance, layout) {
      const root = document.documentElement;
      root.dataset.theme = theme === 'Year2026' ? 'year' : 'brand';
      root.dataset.appearance = appearance;
      root.dataset.layout = layout;
      applyScheme();
    },

    markFirstRender() {
      window.__gsbcFirstRender ??= performance.now();
      plugin('SplashScreen')?.hide({ fadeOutDuration: 200 });
      plugin('StatusBar')?.setStyle({ style: 'DARK' });
    },

    back(fallback) {
      if (history.state?._index > 0) history.back();
      else window.Blazor.navigateTo(fallback);
    },

    scrollToSection(id) {
      const target = document.getElementById(id);
      const scroller = scrollerOf(target);
      if (!scroller) return;
      const top = scroller.scrollTop + target.getBoundingClientRect().top - scroller.getBoundingClientRect().top - stuckHeight(scroller) - 12;
      scroller.scrollTo({ top, behavior: 'smooth' });
    },

    scrollToTop() {
      document.querySelector('.page-scroll')?.scrollTo({ top: 0, behavior: 'smooth' });
    },

    spySections(nav) {
      const scroller = scrollerOf(nav);
      if (!scroller) return;
      const buttons = [...nav.querySelectorAll('button[data-target]')];
      const ink = nav.querySelector('.section-tabs__ink');
      let last = null;
      const update = () => {
        const line = nav.getBoundingClientRect().bottom + 24;
        let current = buttons[0];
        for (const b of buttons) {
          const target = document.getElementById(b.dataset.target);
          if (target && target.getBoundingClientRect().top <= line) current = b;
        }
        if (scroller.scrollTop > 0 && scroller.scrollTop + scroller.clientHeight >= scroller.scrollHeight - 4) current = buttons[buttons.length - 1];
        if (current === last) return;
        last = current;
        buttons.forEach((b) => b.classList.toggle('on', b === current));
        if (ink) {
          ink.style.width = `${current.offsetWidth}px`;
          ink.style.transform = `translateX(${current.offsetLeft}px)`;
        }
        nav.scrollTo({ left: current.offsetLeft - (nav.clientWidth - current.offsetWidth) / 2, behavior: 'smooth' });
      };
      scroller.addEventListener('scroll', update, { passive: true });
      update();
    },

    positions() {
      const out = {};
      try {
        for (let i = 0; i < localStorage.length; i++) {
          const key = localStorage.key(i);
          if (!key?.startsWith('gsbc:position:')) continue;
          const id = key.slice(14);
          const position = Number(localStorage.getItem(key)) || 0;
          const length = Number(localStorage.getItem(`gsbc:length:${id}`)) || 0;
          if (position > 30 && length > 0 && position < length - 30) out[id] = position / length;
        }
      } catch { }
      return JSON.stringify(out);
    },

    player: {
      listen(dotnet) { player.listener = dotnet; },
      load(id, src, title, artist, artwork, startAt) {
        const a = player.audio;
        if (player.id !== id) {
          player.id = id;
          a.src = src;
          a.addEventListener('loadedmetadata', () => { if (startAt > 0) a.currentTime = startAt; }, { once: true });
        }
        if ('mediaSession' in navigator) {
          navigator.mediaSession.metadata = new MediaMetadata({ title, artist, album: 'Good Shepherd Baptist Church', artwork: artwork ? [{ src: artwork, sizes: '1920x1080' }] : [] });
          navigator.mediaSession.setActionHandler('play', () => a.play());
          navigator.mediaSession.setActionHandler('pause', () => a.pause());
          navigator.mediaSession.setActionHandler('seekbackward', () => { a.currentTime = Math.max(0, a.currentTime - 15); });
          navigator.mediaSession.setActionHandler('seekforward', () => { a.currentTime += 30; });
        }
        return a.play().then(() => true).catch(() => false);
      },
      toggle() {
        const a = player.audio;
        if (!player.id) return Promise.resolve(false);
        if (a.paused) return a.play().then(() => true).catch(() => false);
        a.pause();
        return Promise.resolve(false);
      },
      seek(seconds) { player.audio.currentTime = Math.max(0, Math.min(player.audio.duration || 0, seconds)); },
      skip(delta) { player.audio.currentTime = Math.max(0, player.audio.currentTime + delta); },
    },

    map: {
      attach(viewport, dotnet) {
        const canvas = viewport.querySelector('.site-map__canvas');
        const view = { scale: 1, x: 0, y: 0 };
        const pointers = new Map();
        let gesture = null;
        let moved = false;
        let lastTap = 0;

        const minScale = () => Math.max(1, viewport.clientHeight / canvas.offsetHeight);
        const bound = (scale) => Math.min(minScale() * 4, Math.max(minScale(), scale));
        const clamp = () => {
          view.scale = bound(view.scale);
          const w = viewport.clientWidth, h = viewport.clientHeight;
          const cw = canvas.offsetWidth * view.scale, ch = canvas.offsetHeight * view.scale;
          view.x = cw <= w ? (w - cw) / 2 : Math.min(0, Math.max(w - cw, view.x));
          view.y = ch <= h ? (h - ch) / 2 : Math.min(0, Math.max(h - ch, view.y));
        };
        const apply = (animate) => {
          clamp();
          canvas.style.transition = animate ? 'transform 320ms cubic-bezier(0.22, 1, 0.36, 1)' : 'none';
          canvas.style.transform = `translate(${view.x}px, ${view.y}px) scale(${view.scale})`;
          canvas.style.setProperty('--inv-zoom', Math.min(1, 1.5 / view.scale));
        };
        const local = (e) => {
          const r = viewport.getBoundingClientRect();
          return { x: e.clientX - r.left, y: e.clientY - r.top };
        };
        const zoomAt = (point, scale, animate) => {
          const contentX = (point.x - view.x) / view.scale, contentY = (point.y - view.y) / view.scale;
          view.scale = bound(scale);
          view.x = point.x - contentX * view.scale;
          view.y = point.y - contentY * view.scale;
          apply(animate);
        };
        const start = () => {
          const [a, b] = [...pointers.values()];
          gesture = b
            ? { mid: { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 }, distance: Math.hypot(a.x - b.x, a.y - b.y), ...view }
            : { origin: { ...a }, ...view };
        };
        const tap = (e) => {
          const target = document.elementFromPoint(e.clientX, e.clientY);
          const mark = target?.closest('[data-mark]');
          const space = target?.closest('[data-space]');
          if (mark) dotnet.invokeMethodAsync('Tapped', 'mark', mark.dataset.mark);
          else if (space) dotnet.invokeMethodAsync('Tapped', 'space', space.dataset.space);
        };

        viewport.addEventListener('pointerdown', (e) => {
          viewport.setPointerCapture(e.pointerId);
          pointers.set(e.pointerId, local(e));
          if (pointers.size === 1) moved = false;
          start();
        });
        viewport.addEventListener('pointermove', (e) => {
          if (!pointers.has(e.pointerId)) return;
          pointers.set(e.pointerId, local(e));
          const [a, b] = [...pointers.values()];
          if (b && gesture.distance) {
            const mid = { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 };
            const scale = bound(gesture.scale * Math.hypot(a.x - b.x, a.y - b.y) / gesture.distance);
            const contentX = (gesture.mid.x - gesture.x) / gesture.scale, contentY = (gesture.mid.y - gesture.y) / gesture.scale;
            Object.assign(view, { scale, x: mid.x - contentX * scale, y: mid.y - contentY * scale });
            moved = true;
          } else if (gesture.origin) {
            const dx = a.x - gesture.origin.x, dy = a.y - gesture.origin.y;
            if (Math.hypot(dx, dy) > 6) moved = true;
            Object.assign(view, { x: gesture.x + dx, y: gesture.y + dy });
          }
          apply(false);
        });
        const end = (e) => {
          if (!pointers.delete(e.pointerId)) return;
          if (pointers.size > 0) { start(); return; }
          if (moved || e.type === 'pointercancel') return;
          const now = performance.now();
          if (now - lastTap < 300) {
            lastTap = 0;
            zoomAt(local(e), view.scale > minScale() * 1.8 ? minScale() : view.scale * 2.2, true);
            return;
          }
          lastTap = now;
          tap(e);
        };
        viewport.addEventListener('pointerup', end);
        viewport.addEventListener('pointercancel', end);
        viewport.addEventListener('wheel', (e) => {
          e.preventDefault();
          zoomAt(local(e), view.scale * Math.exp(-e.deltaY / 300), false);
        }, { passive: false });
        new ResizeObserver(() => apply(false)).observe(viewport);
        viewport.gsbcMap = { view, apply, zoomAt };
        view.scale = minScale();
        view.x = (viewport.clientWidth - canvas.offsetWidth * view.scale) / 2;
        apply(false);
      },

      focus(viewport, fx, fy, scale) {
        const map = viewport.gsbcMap;
        if (!map) return;
        const canvas = viewport.querySelector('.site-map__canvas');
        map.view.scale = scale;
        map.view.x = viewport.clientWidth / 2 - fx * canvas.offsetWidth * scale;
        map.view.y = viewport.clientHeight / 2 - fy * canvas.offsetHeight * scale;
        map.apply(true);
      },
    },

    updates: {
      waiting: false,
      listener: null,
      lastCheck: 0,
      watch(registration) {
        const updates = window.gsbc.updates;
        updates.lastCheck = Date.now();
        const installed = () => navigator.serviceWorker.controller && updates.ready();
        if (registration.waiting) installed();
        registration.addEventListener('updatefound', () => {
          const next = registration.installing;
          next?.addEventListener('statechange', () => { if (next.state === 'installed') installed(); });
        });
        const check = () => {
          if (Date.now() - updates.lastCheck < 30_000) return;
          updates.lastCheck = Date.now();
          registration.update().catch(() => { });
        };
        document.addEventListener('visibilitychange', () => { if (document.visibilityState === 'visible') check(); });
        setInterval(check, 5 * 60_000);
      },
      ready() {
        const updates = window.gsbc.updates;
        if (updates.waiting) return;
        updates.waiting = true;
        updates.listener?.invokeMethodAsync('UpdateReady');
      },
      onReady(listener) {
        const updates = window.gsbc.updates;
        updates.listener = listener;
        if (updates.waiting) listener.invokeMethodAsync('UpdateReady');
      },
      async apply() {
        try {
          const registration = await navigator.serviceWorker?.getRegistration();
          const next = registration?.waiting ?? registration?.installing;
          if (next) {
            const switched = new Promise((resolve) => {
              navigator.serviceWorker.addEventListener('controllerchange', resolve, { once: true });
              setTimeout(resolve, 10_000);
            });
            if (next.state === 'installing') {
              await new Promise((resolve) => next.addEventListener('statechange', () => next.state !== 'installing' && resolve()));
            }
            next.postMessage('skip-waiting');
            await switched;
          }
        } catch { }
        location.reload();
      },
    },

    async probe(name) {
      try {
        switch (name) {
          case 'runtime':
            return `${isNative() ? `Capacitor ${platform()}` : 'Browser'}`;
          case 'plugins': {
            const names = ['Haptics', 'Share', 'Browser', 'LocalNotifications', 'CapacitorCalendar', 'StatusBar', 'SplashScreen'];
            if (!isNative()) return 'Browser: native plugins not present';
            const missing = names.filter((n) => !cap().isPluginAvailable?.(n));
            return missing.length ? `Missing: ${missing.join(', ')}` : `All ${names.length} available`;
          }
          case 'startup': {
            const nav = performance.getEntriesByType('navigation')[0];
            const firstPaint = window.__gsbcFirstRender ?? 0;
            return `${Math.round(firstPaint)} ms to first screen${nav ? ` · ${Math.round(nav.duration)} ms page load` : ''}`;
          }
          case 'safe-area': {
            const probe = document.createElement('div');
            probe.style.cssText = 'position:fixed;top:0;padding-top:env(safe-area-inset-top);padding-bottom:env(safe-area-inset-bottom);visibility:hidden';
            document.body.appendChild(probe);
            const style = getComputedStyle(probe);
            const result = `top ${style.paddingTop} · bottom ${style.paddingBottom}`;
            probe.remove();
            return result;
          }
          case 'storage': {
            const estimate = await navigator.storage?.estimate?.();
            const persisted = await navigator.storage?.persisted?.();
            return estimate ? `${(estimate.usage / 1048576).toFixed(1)} MB used · persisted: ${persisted}` : 'No StorageManager';
          }
          default:
            return 'Unknown probe';
        }
      } catch (e) {
        return `error: ${e?.message ?? e}`;
      }
    },
  };

  document.addEventListener('scroll', (e) => {
    const scroller = e.target;
    if (!(scroller instanceof Element) || !scroller.classList.contains('page-scroll')) return;
    scroller.querySelector('.topbar')?.classList.toggle('is-stuck', scroller.scrollTop > (scroller.dataset.stuckAt ?? 44));
  }, { capture: true, passive: true });

  window.addEventListener('DOMContentLoaded', () => {
    plugin('App')?.addListener('backButton', () => history.length > 1 && history.back());
  });

  if (!isNative() && 'serviceWorker' in navigator && location.protocol === 'https:') {
    navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' })
      .then((registration) => window.gsbc.updates.watch(registration))
      .catch(() => { });
  }
})();
