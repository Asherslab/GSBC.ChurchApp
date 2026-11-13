(options) => {
  const gutter = options.gutter;
  const tolerance = 0.5;
  const findings = [];
  const viewWidth = document.documentElement.clientWidth;
  const styles = new Map();
  const style = element => {
    let found = styles.get(element);
    if (!found) styles.set(element, found = getComputedStyle(element));
    return found;
  };

  const alphaOf = colour => {
    if (!colour || colour === 'transparent') return 0;
    const slash = colour.match(/\/\s*([\d.]+)(%?)\s*\)$/);
    if (slash) return parseFloat(slash[1]) * (slash[2] ? 0.01 : 1);
    const rgba = colour.match(/^rgba\(([^)]+)\)$/);
    if (rgba) return parseFloat(rgba[1].split(',')[3]);
    return 1;
  };

  const backgrounds = new Map();
  const backgroundOf = element => {
    if (!element || element.nodeType !== 1) return 'page';
    if (backgrounds.has(element)) return backgrounds.get(element);
    const own = style(element).backgroundColor;
    const found = alphaOf(own) >= 0.99 ? own : backgroundOf(element.parentElement);
    backgrounds.set(element, found);
    return found;
  };

  const optedOut = (element, attribute) => !!element.closest?.(`[${attribute}]`);

  const describe = element => {
    const parts = [];
    for (let at = element; at && at !== document.body && parts.length < 3; at = at.parentElement) {
      const classes = [...at.classList].filter(name => !name.startsWith('b-')).slice(0, 2);
      parts.unshift(at.tagName.toLowerCase() + classes.map(name => '.' + name).join(''));
    }
    return parts.join(' › ');
  };

  const textOf = element => (element.innerText ?? element.textContent ?? '').trim().replace(/\s+/g, ' ').slice(0, 60);
  const box = rect => ({ x: Math.round(rect.left + scrollX), y: Math.round(rect.top + scrollY), w: Math.round(rect.width), h: Math.round(rect.height) });

  const shown = element => element.checkVisibility({ opacityProperty: true, visibilityProperty: true, contentVisibilityAuto: true });
  const skipped = element => options.skip && element.closest(options.skip);
  const all = [...document.body.querySelectorAll('*')].filter(element => !(element instanceof SVGElement && !(element instanceof SVGSVGElement)) && shown(element) && !skipped(element));
  const narrow = viewWidth <= 760;
  const isPane = rect => rect.height >= innerHeight * 0.4;

  const sides = ['left', 'right', 'top', 'bottom'];
  const capital = side => side[0].toUpperCase() + side.slice(1);
  const visibleSides = element => {
    const css = style(element);
    const set = new Set();
    const filled = (alphaOf(css.backgroundColor) > 0.05 && css.backgroundColor !== backgroundOf(element.parentElement)) || css.backgroundImage !== 'none';
    const shadowed = css.boxShadow !== 'none' && css.boxShadow.split(/,(?![^(]*\))/).some(shadow => !shadow.includes('inset'));
    if (filled || shadowed) sides.forEach(side => set.add(side));
    for (const side of sides) {
      const css2 = style(element);
      if (parseFloat(css2[`border${capital(side)}Width`]) >= 1 && css2[`border${capital(side)}Style`] !== 'none' && alphaOf(css2[`border${capital(side)}Color`]) > 0.05) set.add(side);
    }
    return set;
  };

  const inner = (element, rect) => {
    const css = style(element);
    return {
      left: rect.left + parseFloat(css.borderLeftWidth),
      right: rect.right - parseFloat(css.borderRightWidth),
      top: rect.top + parseFloat(css.borderTopWidth),
      bottom: rect.bottom - parseFloat(css.borderBottomWidth),
    };
  };

  const isBig = rect => rect.width >= 100 && rect.height > 48;
  const isField = element => element.matches('input, textarea, select, [contenteditable], [role=textbox], .field__control, .field__input, .search-field');
  const providers = new Map();
  const atomic = new Set();
  for (const element of all) {
    if (element.tagName === 'svg' || element.tagName === 'BODY' || element.tagName === 'HTML') continue;
    const rect = element.getBoundingClientRect();
    if (rect.width < 1 || rect.height < 1) continue;
    const seen = visibleSides(element);
    if (seen.size === 0) continue;
    const control = element.matches('button, a[href], [role=button], [role=tab], label') && rect.width < 200;
    if (element.parentElement?.closest('button, [role=button]')) { if (seen.size === 4 && !control) atomic.add(element); continue; }
    if (control) { if (seen.size === 4) atomic.add(element); continue; }
    const pill = parseFloat(style(element).borderTopLeftRadius) >= Math.min(rect.width, rect.height) / 2 - 1;
    const band = rect.width >= 300 && rect.width >= (element.parentElement?.getBoundingClientRect().width ?? 0) * 0.9;
    if (isBig(rect) && !isField(element) && !pill) providers.set(element, { element, rect, inner: inner(element, rect), sides: seen });
    else if (seen.size === 4 && !band) atomic.add(element);
  }

  const insideAtomic = element => {
    for (let at = element.parentElement; at; at = at.parentElement) if (atomic.has(at)) return true;
    return false;
  };

  const hiddenAway = element => {
    for (let at = element; at && at !== document.body; at = at.parentElement) {
      const css = style(at);
      if (css.clip !== 'auto' || css.clipPath !== 'none') return true;
      if (css.overflowX !== 'visible' && (at.clientWidth <= 1 || at.clientHeight <= 1)) return true;
    }
    return false;
  };

  const clipsOf = (element, self = false) => {
    const found = [];
    for (let at = self ? element : element.parentElement; at && at !== document.documentElement; at = at.parentElement) {
      const css = style(at);
      if (css.overflowX !== 'visible' || css.overflowY !== 'visible') found.push(at);
    }
    return found;
  };

  const inkOf = svg => {
    const painted = shape => {
      const css = style(shape);
      return css.visibility !== 'hidden' && parseFloat(css.opacity) > 0 && ((css.fill !== 'none' && alphaOf(css.fill) > 0 && parseFloat(css.fillOpacity) > 0) || (css.stroke !== 'none' && alphaOf(css.stroke) > 0 && parseFloat(css.strokeOpacity) > 0));
    };
    const shapes = [...svg.querySelectorAll('path, circle, rect, line, polyline, polygon, ellipse, text, use')].filter(painted).map(shape => shape.getBoundingClientRect()).filter(rect => rect.width > 0 || rect.height > 0);
    if (!shapes.length) return svg.getBoundingClientRect();
    const left = Math.min(...shapes.map(rect => rect.left)), top = Math.min(...shapes.map(rect => rect.top));
    const right = Math.max(...shapes.map(rect => rect.right)), bottom = Math.max(...shapes.map(rect => rect.bottom));
    return new DOMRect(left, top, right - left, bottom - top);
  };
  const pinned = element => {
    for (let at = element; at && at !== document.body; at = at.parentElement) if (['sticky', 'fixed'].includes(style(at).position)) return at;
    return null;
  };

  const leaves = [];
  for (const element of atomic) if (!insideAtomic(element)) leaves.push({ element, rects: [element.getBoundingClientRect()], kind: 'control' });
  for (const element of all) {
    if (atomic.has(element) || insideAtomic(element)) continue;
    const tag = element.tagName;
    if (tag === 'svg' || ['IMG', 'VIDEO', 'CANVAS', 'INPUT', 'SELECT', 'TEXTAREA', 'IFRAME'].includes(tag)) {
      if (tag === 'svg' && element.parentElement?.closest('svg')) continue;
      leaves.push({ element, rects: [tag === 'svg' ? inkOf(element) : element.getBoundingClientRect()], kind: tag === 'svg' ? 'icon' : 'media' });
    }
  }
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    const parent = node.parentElement;
    if (!parent || skipped(parent) || !node.textContent.trim() || ['SCRIPT', 'STYLE', 'NOSCRIPT', 'TEMPLATE'].includes(parent.tagName)) continue;
    if (!shown(parent) || atomic.has(parent) || insideAtomic(parent) || parent.closest('svg')) continue;
    const range = document.createRange();
    range.selectNodeContents(node);
    const rects = [...range.getClientRects()].filter(rect => rect.width > 1 && rect.height > 1);
    if (rects.length) leaves.push({ element: parent, rects, kind: 'text', text: node.textContent.trim().replace(/\s+/g, ' ').slice(0, 60) });
  }

  const add = finding => findings.push(finding);
  const providerList = [...providers.values()];
  const overlapsVertically = (a, b) => Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top);
  const overlapsHorizontally = (a, b) => Math.min(a.right, b.right) - Math.max(a.left, b.left);

  const clippedBy = (leaf, rect) => {
    for (const clip of clipsOf(leaf.element, leaf.kind === 'text')) {
      const edges = inner(clip, clip.getBoundingClientRect());
      const css = style(clip);
      const outside = rect.left < edges.left - tolerance || rect.right > edges.right + tolerance || rect.top < edges.top - tolerance || rect.bottom > edges.bottom + tolerance;
      if (!outside) continue;
      const fully = rect.right <= edges.left || rect.left >= edges.right || rect.bottom <= edges.top || rect.top >= edges.bottom;
      return { clip, scrolls: /auto|scroll/.test(css.overflowX + css.overflowY), fully };
    }
    return null;
  };

  const intended = (leaf, clip) => {
    for (let at = leaf.element; at; at = at.parentElement) {
      const css = style(at);
      if (css.textOverflow === 'ellipsis' || (css.webkitLineClamp && css.webkitLineClamp !== 'none')) return true;
      if (at === clip) break;
    }
    return false;
  };

  for (let index = leaves.length - 1; index >= 0; index--) if (hiddenAway(leaves[index].element)) leaves.splice(index, 1);
  for (const leaf of leaves) {
    leaf.ancestors = new Set();
    for (let at = leaf.element.parentElement; at; at = at.parentElement) if (providers.has(at)) leaf.ancestors.add(at);
    if (leaf.kind !== 'text' && providers.has(leaf.element)) leaf.ancestors.delete(leaf.element);
  }

  const reportedClips = new Set();
  for (const leaf of leaves) {
    leaf.live = [];
    for (const rect of leaf.rects) {
      const clipped = clippedBy(leaf, rect);
      if (!clipped) { leaf.live.push(rect); continue; }
      if (clipped.scrolls || clipped.fully || leaf.kind !== 'text') continue;
      if (intended(leaf, clipped.clip) || optedOut(leaf.element, 'data-look-clip') || reportedClips.has(leaf.element)) continue;
      reportedClips.add(leaf.element);
      add({ check: 'clipped', what: describe(leaf.element), text: leaf.text, against: describe(clipped.clip), rect: box(rect) });
    }
  }

  const edgeOf = (provider, side) => provider.inner[side];
  const opposites = { left: 'right', right: 'left', top: 'bottom', bottom: 'top' };
  const scrollsSideways = element => {
    for (let at = element.parentElement; at; at = at.parentElement) if (/auto|scroll/.test(style(at).overflowX) && at.scrollWidth > at.clientWidth + 1) return true;
    return false;
  };
  for (const leaf of leaves) {
    if (optedOut(leaf.element, 'data-look-bleed')) continue;
    const sideways = scrollsSideways(leaf.element);
    for (const rect of leaf.live) {
      if (rect.width < 2 || rect.height < 2) continue;
      for (const side of sides) {
        if (sideways && (side === 'left' || side === 'right')) continue;
        let nearest = null;
        const consider = (distance, against, how) => {
          if (nearest === null || distance < nearest.distance) nearest = { distance, against, how };
        };
        if (side === 'left') consider(rect.left, 'the window', 'edge');
        if (side === 'right') consider(viewWidth - rect.right, 'the window', 'edge');
        for (const provider of providerList) {
          if (!leaf.ancestors.has(provider.element) && (side === 'top' || side === 'bottom' || !isPane(provider.rect))) continue;
          if (leaf.ancestors.has(provider.element)) {
            if (!provider.sides.has(side)) continue;
            const edge = edgeOf(provider, side);
            const distance = side === 'left' ? rect.left - edge : side === 'right' ? edge - rect.right : side === 'top' ? rect.top - edge : edge - rect.bottom;
            consider(distance, describe(provider.element), 'box');
            continue;
          }
          const layers = [pinned(leaf.element), pinned(provider.element)];
          if (layers[0] !== layers[1] && layers.some(layer => layer && overlapsHorizontally(layer.getBoundingClientRect(), provider.rect) > 1 && overlapsHorizontally(layer.getBoundingClientRect(), rect) > 1)) continue;
          if (!provider.sides.has(opposites[side]) || leaf.element === provider.element || provider.element.contains(leaf.element) || leaf.element.contains(provider.element)) continue;
          const r = provider.rect;
          const distance = side === 'left' ? rect.left - r.right : side === 'right' ? r.left - rect.right : side === 'top' ? rect.top - r.bottom : r.top - rect.bottom;
          if (distance < -1 || distance >= gutter) continue;
          const alongside = side === 'left' || side === 'right' ? overlapsVertically(r, rect) >= Math.min(rect.height, 10) : overlapsHorizontally(r, rect) >= Math.min(rect.width, 10);
          if (alongside) consider(Math.max(distance, 0), describe(provider.element), 'neighbour');
        }
        if (!nearest || nearest.distance >= gutter - tolerance) continue;
        if (leaf.kind === 'media' && nearest.how === 'box') continue;
        add({
          check: nearest.distance < -tolerance ? 'spills' : 'edge',
          side,
          distance: Math.round(nearest.distance * 10) / 10,
          what: describe(leaf.element),
          text: leaf.text ?? textOf(leaf.element),
          against: nearest.against,
          rect: box(rect),
        });
        break;
      }
    }
  }

  for (const element of all) {
    if (visibleSides(element).size < 4 || optedOut(element, 'data-look-bleed') || scrollsSideways(element)) continue;
    const rect = element.getBoundingClientRect();
    if (clipsOf(element).some(clip => { const edges = clip.getBoundingClientRect(); return rect.left < edges.left - tolerance || rect.right > edges.right + tolerance; })) continue;
    const gaps = { left: rect.left, right: viewWidth - rect.right };
    const near = Object.entries(gaps).find(([, gap]) => gap > tolerance && gap < gutter - tolerance);
    if (near) add({ check: 'edge', side: near[0], distance: Math.round(near[1] * 10) / 10, what: describe(element), text: 'a filled box stops just short of the window edge', against: 'the window', rect: box(rect) });
  }

  const textLines = leaves.filter(leaf => leaf.kind === 'text').flatMap(leaf => leaf.live.map(rect => ({ leaf, rect })));
  textLines.sort((a, b) => a.rect.top - b.rect.top);
  const reportedPairs = new Set();
  for (let i = 0; i < textLines.length; i++) {
    for (let j = i + 1; j < textLines.length && textLines[j].rect.top < textLines[i].rect.bottom; j++) {
      const a = textLines[i], b = textLines[j];
      if (a.leaf === b.leaf || pinned(a.leaf.element) !== pinned(b.leaf.element)) continue;
      if (overlapsHorizontally(a.rect, b.rect) <= 2 || overlapsVertically(a.rect, b.rect) <= 2) continue;
      if (optedOut(a.leaf.element, 'data-look-overlap') || optedOut(b.leaf.element, 'data-look-overlap')) continue;
      const key = describe(a.leaf.element) + '|' + describe(b.leaf.element);
      if (reportedPairs.has(key)) continue;
      reportedPairs.add(key);
      add({ check: 'overlap', what: describe(a.leaf.element), text: `"${a.leaf.text}" over "${b.leaf.text}"`, against: describe(b.leaf.element), rect: box(a.rect) });
    }
  }

  for (const parent of all) {
    const children = [...parent.children].filter(child => {
      if (!shown(child)) return false;
      const css = style(child);
      if (['absolute', 'fixed', 'sticky'].includes(css.position) || css.display.startsWith('inline') && css.display !== 'inline-block' && css.display !== 'inline-flex' && css.display !== 'inline-grid') return false;
      if ([css.marginLeft, css.marginRight, css.marginTop, css.marginBottom].some(margin => parseFloat(margin) < 0)) return false;
      const rect = child.getBoundingClientRect();
      return rect.width > 2 && rect.height > 2;
    });
    if (children.length < 2) continue;
    const boxes = children.map(child => ({ child, rect: child.getBoundingClientRect() })).sort((a, b) => a.rect.top - b.rect.top);
    for (let i = 0; i < boxes.length; i++) {
      for (let j = i + 1; j < boxes.length && boxes[j].rect.top < boxes[i].rect.bottom; j++) {
        const a = boxes[i], b = boxes[j];
        if (overlapsHorizontally(a.rect, b.rect) <= 2 || overlapsVertically(a.rect, b.rect) <= 2) continue;
        if (optedOut(a.child, 'data-look-overlap') || optedOut(b.child, 'data-look-overlap')) continue;
        const placed = child => style(child).gridColumnStart !== 'auto' || style(child).gridRowStart !== 'auto';
        if (style(parent).display.includes('grid') && (placed(a.child) || placed(b.child))) continue;
        add({ check: 'overlap', what: describe(a.child), text: textOf(a.child), against: describe(b.child), rect: box(a.rect) });
      }
    }
  }

  const root = document.scrollingElement;
  if (root.scrollWidth > root.clientWidth + 1) {
    const culprits = all.filter(element => {
      const rect = element.getBoundingClientRect();
      if (rect.right <= viewWidth + 1) return false;
      if (clipsOf(element).some(clip => style(clip).overflowX !== 'visible')) return false;
      const parent = element.parentElement;
      return !parent || parent.getBoundingClientRect().right <= viewWidth + 1;
    });
    add({ check: 'overflow', what: 'the page', text: `${root.scrollWidth - root.clientWidth}px wider than the window`, against: culprits.slice(0, 3).map(describe).join('; '), rect: culprits[0] ? box(culprits[0].getBoundingClientRect()) : box({ left: 0, top: 0, width: viewWidth, height: 10 }) });
  }
  for (const element of all) {
    const css = style(element);
    if (!/auto|scroll/.test(css.overflowX) || element.clientWidth < 200 || element.scrollWidth <= element.clientWidth + 1) continue;
    const allowed = element.closest('[data-look-scroll]')?.dataset.lookScroll;
    if (allowed === '' || (allowed === 'narrow' && narrow) || element.matches('.tab-strip, [role=tablist]')) continue;
    add({ check: 'overflow', what: describe(element), text: `scrolls sideways by ${element.scrollWidth - element.clientWidth}px`, against: '', rect: box(element.getBoundingClientRect()) });
  }

  const scrollParent = element => {
    for (let at = element.parentElement; at; at = at.parentElement) {
      const css = style(at);
      if (/auto|scroll/.test(css.overflowY) || /auto|scroll/.test(css.overflowX)) return at;
    }
    return document.documentElement;
  };
  for (const element of all) {
    const css = style(element);
    const foot = [...element.classList].some(name => /(__foot|-foot)$/.test(name));
    const stuckToBottom = (css.position === 'sticky' || css.position === 'fixed') && css.bottom !== 'auto';
    if (!foot && !stuckToBottom) continue;
    if (visibleSides(element).size < 4 || optedOut(element, 'data-look-bleed')) continue;
    const rect = element.getBoundingClientRect();
    let container;
    if (css.position === 'fixed') container = { element: null, edges: { left: 0, right: viewWidth, top: 0, bottom: innerHeight } };
    else if (stuckToBottom) {
      const parent = scrollParent(element);
      const edges = parent === document.documentElement
        ? { left: 0, right: viewWidth, top: 0, bottom: innerHeight }
        : (() => { const r = parent.getBoundingClientRect(); const e = inner(parent, r); return { ...e, right: e.left + parent.clientWidth, bottom: e.top + parent.clientHeight }; })();
      container = { element: parent, edges };
    } else {
      let card = element.parentElement;
      while (card && !providers.has(card)) card = card.parentElement;
      if (!card) continue;
      container = { element: card, edges: providers.get(card).inner };
    }
    if (css.position === 'fixed' && rect.width < viewWidth * 0.6) continue;
    const gaps = { left: rect.left - container.edges.left, right: container.edges.right - rect.right };
    if (!stuckToBottom) gaps.bottom = container.edges.bottom - rect.bottom;
    const off = Object.entries(gaps).filter(([, gap]) => Math.abs(gap) > 1);
    if (!off.length) continue;
    add({ check: 'bar', what: describe(element), text: off.map(([side, gap]) => `${Math.round(gap)}px ${gap > 0 ? 'short of' : 'past'} the ${side} edge`).join(', '), against: container.element ? describe(container.element) : 'the window', rect: box(rect) });
  }

  const broken = [...document.querySelectorAll('.empty')].map(textOf).find(text => /stopped working|isn't part of your role|Sign in to see this/.test(text));
  if (broken) add({ check: 'broken', what: 'the page', text: broken, against: '', rect: box({ left: 0, top: 0, width: viewWidth, height: 40 }) });

  return { findings, scheme: document.documentElement.dataset.scheme ?? '', height: root.scrollHeight };
}
