window.gsbcEvents = (() => {
  let watching = null;

  function scroller() {
    return document.querySelector('.page-scroll');
  }

  function follow(receiver) {
    unfollow();
    const page = scroller();
    if (!page) return;
    let last = null;
    const report = () => {
      const band = page.querySelector('.week-band');
      const line = band ? band.getBoundingClientRect().bottom + 8 : 0;
      let here = null;
      for (const day of page.querySelectorAll('[data-day]')) {
        if (day.getBoundingClientRect().top <= line) here = day.dataset.day;
        else break;
      }
      here ??= page.querySelector('[data-day]')?.dataset.day ?? null;
      if (here && here !== last) {
        last = here;
        receiver.invokeMethodAsync('Here', here);
      }
    };
    let queued = false;
    const onScroll = () => {
      if (queued) return;
      queued = true;
      requestAnimationFrame(() => { queued = false; report(); });
    };
    page.addEventListener('scroll', onScroll, { passive: true });
    watching = { page, onScroll };
  }

  function unfollow() {
    if (watching) watching.page.removeEventListener('scroll', watching.onScroll);
    watching = null;
  }

  function scrollToDay(day) {
    const page = scroller();
    const target = page?.querySelector(`[data-day="${day}"]`);
    if (!page || !target) return false;
    const band = page.querySelector('.week-band');
    const offset = band ? band.getBoundingClientRect().bottom - page.getBoundingClientRect().top : 0;
    const top = target.getBoundingClientRect().top - page.getBoundingClientRect().top + page.scrollTop - offset - 4;
    const still = matchMedia('(prefers-reduced-motion: reduce)').matches;
    page.scrollTo({ top, behavior: still ? 'auto' : 'smooth' });
    return true;
  }

  function saveCalendarFile(name, text) {
    const link = document.createElement('a');
    link.href = URL.createObjectURL(new Blob([text], { type: 'text/calendar' }));
    link.download = name;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(link.href), 60000);
  }

  async function share(title, text) {
    if (navigator.share) {
      try {
        await navigator.share({ title, text });
        return 'shared';
      } catch (error) {
        return error?.name === 'AbortError' ? 'cancelled' : 'failed';
      }
    }
    try {
      await navigator.clipboard.writeText(text);
      return 'copied';
    } catch {
      return 'failed';
    }
  }

  async function copyHtml(html, text) {
    try {
      if (window.ClipboardItem && navigator.clipboard?.write) {
        await navigator.clipboard.write([new ClipboardItem({
          'text/html': new Blob([html], { type: 'text/html' }),
          'text/plain': new Blob([text], { type: 'text/plain' }),
        })]);
        return true;
      }
      await navigator.clipboard.writeText(html);
      return true;
    } catch {
      return false;
    }
  }

  function fitFrameHeight(frame, scale) {
    const fit = () => {
      const body = frame.contentDocument?.body;
      if (body) frame.parentElement.style.height = `${Math.ceil(body.scrollHeight * scale)}px`;
    };
    frame.addEventListener('load', fit);
    fit();
  }

  function fitEmail(frame, width) {
    const fit = () => {
      const holder = frame.parentElement;
      const body = frame.contentDocument?.body;
      if (!holder || !body) return;
      const tall = body.scrollHeight;
      const scale = Math.min(1, holder.clientWidth / width);
      frame.style.height = `${tall}px`;
      frame.style.transform = `scale(${scale.toFixed(3)})`;
      frame.style.transformOrigin = 'top center';
      frame.style.marginBottom = `${-tall * (1 - scale)}px`;
      frame.style.marginInline = `${(-width * (1 - scale)) / 2}px`;
    };
    if (!frame.gsbcEmailFit) {
      frame.gsbcEmailFit = new ResizeObserver(fit);
      frame.gsbcEmailFit.observe(frame.parentElement);
      frame.addEventListener('load', fit);
    }
    fit();
  }

  return { follow, unfollow, scrollToDay, saveCalendarFile, share, copyHtml, fitFrameHeight, fitEmail };
})();
