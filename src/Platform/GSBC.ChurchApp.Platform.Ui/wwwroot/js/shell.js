window.gsbcShell = (() => {
  const root = document.documentElement;
  const cap = () => window.Capacitor;
  const isNative = () => !!cap()?.isNativePlatform?.();
  const plugin = (name) => (isNative() && cap().isPluginAvailable?.(name) ? cap().Plugins[name] : null);
  const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
  const scroller = () => document.querySelector('.page-scroll');

  const pushState = history.pushState;
  history.pushState = function (...args) {
    root.dataset.nav = 'forward';
    return pushState.apply(this, args);
  };
  addEventListener('popstate', () => { root.dataset.nav = 'back'; });

  const back = (fallback) => {
    if (history.state?._index > 0) history.back();
    else window.Blazor.navigateTo(fallback);
  };

  document.addEventListener('scroll', (event) => {
    const page = event.target;
    if (!(page instanceof Element) || !page.classList.contains('page-scroll')) return;
    page.querySelector('.topbar')?.classList.toggle('is-stuck', page.scrollTop > Number(page.dataset.stuckAt ?? 44));
  }, { capture: true, passive: true });

  const edgeSwipe = () => {
    const edge = 24;
    let start = null;
    document.addEventListener('touchstart', (event) => {
      const page = scroller();
      const touch = event.touches[0];
      start = page?.dataset.backHref && touch.clientX < edge ? { x: touch.clientX, y: touch.clientY, page, dx: 0, at: event.timeStamp } : null;
    }, { passive: true });
    document.addEventListener('touchmove', (event) => {
      if (!start) return;
      const touch = event.touches[0];
      start.dx = Math.max(0, touch.clientX - start.x);
      if (Math.abs(touch.clientY - start.y) > start.dx) { start.page.style.transform = ''; start = null; return; }
      if (!reducedMotion.matches) start.page.style.transform = `translateX(${start.dx}px)`;
    }, { passive: true });
    document.addEventListener('touchend', (event) => {
      if (!start) return;
      const { page, dx, at } = start;
      start = null;
      const fast = dx / Math.max(1, event.timeStamp - at) > 0.5;
      if (dx > innerWidth * 0.35 || (fast && dx > 40)) {
        window.gsbcShell.haptic('light');
        page.style.transition = 'transform 180ms cubic-bezier(0.22, 1, 0.36, 1)';
        page.style.transform = reducedMotion.matches ? '' : 'translateX(100%)';
        setTimeout(() => back(page.dataset.backHref), reducedMotion.matches ? 0 : 160);
      } else {
        page.style.transition = 'transform 200ms cubic-bezier(0.22, 1, 0.36, 1)';
        page.style.transform = '';
        setTimeout(() => { page.style.transition = ''; }, 220);
      }
    });
  };
  edgeSwipe();

  return {
    isNative,

    onReturn(receiver) {
      document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') receiver.invokeMethodAsync('OnReturned');
      });
    },

    async haptic(kind) {
      const haptics = plugin('Haptics');
      if (!haptics) return;
      try {
        if (kind === 'success' || kind === 'warning') await haptics.notification({ type: kind.toUpperCase() });
        else await haptics.impact({ style: kind === 'medium' ? 'MEDIUM' : 'LIGHT' });
      } catch { }
    },

    started() {
      plugin('StatusBar')?.setStyle({ style: 'DARK' }).catch(() => { });
      document.querySelector('.boot')?.remove();
    },

    back,

    scrollToTop() {
      scroller()?.scrollTo({ top: 0, behavior: reducedMotion.matches ? 'auto' : 'smooth' });
    },

    openExternal(url) {
      if (isNative()) window.location.href = url;
      else window.open(url, '_blank', 'noopener');
    },

    pullToRefresh(host, receiver) {
      const page = host.closest('.page-scroll');
      const indicator = host.querySelector('.pull__indicator');
      if (!page || !indicator) return { dispose() { } };
      const threshold = 72;
      let start = null;
      let pull = 0;
      let busy = false;

      const show = (distance) => {
        pull = distance;
        host.style.setProperty('--pull', `${distance}px`);
        host.style.setProperty('--pull-n', distance);
        host.classList.toggle('is-ready', distance >= threshold);
      };
      const onStart = (event) => {
        start = !busy && page.scrollTop <= 0 ? event.touches[0].clientY : null;
      };
      const onMove = (event) => {
        if (start === null) return;
        const dy = event.touches[0].clientY - start;
        if (dy <= 0) { show(0); return; }
        event.preventDefault();
        const next = Math.min(threshold * 1.6, dy * 0.5);
        if (pull < threshold && next >= threshold) window.gsbcShell.haptic('light');
        host.classList.add('is-pulling');
        show(next);
      };
      const onEnd = async () => {
        if (start === null) return;
        start = null;
        host.classList.remove('is-pulling');
        if (pull < threshold) { show(0); return; }
        busy = true;
        host.classList.add('is-refreshing');
        show(threshold);
        try { await receiver.invokeMethodAsync('Refresh'); } catch { }
        busy = false;
        host.classList.remove('is-refreshing');
        show(0);
      };

      page.addEventListener('touchstart', onStart, { passive: true });
      page.addEventListener('touchmove', onMove, { passive: false });
      page.addEventListener('touchend', onEnd);
      page.addEventListener('touchcancel', onEnd);
      return {
        dispose() {
          page.removeEventListener('touchstart', onStart);
          page.removeEventListener('touchmove', onMove);
          page.removeEventListener('touchend', onEnd);
          page.removeEventListener('touchcancel', onEnd);
        },
      };
    },
  };
})();
