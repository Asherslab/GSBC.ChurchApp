window.gsbcShell = (() => {
  const cap = () => window.Capacitor;
  const isNative = () => !!cap()?.isNativePlatform?.();
  const plugin = (name) => (isNative() && cap().isPluginAvailable?.(name) ? cap().Plugins[name] : null);
  const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
  const scroller = () => document.querySelector('.page-scroll');
  const mastheadLead = 16;
  const focusBeforeModal = [];

  const back = (fallback) => {
    if (history.state?._index > 0) history.back();
    else window.Blazor.navigateTo(fallback);
  };

  const keepPlaces = () => {
    const places = new Map();
    const holdUnseen = 400;
    const giveUpAfter = 1500;
    const settledFor = 200;
    const interruptions = ['touchstart', 'pointerdown', 'wheel', 'keydown'];
    let here = history.state?._index ?? 0;
    let returning = null;

    const stopReturning = () => {
      if (!returning) return;
      const { page, frame } = returning;
      returning = null;
      cancelAnimationFrame(frame);
      interruptions.forEach((kind) => removeEventListener(kind, stopReturning, true));
      if (!page?.classList.contains('is-returning')) return;
      page.classList.remove('is-returning');
      if (reducedMotion.matches) return;
      page.classList.add('is-returned');
      page.addEventListener('animationend', () => page.classList.remove('is-returned'), { once: true });
    };

    const hold = (page) => {
      if (returning.page === page) return;
      returning.page?.classList.remove('is-returning');
      returning.page = page;
      page?.classList.add('is-returning');
    };

    const looksOf = (page) => [page, page?.querySelector('.topbar__title')?.textContent, page?.querySelector(':scope > .page')?.firstElementChild];
    const replaced = (before, page) => looksOf(page).some((part, index) => part !== before[index]);

    const returnTo = (top) => {
      const started = performance.now();
      const leaving = looksOf(scroller());
      let room = -1;
      let steadySince = started;
      returning = { page: null, frame: 0 };
      interruptions.forEach((kind) => addEventListener(kind, stopReturning, { capture: true, passive: true }));
      hold(leaving[0]);
      const step = (now) => {
        const page = scroller();
        if (!page || document.querySelector('dialog[open]')) { stopReturning(); return; }
        const late = now - started > giveUpAfter;
        if (late || replaced(leaving, page)) {
          hold(page);
          const reach = page.scrollHeight - page.clientHeight;
          if (reach !== room) { room = reach; steadySince = now; }
          const loading = !!page.querySelector('.skeleton');
          if ((!loading && (room >= top || now - steadySince > settledFor)) || late) {
            page.scrollTop = Math.min(top, room);
            stopReturning();
            return;
          }
        }
        if (now - started > holdUnseen) returning.page?.classList.remove('is-returning');
        returning.frame = requestAnimationFrame(step);
      };
      returning.frame = requestAnimationFrame(step);
    };

    const leave = () => {
      stopReturning();
      const page = scroller();
      if (page) places.set(here, page.scrollTop);
    };

    const pushState = history.pushState;
    history.pushState = function (...args) {
      leave();
      const result = pushState.apply(this, args);
      here = history.state?._index ?? here + 1;
      for (const index of places.keys()) if (index >= here) places.delete(index);
      return result;
    };

    const replaceState = history.replaceState;
    history.replaceState = function (...args) {
      const before = location.href;
      const result = replaceState.apply(this, args);
      here = history.state?._index ?? here;
      if (location.href !== before) places.delete(here);
      return result;
    };

    addEventListener('popstate', () => {
      leave();
      here = history.state?._index ?? 0;
      const top = places.get(here);
      if (top > 0) returnTo(top);
    });
  };
  keepPlaces();
  const entryIndex = () => history.state?._index ?? 0;
  const visited = [];
  const noteVisit = () => { visited[entryIndex()] = location.pathname; };
  for (const method of ['pushState', 'replaceState']) {
    const original = history[method].bind(history);
    history[method] = (...args) => {
      original(...args);
      noteVisit();
    };
  }
  addEventListener('popstate', noteVisit);
  noteVisit();

  const returnTo = (href) => {
    const target = new URL(href, document.baseURI).pathname;
    const here = entryIndex();
    for (let earlier = here - 1; earlier >= 0; earlier--) {
      if (visited[earlier] === target) {
        history.go(earlier - here);
        return;
      }
    }
    window.Blazor.navigateTo(href, { replaceHistoryEntry: true });
  };

  document.addEventListener('scroll', (event) => {
    const page = event.target;
    if (!(page instanceof Element) || !page.matches('.page-scroll, .phone-frame__screen')) return;
    const topbar = page.querySelector('.topbar');
    if (!topbar) return;
    const masthead = page.querySelector('.masthead');
    const bottom = topbar.getBoundingClientRect().bottom;
    const marked = page.querySelector('[data-page-title]');
    const title = (masthead?.querySelector('h1') ?? marked)?.getBoundingClientRect();
    const titleGone = !!title && title.bottom <= bottom;
    topbar.classList.toggle('is-stuck', titleGone || (!marked && page.scrollTop > Number(page.dataset.stuckAt ?? 44)));
    topbar.classList.toggle('is-solid', !!masthead && page.scrollTop > 0 && (masthead.getBoundingClientRect().bottom < bottom + mastheadLead || (!!title && title.top < bottom)));
  }, { capture: true, passive: true });

  const sidewaysRows = '[data-scroll-cue]';
  const markMore = (row) => {
    const end = row.scrollWidth - row.clientWidth;
    const at = Math.abs(row.scrollLeft);
    const more = end <= 1 ? null : at <= 1 ? 'end' : at >= end - 1 ? 'start' : 'both';
    if (more === null) delete row.dataset.more;
    else if (row.dataset.more !== more) row.dataset.more = more;
  };
  const resized = new ResizeObserver((entries) => entries.forEach((entry) => markMore(entry.target)));
  const watchedRows = new WeakSet();
  let rowsPending = false;
  const markAllRows = () => {
    rowsPending = false;
    for (const row of document.querySelectorAll(sidewaysRows)) {
      if (!watchedRows.has(row)) { watchedRows.add(row); resized.observe(row); }
      markMore(row);
    }
  };
  new MutationObserver(() => {
    if (rowsPending) return;
    rowsPending = true;
    requestAnimationFrame(markAllRows);
  }).observe(document.documentElement, { childList: true, subtree: true });
  document.addEventListener('scroll', (event) => {
    if (event.target instanceof Element && event.target.matches(sidewaysRows)) markMore(event.target);
  }, { capture: true, passive: true });

  const movingKeys = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'PageUp', 'PageDown', 'Home', 'End']);
  document.addEventListener('keydown', (event) => {
    if (movingKeys.has(event.key) && event.target instanceof Element && event.target.closest('[data-keep-arrows]')) event.preventDefault();
  });

  const edgeSwipe = () => {
    const edge = 28;
    const slop = 10;
    const settle = 'transform 200ms cubic-bezier(0.22, 1, 0.36, 1)';
    let swipe = null;
    const backOf = (page) => page?.querySelector(':scope > .topbar .icon-btn--back');
    const settleBack = (page) => {
      page.style.transition = settle;
      page.style.transform = '';
      setTimeout(() => { page.style.transition = ''; }, 220);
    };
    document.addEventListener('touchstart', (event) => {
      const touch = event.touches[0];
      const target = event.target instanceof Element ? event.target : null;
      const page = event.touches.length === 1 && touch.clientX < edge && !target?.closest('.sheet-layer, dialog') ? target?.closest('.page-scroll') : null;
      const button = backOf(page);
      swipe = button ? { x: touch.clientX, y: touch.clientY, page, button, dx: 0, at: event.timeStamp, tracking: false } : null;
    }, { capture: true, passive: true });
    document.addEventListener('touchmove', (event) => {
      if (!swipe) return;
      const touch = event.touches[0];
      const dx = touch.clientX - swipe.x;
      const dy = touch.clientY - swipe.y;
      if (!swipe.tracking) {
        if (Math.hypot(dx, dy) < slop) return;
        if (dx <= Math.abs(dy)) { swipe = null; return; }
        swipe.tracking = true;
      }
      event.preventDefault();
      event.stopPropagation();
      swipe.dx = Math.max(0, dx);
      if (!reducedMotion.matches) swipe.page.style.transform = `translateX(${swipe.dx}px)`;
    }, { capture: true, passive: false });
    const finish = (event, cancelled) => {
      if (!swipe?.tracking) { swipe = null; return; }
      const { page, button, dx, at } = swipe;
      swipe = null;
      const fast = dx / Math.max(1, event.timeStamp - at) > 0.5;
      if (cancelled || !(dx > innerWidth * 0.35 || (fast && dx > 40))) { settleBack(page); return; }
      page.style.transition = 'transform 180ms cubic-bezier(0.22, 1, 0.36, 1)';
      page.style.transform = reducedMotion.matches ? '' : 'translateX(100%)';
      setTimeout(() => {
        button.click();
        requestAnimationFrame(() => { if (page.isConnected) settleBack(page); });
      }, reducedMotion.matches ? 0 : 160);
    };
    document.addEventListener('touchend', (event) => finish(event, false), true);
    document.addEventListener('touchcancel', (event) => finish(event, true), true);
  };
  edgeSwipe();

  const typing = 'input, textarea, select, [contenteditable]:not([contenteditable="false"])';
  const ownKeyboardBar = '.composer, .rich-text-field';
  const answersATap = `${typing}, button, a, label, summary, [role="button"], [tabindex]:not([tabindex="-1"]), .sheet-layer`;

  const hideKeyboard = () => {
    if (document.activeElement instanceof HTMLElement && document.activeElement.matches(typing)) document.activeElement.blur();
    plugin('Keyboard')?.hide().catch(() => { });
  };

  const keepFieldInView = () => {
    const field = document.activeElement;
    if (!(field instanceof HTMLElement) || !field.matches(typing)) return;
    field.scrollIntoView({ block: 'nearest' });
    const dock = field.closest('.rich-text-field')?.querySelector('.rich-text-field__bar');
    if (!dock || field.isContentEditable || getComputedStyle(dock).position !== 'fixed') return;
    const covered = field.getBoundingClientRect().bottom - dock.getBoundingClientRect().top + 12;
    if (covered <= 0) return;
    let scroller = field.parentElement;
    while (scroller && scroller !== document.body && !(scroller.scrollHeight > scroller.clientHeight && /auto|scroll/.test(getComputedStyle(scroller).overflowY))) scroller = scroller.parentElement;
    (scroller && scroller !== document.body ? scroller : document.scrollingElement).scrollBy(0, covered);
  };

  const followKeyboard = () => {
    const keyboard = plugin('Keyboard');
    if (!keyboard) return;
    const root = document.documentElement;
    const overlaysPage = cap().getPlatform() === 'ios';
    const afterLayout = (then) => requestAnimationFrame(() => requestAnimationFrame(then));
    const dataLayer = plugin('DataLayer');
    let accessoryWanted = null;
    let accessoryShown = false;
    const showAccessory = (visible, refresh) => {
      if (visible === accessoryWanted && (!refresh || visible === accessoryShown)) return;
      accessoryWanted = visible;
      if (refresh) accessoryShown = visible;
      (dataLayer ? dataLayer.showKeyboardAccessory({ visible, refresh }) : keyboard.setAccessoryBarVisible({ isVisible: visible })).catch(() => { });
    };
    const wantsAccessory = (field) => !field.closest(ownKeyboardBar);
    showAccessory(false, false);
    keyboard.addListener('keyboardWillShow', ({ keyboardHeight }) => {
      root.style.setProperty('--kb', `${overlaysPage ? keyboardHeight : 0}px`);
      root.classList.add('keyboard-open');
      afterLayout(keepFieldInView);
    });
    keyboard.addListener('keyboardDidShow', keepFieldInView);
    keyboard.addListener('keyboardWillHide', () => {
      root.classList.remove('keyboard-open');
      root.style.setProperty('--kb', '0px');
    });
    const accessoryForFocused = () => {
      if (document.activeElement instanceof Element && document.activeElement.matches(typing)) showAccessory(wantsAccessory(document.activeElement), true);
    };
    document.addEventListener('focusin', () => {
      afterLayout(accessoryForFocused);
      if (root.classList.contains('keyboard-open')) afterLayout(keepFieldInView);
    });
    document.addEventListener('pointerdown', (event) => {
      const field = event.target instanceof Element ? event.target.closest(typing) : null;
      if (field) showAccessory(wantsAccessory(field), false);
    }, true);
    const tapSettles = 400;
    document.addEventListener('pointerup', () => setTimeout(accessoryForFocused, tapSettles), true);
    document.addEventListener('pointercancel', () => setTimeout(accessoryForFocused, tapSettles), true);

    let tapAway = null;
    document.addEventListener('pointerdown', (event) => {
      const own = document.activeElement?.closest?.(ownKeyboardBar);
      const target = event.target instanceof Element ? event.target : null;
      tapAway = root.classList.contains('keyboard-open') && own && target && !own.contains(target) && !target.closest(answersATap)
        ? { x: event.clientX, y: event.clientY }
        : null;
    }, true);
    document.addEventListener('pointerup', (event) => {
      if (tapAway && Math.hypot(event.clientX - tapAway.x, event.clientY - tapAway.y) < 10) hideKeyboard();
      tapAway = null;
    }, true);
  };
  followKeyboard();

  const holdAutofocus = (dialog) => {
    if (!dialog.classList.contains('sheet-layer')) return () => { };
    const fields = [...dialog.querySelectorAll('[autofocus]')];
    fields.forEach((field) => field.removeAttribute('autofocus'));
    return () => fields.forEach((field) => field.setAttribute('autofocus', ''));
  };

  return {
    isNative,
    hideKeyboard,

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

    jumpKeys(receiver) {
      const key = (event) => {
        if (!(event.metaKey || event.ctrlKey) || event.altKey || event.shiftKey || event.key.toLowerCase() !== 'k') return;
        if (document.querySelector('dialog[open]:not(.jump-layer)')) return;
        event.preventDefault();
        receiver.invokeMethodAsync('OnHeard', 'jump');
      };
      document.addEventListener('keydown', key);
      return { close: () => document.removeEventListener('keydown', key) };
    },

    usesCommandKey() {
      return [navigator.userAgentData?.platform, navigator.platform].some((platform) => /mac|iphone|ipad/i.test(platform ?? ''));
    },

    recall(key) {
      try { return localStorage.getItem(`gsbc:${key}`); } catch { return null; }
    },

    remember(key, value) {
      try { localStorage.setItem(`gsbc:${key}`, value); } catch { }
    },

    reveal(id) {
      document.getElementById(id)?.scrollIntoView({ block: 'nearest' });
    },

    pointOut(id) {
      const element = document.getElementById(id);
      if (!element) return;
      element.scrollIntoView({ block: 'center', behavior: reducedMotion.matches ? 'auto' : 'smooth' });
      element.classList.remove('is-pointed-out');
      void element.offsetWidth;
      element.classList.add('is-pointed-out');
      element.addEventListener('animationend', () => element.classList.remove('is-pointed-out'), { once: true });
    },

    revealTab(strip) {
      const tab = strip?.querySelector('[aria-current="page"], [aria-selected="true"]');
      if (!tab || strip.scrollWidth <= strip.clientWidth) return;
      const start = tab.offsetLeft - strip.offsetLeft;
      const end = start + tab.offsetWidth;
      const margin = parseFloat(getComputedStyle(strip).getPropertyValue('--scroll-fade')) || 24;
      if (start - margin < strip.scrollLeft) strip.scrollTo({ left: Math.max(0, start - margin), behavior: 'smooth' });
      else if (end + margin > strip.scrollLeft + strip.clientWidth) strip.scrollTo({ left: end + margin - strip.clientWidth, behavior: 'smooth' });
    },

    restoreFocus() {
      const before = focusBeforeModal.pop();
      if (before?.isConnected) before.focus({ preventScroll: true });
    },

    focusFirst(sheet) {
      sheet.focus({ preventScroll: true });
      const field = sheet.querySelector('[autofocus]');
      if (!field) return;
      const longestOpening = 400;
      let done = false;
      const focusField = () => {
        if (done) return;
        done = true;
        sheet.removeEventListener('animationend', arrived);
        if (field.isConnected && document.activeElement === sheet) field.focus({ preventScroll: true });
      };
      const arrived = (event) => { if (event.target === sheet) focusField(); };
      sheet.addEventListener('animationend', arrived);
      setTimeout(focusField, longestOpening);
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

    returnTo,

    scrollToTop() {
      scroller()?.scrollTo({ top: 0, behavior: reducedMotion.matches ? 'auto' : 'smooth' });
    },

    showModal(dialog) {
      if (!dialog || dialog.open || typeof dialog.showModal !== 'function') return;
      focusBeforeModal.push(document.activeElement);
      dialog.addEventListener('cancel', (event) => event.preventDefault());
      const restoreAutofocus = holdAutofocus(dialog);
      dialog.showModal();
      restoreAutofocus();
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

    sheetDrag(sheet, receiver) {
      const ease = 'cubic-bezier(0.22, 1, 0.36, 1)';
      const backdrop = () => sheet.parentElement?.querySelector('.sheet-backdrop');
      const docked = () => getComputedStyle(sheet).bottom === '0px';
      const fixed = 'input, textarea, select, [contenteditable="true"], .chips, .no-sheet-drag, .map-view';
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

    swipePages(host, receiver) {
      const track = host.querySelector('.swipe-pages__track');
      if (!track) return { dispose() { } };
      const ease = 'cubic-bezier(0.22, 1, 0.36, 1)';
      const lockAfter = 8;
      const tapGuard = 400;
      let start = null;
      let settling = false;
      let draggedAt = -Infinity;

      const place = (dx) => { track.style.transform = dx ? `translateX(${dx}px)` : ''; };
      const opens = (direction) => !!track.querySelector(direction > 0 ? '.swipe-pages__page--after' : '.swipe-pages__page--before');
      const glide = (dx, duration) => {
        track.style.transition = duration ? `transform ${duration}ms ${ease}` : 'none';
        place(dx);
        return new Promise((done) => setTimeout(done, duration));
      };

      const onStart = (event) => {
        start = !settling && event.touches.length === 1
          ? { x: event.touches[0].clientX, y: event.touches[0].clientY, at: event.timeStamp, dx: 0, sideways: null }
          : null;
      };
      const onMove = (event) => {
        if (!start) return;
        if (event.touches.length > 1) { start = null; place(0); return; }
        const dx = event.touches[0].clientX - start.x;
        const dy = event.touches[0].clientY - start.y;
        if (start.sideways === null) {
          if (Math.hypot(dx, dy) < lockAfter) return;
          start.sideways = Math.abs(dx) > Math.abs(dy);
          if (!start.sideways) { start = null; return; }
          track.style.transition = 'none';
        }
        event.preventDefault();
        start.dx = opens(dx < 0 ? 1 : -1) ? dx : dx / 4;
        place(start.dx);
      };
      const onEnd = async (event) => {
        if (!start?.sideways) { start = null; return; }
        const { dx, at } = start;
        start = null;
        draggedAt = event.timeStamp;
        const width = host.offsetWidth;
        const direction = dx < 0 ? 1 : -1;
        const fast = Math.abs(dx) / Math.max(1, event.timeStamp - at) > 0.5 && Math.abs(dx) > 30;
        const duration = reducedMotion.matches ? 0 : 260;
        if (!opens(direction) || (Math.abs(dx) < width * 0.25 && !fast)) {
          await glide(0, duration);
          track.style.transition = '';
          return;
        }
        settling = true;
        await glide(-direction * width, duration);
        try { await receiver.invokeMethodAsync('Step', direction); } catch { }
        await glide(0, 0);
        track.style.transition = '';
        settling = false;
      };
      const onCancel = () => { if (start?.sideways) { glide(0, 0); } start = null; };
      const onClick = (event) => {
        if (event.timeStamp - draggedAt < tapGuard) { event.preventDefault(); event.stopPropagation(); }
      };

      host.addEventListener('touchstart', onStart, { passive: true });
      host.addEventListener('touchmove', onMove, { passive: false });
      host.addEventListener('touchend', onEnd);
      host.addEventListener('touchcancel', onCancel);
      host.addEventListener('click', onClick, { capture: true });
      return {
        dispose() {
          host.removeEventListener('touchstart', onStart);
          host.removeEventListener('touchmove', onMove);
          host.removeEventListener('touchend', onEnd);
          host.removeEventListener('touchcancel', onCancel);
          host.removeEventListener('click', onClick, { capture: true });
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
