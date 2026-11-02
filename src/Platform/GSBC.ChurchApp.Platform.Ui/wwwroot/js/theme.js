window.gsbcTheme = (() => {
  const root = document.documentElement;
  const darkQuery = matchMedia('(prefers-color-scheme: dark)');
  const read = (key) => {
    try { return localStorage.getItem(`gsbc:${key}`); } catch { return null; }
  };
  const write = (key, value) => {
    try {
      if (value === null) localStorage.removeItem(`gsbc:${key}`);
      else localStorage.setItem(`gsbc:${key}`, value);
    } catch { }
  };

  const customStyle = () => {
    let style = document.getElementById('gsbc-custom-theme');
    if (!style) {
      style = document.createElement('style');
      style.id = 'gsbc-custom-theme';
      document.head.append(style);
    }
    return style;
  };

  const apply = () => {
    customStyle().textContent = read('custom-theme') ?? '';
    const appearance = read('appearance') ?? 'automatic';
    const dark = appearance === 'dark' || (appearance === 'automatic' && darkQuery.matches);
    root.dataset.scheme = dark ? 'dark' : 'light';
    root.dataset.theme = read('palette-preview') ?? read('palette') ?? 'brand';
    const masthead = getComputedStyle(root).getPropertyValue('--masthead').trim();
    if (masthead) document.querySelector('meta[name="theme-color"]')?.setAttribute('content', masthead);
  };

  apply();
  darkQuery.addEventListener('change', apply);

  return {
    appearance: () => read('appearance') ?? 'automatic',
    preview: () => read('palette-preview'),
    setAppearance(appearance) { write('appearance', appearance); apply(); },
    setPalette(palette, customCss) {
      write('palette', palette);
      write('custom-theme', customCss ?? null);
      apply();
    },
    setPreview(palette) { write('palette-preview', palette); apply(); },
  };
})();
