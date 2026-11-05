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
    if (!(page instanceof Element) || !page.matches('.page-scroll, .phone-frame__screen')) return;
    page.querySelector('.topbar')?.classList.toggle('is-stuck', page.scrollTop > Number(page.dataset.stuckAt ?? 44));
  }, { capture: true, passive: true });

  const edgeSwipe = () => {
    const edge = 24;
    let start = null;
    document.addEventListener('touchstart', (event) => {
      const page = scroller();
      const touch = event.touches[0];
      const own = event.target instanceof Element && !!event.target.closest('.now-playing, .sheet-layer');
      start = page?.dataset.backHref && touch.clientX < edge && !own ? { x: touch.clientX, y: touch.clientY, page, dx: 0, at: event.timeStamp } : null;
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

    fitToSpace(frame, width, height) {
      const bezel = 18;
      const update = () => {
        const holder = frame.parentElement;
        if (!holder) return;
        const above = frame.getBoundingClientRect().top - holder.getBoundingClientRect().top;
        const chrome = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--chrome-top')) || 0;
        const tall = holder.hasAttribute('data-fit-height')
          ? holder.clientHeight - above
          : innerHeight - Math.max(chrome + 16, holder.getBoundingClientRect().top) - above - 24;
        const scale = Math.max(parseFloat(holder.dataset.fitLeast) || 0.4, Math.min(1, holder.clientWidth / (width + bezel), tall / (height + bezel)));
        const spare = 1 - scale;
        frame.style.transform = `scale(${scale.toFixed(3)})`;
        frame.style.transformOrigin = 'top center';
        frame.style.marginBottom = `${9 - height * spare}px`;
        frame.style.marginInline = `${9 - (width * spare) / 2}px`;
        frame.dataset.scale = scale.toFixed(3);
      };
      frame.style.transform = '';
      frame.style.margin = '';
      update();
      if (!frame.gsbcFit) {
        frame.gsbcFit = new ResizeObserver(update);
        frame.gsbcFit.observe(frame.parentElement);
        addEventListener('resize', update);
      }
      frame.gsbcFitUpdate = update;
    },

    openWindow(url, name, width, height) {
      open(url, name, `popup,width=${width},height=${height}`);
    },

    tell(channel, message) {
      const sender = new BroadcastChannel(channel);
      sender.postMessage(message);
      sender.close();
    },

    hear(channel, receiver) {
      const listener = new BroadcastChannel(channel);
      listener.onmessage = (event) => receiver.invokeMethodAsync('OnHeard', String(event.data));
      return { close: () => listener.close() };
    },

    undoKeys(receiver) {
      const typing = (target) => target instanceof Element && !!target.closest('input, textarea, select, [contenteditable="true"], dialog[open]');
      const key = (event) => {
        const letter = event.key.toLowerCase();
        if (!(event.metaKey || event.ctrlKey) || event.altKey || (letter !== 'z' && letter !== 'y') || typing(event.target) || !document.querySelector('[data-undo-keys]')) return;
        event.preventDefault();
        receiver.invokeMethodAsync('OnHeard', letter === 'y' || event.shiftKey ? 'redo' : 'undo');
      };
      document.addEventListener('keydown', key);
      return { close: () => document.removeEventListener('keydown', key) };
    },

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

    showModal(dialog) {
      if (!dialog || dialog.open || typeof dialog.showModal !== 'function') return;
      dialog.addEventListener('cancel', (event) => event.preventDefault());
      dialog.showModal();
    },

    async copy(text) {
      try {
        await navigator.clipboard.writeText(text);
        return true;
      } catch {
        return false;
      }
    },

    openExternal(url) {
      if (isNative()) window.location.href = url;
      else window.open(url, '_blank', 'noopener');
    },

    swipeAway(element, receiver) {
      const ease = 'cubic-bezier(0.22, 1, 0.36, 1)';
      let start = null;
      let swiped = false;
      const place = (x, y) => {
        element.style.transform = reducedMotion.matches ? '' : `translate(${x}px, ${y}px)`;
        element.style.opacity = String(1 - Math.min(0.8, Math.max(Math.abs(x) / element.offsetWidth, y / element.offsetHeight / 2)));
      };
      const settle = () => {
        element.style.transition = `transform 200ms ${ease}, opacity 200ms ${ease}`;
        place(0, 0);
        element.style.opacity = '';
        setTimeout(() => { element.style.transition = ''; }, 220);
      };
      const onStart = (event) => {
        const touch = event.touches[0];
        start = { x: touch.clientX, y: touch.clientY, at: event.timeStamp, axis: null, dx: 0, dy: 0 };
        swiped = false;
      };
      const onMove = (event) => {
        if (!start) return;
        const touch = event.touches[0];
        const dx = touch.clientX - start.x;
        const dy = touch.clientY - start.y;
        if (!start.axis) {
          if (Math.hypot(dx, dy) < 8) return;
          start.axis = Math.abs(dx) > Math.abs(dy) ? 'x' : dy > 0 ? 'y' : null;
          if (!start.axis) { start = null; return; }
        }
        event.preventDefault();
        swiped = true;
        start.dx = start.axis === 'x' ? dx : 0;
        start.dy = start.axis === 'y' ? Math.max(0, dy) : 0;
        element.style.transition = '';
        place(start.dx, start.dy);
      };
      const onEnd = (event) => {
        if (!start?.axis) { start = null; return; }
        const { dx, dy, at, axis } = start;
        start = null;
        const speed = Math.max(Math.abs(dx), dy) / Math.max(1, event.timeStamp - at);
        const away = axis === 'x'
          ? Math.abs(dx) > element.offsetWidth * 0.35 || (speed > 0.5 && Math.abs(dx) > 40)
          : dy > element.offsetHeight * 0.8 || (speed > 0.5 && dy > 24);
        if (!away) { settle(); return; }
        window.gsbcShell.haptic('light');
        element.style.transition = `transform 180ms ${ease}, opacity 180ms ${ease}`;
        place(axis === 'x' ? Math.sign(dx) * element.offsetWidth * 1.1 : 0, axis === 'y' ? element.offsetHeight * 1.5 : 0);
        element.style.opacity = '0';
        setTimeout(() => receiver.invokeMethodAsync('OnSwipedAway'), reducedMotion.matches ? 0 : 170);
      };
      const onClick = (event) => {
        if (!swiped) return;
        swiped = false;
        event.stopPropagation();
        event.preventDefault();
      };
      element.addEventListener('touchstart', onStart, { passive: true });
      element.addEventListener('touchmove', onMove, { passive: false });
      element.addEventListener('touchend', onEnd);
      element.addEventListener('touchcancel', settle);
      element.addEventListener('click', onClick, true);
      return {
        dispose() {
          element.removeEventListener('touchstart', onStart);
          element.removeEventListener('touchmove', onMove);
          element.removeEventListener('touchend', onEnd);
          element.removeEventListener('touchcancel', settle);
          element.removeEventListener('click', onClick, true);
        },
      };
    },

    sheetDrag(sheet, receiver) {
      const ease = 'cubic-bezier(0.22, 1, 0.36, 1)';
      const backdrop = () => sheet.parentElement?.querySelector('.sheet-backdrop');
      const docked = () => getComputedStyle(sheet).bottom === '0px';
      const fixed = 'input, textarea, select, [contenteditable="true"], .chips, .no-sheet-drag';
      let start = null;
      const place = (dy) => {
        sheet.style.transform = `translateY(${dy}px)`;
        const scrim = backdrop();
        if (scrim) scrim.style.opacity = String(1 - Math.min(1, dy / sheet.offsetHeight));
      };
      const settleIn = reducedMotion.matches ? 0 : 240;
      const reset = () => {
        sheet.style.transition = settleIn ? `transform ${settleIn}ms ${ease}` : 'none';
        place(0);
        const scrim = backdrop();
        if (scrim) { scrim.style.transition = settleIn ? `opacity ${settleIn}ms` : 'none'; scrim.style.opacity = ''; }
        setTimeout(() => { sheet.style.transition = ''; sheet.style.transform = ''; if (scrim) scrim.style.transition = ''; }, settleIn + 20);
      };
      const onStart = (event) => {
        const target = event.target instanceof Element ? event.target : null;
        const onHandle = !!target?.closest('.sheet__grabber, .sheet__cover');
        start = docked() && (onHandle || sheet.scrollTop <= 0) && !target?.closest(fixed)
          ? { y: event.touches[0].clientY, x: event.touches[0].clientX, at: event.timeStamp, dy: 0, dragging: false }
          : null;
      };
      const onMove = (event) => {
        if (!start) return;
        const touch = event.touches[0];
        const dy = touch.clientY - start.y;
        const dx = touch.clientX - start.x;
        if (!start.dragging) {
          if (Math.hypot(dx, dy) < 6) return;
          if (dy <= 0 || Math.abs(dx) > dy || sheet.scrollTop > 0) { start = null; return; }
          start.dragging = true;
          sheet.style.animation = 'none';
          sheet.style.transition = '';
        }
        event.preventDefault();
        start.dy = Math.max(0, dy);
        place(start.dy);
      };
      const onEnd = (event) => {
        if (!start?.dragging) { start = null; return; }
        const { dy, at } = start;
        start = null;
        const speed = dy / Math.max(1, event.timeStamp - at);
        if (dy > sheet.offsetHeight * 0.3 || (speed > 0.6 && dy > 30)) {
          const away = reducedMotion.matches ? 0 : 200;
          sheet.style.transition = away ? `transform ${away}ms ${ease}` : 'none';
          place(sheet.offsetHeight + 24);
          setTimeout(() => receiver.invokeMethodAsync('OnDragDismissed'), away ? away - 10 : 0);
        } else {
          reset();
        }
      };
      sheet.addEventListener('touchstart', onStart, { passive: true });
      sheet.addEventListener('touchmove', onMove, { passive: false });
      sheet.addEventListener('touchend', onEnd);
      const onCancel = () => { if (start?.dragging) reset(); start = null; };
      sheet.addEventListener('touchcancel', onCancel);
      return {
        dispose() {
          sheet.removeEventListener('touchstart', onStart);
          sheet.removeEventListener('touchmove', onMove);
          sheet.removeEventListener('touchend', onEnd);
          sheet.removeEventListener('touchcancel', onCancel);
        },
      };
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
