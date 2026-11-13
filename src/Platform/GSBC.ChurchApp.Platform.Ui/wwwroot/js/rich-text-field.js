import {
  EditorState, EditorSelection, StateField, StateEffect, RangeSetBuilder, Prec, EditorView, Decoration, ViewPlugin, WidgetType, keymap, placeholder,
  history, defaultKeymap, historyKeymap, Language, defineLanguageFacet, LanguageSupport, syntaxTree, markdownParser,
  Table, Strikethrough, Subscript, Superscript, Emoji,
} from './codemirror.js';

const blank = text => /\s|^$/.test(text);

const HighlightDelimiter = { resolve: 'Highlight', mark: 'HighlightMark' };
const Highlight = {
  defineNodes: ['Highlight', 'HighlightMark'],
  parseInline: [{
    name: 'Highlight',
    parse(cx, next, pos) {
      if (next !== 61 || cx.char(pos + 1) !== 61 || cx.char(pos + 2) === 61 || cx.char(pos - 1) === 61) return -1;
      return cx.addDelimiter(HighlightDelimiter, pos, pos + 2, !blank(cx.slice(pos + 2, pos + 3)), !blank(cx.slice(pos - 1, pos)));
    },
    after: 'Emphasis',
  }],
};

const FootnoteReference = {
  defineNodes: ['FootnoteRef'],
  parseInline: [{
    name: 'FootnoteRef',
    parse(cx, next, pos) {
      if (next !== 91 || cx.char(pos + 1) !== 94) return -1;
      const found = /^\[\^[A-Za-z0-9_-]{1,40}\]/.exec(cx.slice(pos, Math.min(cx.end, pos + 44)));
      if (!found || cx.char(pos + found[0].length) === 58) return -1;
      return cx.addElement(cx.elt('FootnoteRef', pos, pos + found[0].length));
    },
    before: 'Link',
  }],
};

const wholeLine = (name, pattern) => ({
  defineNodes: [{ name, block: true }],
  parseBlock: [{
    name,
    parse(cx, line) {
      if (!pattern.test(line.text)) return false;
      cx.addElement(cx.elt(name, cx.lineStart, cx.lineStart + line.text.length));
      cx.nextLine();
      return true;
    },
    endLeaf: name === 'Directive' ? (cx, line) => pattern.test(line.text) : undefined,
    before: 'HorizontalRule',
  }],
});

const CardSplit = wholeLine('CardSplit', /^={3,}\s*$/);
const Directive = wholeLine('Directive', /^::[a-z][a-z0-9-]{0,30}\{[^{}]{0,500}\}\s*$/);

const parser = markdownParser.configure([
  Table, Strikethrough, Subscript, Superscript, Emoji, Highlight, FootnoteReference, CardSplit, Directive,
  { remove: ['SetextHeading', 'IndentedCode', 'HTMLBlock', 'LinkReference', 'HTMLTag', 'Entity', 'Autolink'] },
]);
const markdown = new LanguageSupport(new Language(defineLanguageFacet(), parser, [], 'markdown'));

const inlineKinds = { bold: 'StrongEmphasis', italic: 'Emphasis', strike: 'Strikethrough', highlight: 'Highlight', code: 'InlineCode', sub: 'Subscript', sup: 'Superscript', link: 'Link' };
const lineKinds = { ATXHeading1: 'h1', ATXHeading2: 'h2', ATXHeading3: 'h3', BulletList: 'bullets', OrderedList: 'numbers', Blockquote: 'quote', FencedCode: 'codeblock' };
const inlineStyles = {
  StrongEmphasis: 'cm-md-strong', Emphasis: 'cm-md-em', Link: 'cm-md-link', Strikethrough: 'cm-md-strike', Highlight: 'cm-md-highlight',
  InlineCode: 'cm-md-code', Subscript: 'cm-md-sub', Superscript: 'cm-md-sup',
};
const markNodes = new Set(['EmphasisMark', 'LinkMark', 'URL', 'LinkTitle', 'StrikethroughMark', 'HighlightMark', 'CodeMark', 'SubscriptMark', 'SuperscriptMark']);

class Marker extends WidgetType {
  constructor(text, ordered) { super(); this.text = text; this.ordered = ordered; }
  eq(other) { return other.text === this.text && other.ordered === this.ordered; }
  toDOM() {
    const marker = document.createElement('span');
    marker.className = this.ordered ? 'cm-md-number' : 'cm-md-bullet';
    marker.textContent = this.text;
    return marker;
  }
  ignoreEvent() { return false; }
}

class Rule extends WidgetType {
  constructor(split) { super(); this.split = split; }
  eq(other) { return other.split === this.split; }
  toDOM() {
    const rule = document.createElement('span');
    rule.className = this.split ? 'cm-md-split' : 'cm-md-rule';
    if (this.split) rule.textContent = 'Next card';
    rule.setAttribute('aria-label', this.split ? 'Split into the next card' : 'Line across');
    return rule;
  }
  ignoreEvent() { return false; }
}

class Glyph extends WidgetType {
  constructor(glyph, className = '') { super(); this.glyph = glyph; this.className = className; }
  eq(other) { return other.glyph === this.glyph && other.className === this.className; }
  toDOM() {
    const glyph = document.createElement('span');
    glyph.className = this.className;
    glyph.textContent = this.glyph;
    return glyph;
  }
  ignoreEvent() { return false; }
}

const hidden = Decoration.replace({});
const collapsed = Decoration.mark({ class: 'cm-md-hidden' });
const quietMark = Decoration.mark({ class: 'cm-md-mark' });
const faint = Decoration.mark({ class: 'cm-md-mark' });
const blankLine = Decoration.line({ class: 'cm-md-blank' });
const blankInList = Decoration.line({ class: 'cm-md-blank-in-list' });
const listLine = /^\s*(?:[-*+]|\d{1,3}[.)])(\s|$)/;
const footnoteDefinition = /^\[\^[A-Za-z0-9_-]{1,40}\]:/;
const styles = Object.fromEntries(Object.entries(inlineStyles).map(([name, css]) => [name, Decoration.mark({ class: css })]));
const lines = {
  ATXHeading1: Decoration.line({ class: 'cm-md-h1' }),
  ATXHeading2: Decoration.line({ class: 'cm-md-h2' }),
  ATXHeading3: Decoration.line({ class: 'cm-md-h3' }),
  ATXHeading4: Decoration.line({ class: 'cm-md-h4' }),
  ATXHeading5: Decoration.line({ class: 'cm-md-h4' }),
  ATXHeading6: Decoration.line({ class: 'cm-md-h4' }),
  Blockquote: Decoration.line({ class: 'cm-md-quote' }),
  ListItem: Decoration.line({ class: 'cm-md-li' }),
  CodeLine: Decoration.line({ class: 'cm-md-codeblock' }),
  Fence: Decoration.line({ class: 'cm-md-codeblock cm-md-fence' }),
  Table: Decoration.line({ class: 'cm-md-table' }),
  Rule: Decoration.line({ class: 'cm-md-rule-line' }),
  Directive: Decoration.line({ class: 'cm-md-directive' }),
  Footnote: Decoration.line({ class: 'cm-md-fndef' }),
};

function touches(selection, from, to) {
  return selection !== null && selection.ranges.some(range => range.from <= to && range.to >= from);
}

function trailingSpace(state, to) {
  return to < state.doc.length && state.doc.sliceString(to, to + 1) === ' ' ? to + 1 : to;
}

function decorate(view, emoji) {
  const { state } = view;
  const selection = view.hasFocus ? state.selection : null;
  const media = state.field(mediaField, false);
  const marks = [];
  const lineMarks = new Map();
  const markLine = (position, decoration) => {
    const line = state.doc.lineAt(position);
    if (!lineMarks.has(line.from)) lineMarks.set(line.from, decoration);
  };
  const markLines = (from, to, decoration) => {
    for (let line = state.doc.lineAt(from); ; line = state.doc.line(line.number + 1)) {
      markLine(line.from, decoration);
      if (line.to >= to || line.number >= state.doc.lines) break;
    }
  };
  const inMedia = position => media && media.ranges.some(range => range.from <= position && range.to >= position);
  for (const { from, to } of view.visibleRanges) {
    syntaxTree(state).iterate({
      from, to,
      enter: node => {
        const name = node.name;
        if (inMedia(node.from)) return false;
        if (name === 'Link' && node.from === state.doc.lineAt(node.from).from && footnoteDefinition.test(state.doc.lineAt(node.from).text)) return false;
        if (styles[name]) {
          marks.push(styles[name].range(node.from, node.to));
        } else if (markNodes.has(name)) {
          const parent = node.node.parent;
          if (parent && styles[parent.name] && node.from < node.to)
            marks.push((touches(selection, parent.from, parent.to) ? faint : collapsed).range(node.from, node.to));
        } else if (lines[name] && name.startsWith('ATXHeading')) {
          markLine(node.from, lines[name]);
          const mark = node.node.getChild('HeaderMark');
          if (mark) marks.push((touches(selection, node.from, node.to) ? faint : hidden).range(mark.from, trailingSpace(state, mark.to)));
        } else if (name === 'QuoteMark') {
          markLine(node.from, lines.Blockquote);
          const line = state.doc.lineAt(node.from);
          marks.push((touches(selection, line.from, line.to) ? faint : hidden).range(node.from, trailingSpace(state, node.to)));
        } else if (name === 'ListMark') {
          markLine(node.from, lines.ListItem);
          const ordered = node.node.parent?.parent?.name === 'OrderedList';
          const active = touches(selection, node.from, node.to);
          const text = state.doc.sliceString(node.from, node.to);
          marks.push((active ? faint : Decoration.replace({ widget: new Marker(ordered ? text : '•', ordered) })).range(node.from, active ? node.to : trailingSpace(state, node.to)));
        } else if (name === 'FencedCode') {
          markLines(node.from, node.to, lines.CodeLine);
          const first = state.doc.lineAt(node.from);
          const last = state.doc.lineAt(node.to);
          lineMarks.set(first.from, lines.Fence);
          if (last.number > first.number && /^\s*(`{3,}|~{3,})\s*$/.test(last.text)) lineMarks.set(last.from, lines.Fence);
          if (!touches(selection, node.from, node.to)) {
            for (let child = node.node.firstChild; child; child = child.nextSibling) {
              if ((child.name === 'CodeMark' || child.name === 'CodeInfo') && child.from < child.to) marks.push(collapsed.range(child.from, child.to));
            }
          }
          return false;
        } else if (name === 'Table') {
          markLines(node.from, node.to, lines.Table);
        } else if (name === 'TableDelimiter') {
          marks.push(quietMark.range(node.from, node.to));
        } else if (name === 'HorizontalRule' || name === 'CardSplit') {
          markLine(node.from, lines.Rule);
          if (!touches(selection, node.from, node.to)) marks.push(Decoration.replace({ widget: new Rule(name === 'CardSplit') }).range(node.from, node.to));
        } else if (name === 'Directive') {
          markLine(node.from, lines.Directive);
        } else if (name === 'Emoji') {
          const glyph = emoji[state.doc.sliceString(node.from + 1, node.to - 1)];
          if (glyph && !touches(selection, node.from, node.to)) marks.push(Decoration.replace({ widget: new Glyph(glyph) }).range(node.from, node.to));
        } else if (name === 'FootnoteRef') {
          if (!touches(selection, node.from, node.to))
            marks.push(Decoration.replace({ widget: new Glyph(state.doc.sliceString(node.from + 2, node.to - 1), 'cm-md-fnref') }).range(node.from, node.to));
        } else if (name === 'Escape') {
          if (!touches(selection, node.from, node.to)) marks.push(collapsed.range(node.from, node.from + 1));
        }
      },
    });
  }
  for (const { from, to } of view.visibleRanges) {
    for (let position = from; position <= to;) {
      const line = state.doc.lineAt(position);
      if (footnoteDefinition.test(line.text) && !lineMarks.has(line.from)) lineMarks.set(line.from, lines.Footnote);
      if (line.length === 0 && line.number > 1 && line.number < state.doc.lines && !(selection && touches(selection, line.from, line.to)) && !lineMarks.has(line.from) && !inMedia(line.from))
        lineMarks.set(line.from, listLine.test(state.doc.line(line.number - 1).text) && listLine.test(state.doc.line(line.number + 1).text) ? blankInList : blankLine);
      position = line.to + 1;
    }
  }
  const builder = new RangeSetBuilder();
  const all = [...marks, ...[...lineMarks].map(([from, decoration]) => decoration.range(from))];
  all.sort((a, b) => a.from - b.from || a.value.startSide - b.value.startSide || a.to - b.to);
  let replacedTo = -1;
  for (const range of all) {
    const replaces = range.value.spec.widget !== undefined || range.value === hidden;
    if (replaces && range.from < replacedTo) continue;
    builder.add(range.from, range.to, range.value);
    if (replaces) replacedTo = Math.max(replacedTo, range.to);
  }
  return builder.finish();
}

const mediaPattern = /^!\[([^\]]*)\]\((media:[^\s)"]+|https:\/\/[^\s)"]+)(?:\s+"([^"]*)")?\)\s*$/;
const setMediaInfo = StateEffect.define();

function parseMediaLine(text) {
  const found = mediaPattern.exec(text);
  if (!found) return null;
  const [, alt, target, caption] = found;
  return target.startsWith('media:') ? { alt, id: target.slice(6), caption: caption ?? '' } : { alt, link: target, caption: caption ?? alt };
}

function mediaLine(item) {
  const quoted = (item.caption ?? '').replace(/"/g, "'").replace(/[\r\n]+/g, ' ').trim();
  const alt = (item.alt ?? '').replace(/[[\]]/g, '');
  if (item.link) return `![${quoted.replace(/[[\]]/g, '')}](${item.link})`;
  return quoted.length ? `![${alt}](media:${item.id} "${quoted}")` : `![${alt}](media:${item.id})`;
}

function kindOf(item, info) {
  if (item.link) return 'link';
  return info.media[item.id]?.kind ?? 'picture';
}

function mediaRanges(state, info) {
  const ranges = [];
  for (let number = 1; number <= state.doc.lines; number++) {
    const line = state.doc.line(number);
    const item = parseMediaLine(line.text);
    if (!item) continue;
    const kind = kindOf(item, info);
    const previous = ranges[ranges.length - 1];
    if (previous && previous.lastLine === number - 1 && kind === 'picture' && previous.kind === 'picture') {
      previous.items.push({ ...item, line: number });
      previous.to = line.to;
      previous.lastLine = number;
      continue;
    }
    ranges.push({ from: line.from, to: line.to, lastLine: number, kind, items: [{ ...item, line: number }] });
  }
  return ranges;
}

class MediaWidget extends WidgetType {
  constructor(range, info, selected, actions) {
    super();
    this.range = range;
    this.info = info;
    this.selected = selected;
    this.actions = actions;
    this.key = `${range.kind}:${range.items.map(item => item.id ?? item.link).join(',')}:${info.version}`;
  }

  eq(other) { return other.key === this.key && other.selected === this.selected && other.caption === this.caption; }

  get caption() { return this.range.items[0].caption; }

  updateDOM(dom) {
    if (dom.dataset.key !== this.key) return false;
    dom.classList.toggle('cm-media--selected', this.selected);
    const input = dom.querySelector('.cm-media__caption');
    if (input && document.activeElement !== input && input.value !== this.caption) input.value = this.caption;
    return true;
  }

  toDOM(view) {
    const { range, info } = this;
    const dom = document.createElement('div');
    dom.className = `cm-media cm-media--${range.items.length > 1 ? 'gallery' : range.kind}${this.selected ? ' cm-media--selected' : ''}`;
    dom.dataset.key = this.key;
    dom.setAttribute('role', 'group');
    const first = range.items[0];
    const name = item => info.media[item.id]?.name || item.alt || 'File';
    dom.setAttribute('aria-label', range.kind === 'link' ? 'Video link' : range.items.length > 1 ? `Gallery of ${range.items.length} pictures` : `${range.kind[0].toUpperCase()}${range.kind.slice(1)}: ${name(first)}`);

    const frame = document.createElement('div');
    frame.className = 'cm-media__frame';
    frame.dataset.count = String(Math.min(range.items.length, 4));
    for (const item of range.items) frame.append(this.face(item, name(item)));
    dom.append(frame);

    const tools = document.createElement('div');
    tools.className = 'cm-media__tools';
    const button = (label, icon, action) => {
      const tool = document.createElement('button');
      tool.type = 'button';
      tool.className = 'cm-media__tool';
      tool.setAttribute('aria-label', label);
      tool.title = label;
      tool.innerHTML = icon;
      tool.addEventListener('mousedown', event => event.preventDefault());
      tool.addEventListener('click', event => {
        event.preventDefault();
        action(view.posAtDOM(dom));
      });
      return tool;
    };
    const grip = button('Drag to move it', icons.grip, () => {});
    grip.draggable = true;
    grip.classList.add('cm-media__grip');
    grip.addEventListener('dragstart', event => {
      const from = view.posAtDOM(dom);
      const found = view.state.field(mediaField).ranges.find(each => each.from === from);
      if (!found) return;
      event.dataTransfer.setData('application/x-gsbc-media', JSON.stringify({ from: found.from, to: found.to }));
      event.dataTransfer.setData('text/plain', view.state.doc.sliceString(found.from, found.to));
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setDragImage(dom, 24, 24);
    });
    tools.append(grip);
    if (range.kind === 'picture') tools.append(button('Add a picture beside it', icons.plus, at => this.actions.add(at)));
    if (range.kind !== 'link' && range.items.length === 1) tools.append(button('Replace it', icons.replace, at => this.actions.replace(at)));
    tools.append(button(range.items.length > 1 ? 'Take the gallery out' : 'Take it out', icons.remove, at => this.actions.remove(at)));
    dom.append(tools);

    if (range.kind !== 'file') {
      const caption = document.createElement('input');
      caption.className = 'cm-media__caption';
      caption.type = 'text';
      caption.placeholder = 'Add a caption';
      caption.setAttribute('aria-label', 'Caption');
      caption.maxLength = 200;
      caption.value = this.caption;
      caption.addEventListener('input', () => this.actions.caption(view.posAtDOM(dom), caption.value));
      caption.addEventListener('keydown', event => {
        if (event.key !== 'Enter' && event.key !== 'Escape') return;
        event.preventDefault();
        this.actions.after(view.posAtDOM(dom));
      });
      dom.append(caption);
    }

    const where = range.items.map(item => info.media[item.id]?.path).filter(Boolean);
    if (where.length === 1 || range.kind === 'link') {
      const path = document.createElement('span');
      path.className = 'cm-media__where';
      path.textContent = range.kind === 'link' ? first.link : where[0];
      dom.append(path);
    }

    dom.addEventListener('mousedown', event => {
      if (event.target.closest('input, button')) return;
      event.preventDefault();
      const from = view.posAtDOM(dom);
      const found = view.state.field(mediaField).ranges.find(each => each.from === from);
      if (found) view.dispatch({ selection: EditorSelection.range(found.from, found.to) });
      view.focus();
    });
    return dom;
  }

  face(item, name) {
    const known = this.info.media[item.id];
    const kind = item.link ? 'link' : known?.kind ?? 'picture';
    const face = document.createElement('div');
    face.className = `cm-media__face cm-media__face--${kind}`;
    if (kind === 'picture' && known?.url) {
      const image = document.createElement('img');
      image.src = known.url;
      image.alt = item.alt || name;
      image.draggable = false;
      face.append(image);
    } else if (kind === 'video' || kind === 'link' || kind === 'audio') {
      face.innerHTML = `<span class="cm-media__play">${icons.play}</span>`;
      const label = document.createElement('span');
      label.className = 'cm-media__label';
      label.textContent = kind === 'link' ? linkSite(item.link) : name;
      face.append(label);
    } else {
      face.innerHTML = `<span class="cm-media__file-icon">${kind === 'picture' ? icons.image : icons.file}</span>`;
      const text = document.createElement('span');
      text.className = 'cm-media__file-text';
      const title = document.createElement('strong');
      title.textContent = name;
      const size = document.createElement('small');
      size.textContent = known?.size ?? (kind === 'picture' ? 'Waiting for the picture' : '');
      text.append(title, size);
      face.append(text);
    }
    return face;
  }

  ignoreEvent() { return true; }

  get estimatedHeight() { return this.range.kind === 'file' ? 72 : 260; }
}

function linkSite(link) {
  return /vimeo\.com/i.test(link) ? 'A Vimeo video' : 'A YouTube video';
}

const svg = paths => `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths}</svg>`;
const icons = {
  grip: svg('<circle cx="9" cy="6" r="1"/><circle cx="9" cy="12" r="1"/><circle cx="9" cy="18" r="1"/><circle cx="15" cy="6" r="1"/><circle cx="15" cy="12" r="1"/><circle cx="15" cy="18" r="1"/>'),
  plus: svg('<path d="M5 12h14"/><path d="M12 5v14"/>'),
  replace: svg('<path d="M3 12a9 9 0 0 1 15-6.7L21 8"/><path d="M21 3v5h-5"/><path d="M21 12a9 9 0 0 1-15 6.7L3 16"/><path d="M8 16H3v5"/>'),
  remove: svg('<path d="M3 6h18"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>'),
  play: svg('<path d="M6 3l14 9-14 9V3z" fill="currentColor"/>'),
  file: svg('<path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7z"/><path d="M14 2v4a2 2 0 0 0 2 2h4"/>'),
  image: svg('<rect width="18" height="18" x="3" y="3" rx="2"/><circle cx="9" cy="9" r="2"/><path d="m21 15-3.1-3.1a2 2 0 0 0-2.8 0L6 21"/>'),
};

const mediaField = StateField.define({
  create: state => ({ info: { media: {}, version: 0 }, ranges: mediaRanges(state, { media: {} }) }),
  update(value, transaction) {
    let info = value.info;
    for (const effect of transaction.effects) if (effect.is(setMediaInfo)) info = effect.value;
    if (!transaction.docChanged && info === value.info) return value;
    return { info, ranges: mediaRanges(transaction.state, info) };
  },
});

function mediaDecorations(actions) {
  return EditorView.decorations.compute([mediaField, 'selection'], state => {
    const { ranges, info } = state.field(mediaField);
    const selection = state.selection.main;
    const builder = new RangeSetBuilder();
    for (const range of ranges) {
      const selected = selection.from <= range.from && selection.to >= range.to && !selection.empty;
      builder.add(range.from, range.to, Decoration.replace({ widget: new MediaWidget(range, info, selected, actions), block: true }));
    }
    return builder.finish();
  });
}

const atomicMedia = EditorView.atomicRanges.of(view => {
  const builder = new RangeSetBuilder();
  for (const range of view.state.field(mediaField).ranges) builder.add(range.from, range.to, hidden);
  return builder.finish();
});

function rangeAt(state, at) {
  return state.field(mediaField).ranges.find(range => range.from <= at && range.to >= at) ?? null;
}

function removal(state, range) {
  let from = range.from;
  let to = range.to;
  if (to < state.doc.length && state.doc.sliceString(to, to + 1) === '\n') to++;
  if (to < state.doc.length && state.doc.lineAt(to).length === 0 && from > 0) to = Math.min(state.doc.length, to + 1);
  else if (from > 0 && to >= state.doc.length) from = Math.max(0, from - 1);
  return { from, to };
}

function blockInsertAt(state, position, text) {
  const line = state.doc.lineAt(position);
  if (line.length === 0) {
    const before = line.number > 1 && state.doc.line(line.number - 1).length > 0 ? '\n' : '';
    const after = line.number < state.doc.lines && state.doc.line(line.number + 1).length > 0 ? '\n' : '';
    return { from: line.from, insert: `${before}${text}${after}` };
  }
  return { from: line.from, insert: `${text}\n\n` };
}

function enclosing(state, kind) {
  const position = state.selection.main.head;
  for (const side of [-1, 1]) {
    for (let node = syntaxTree(state).resolveInner(position, side); node; node = node.parent) {
      if (node.name === kind) return node;
    }
  }
  return null;
}

function activeFormats(state) {
  const active = Object.entries(inlineKinds).filter(([, kind]) => enclosing(state, kind)).map(([format]) => format);
  for (const [kind, format] of Object.entries(lineKinds)) {
    if (enclosing(state, kind)) active.push(format);
  }
  return active.sort().join(',');
}

function unwrap(view, node) {
  const changes = [];
  for (let child = node.firstChild; child; child = child.nextSibling) {
    if (markNodes.has(child.name) && child.name !== 'URL' && child.name !== 'LinkTitle') changes.push({ from: child.from, to: child.to, insert: '' });
  }
  if (changes.length === 0) return false;
  view.dispatch({ changes, userEvent: 'format' });
  return true;
}

function continueList(view) {
  const { state } = view;
  const range = state.selection.main;
  if (!range.empty) return false;
  const line = state.doc.lineAt(range.head);
  const match = /^(\s*)(?:([-*+])|(\d{1,3})([.)]))(\s+|$)/.exec(line.text) ?? /^(\s*)(>)(\s?)/.exec(line.text);
  if (!match) return false;
  const marker = match[0];
  if (line.text.trim() === marker.trim()) {
    view.dispatch({ changes: { from: line.from, to: line.to, insert: '\n' }, selection: { anchor: line.from + 1 }, userEvent: 'input' });
    return true;
  }
  const next = match[3] && match[4] ? `${match[1]}${Number(match[3]) + 1}${match[4]}${match[5] || ' '}` : marker;
  view.dispatch(state.replaceSelection('\n' + next), { scrollIntoView: true, userEvent: 'input' });
  return true;
}

function pastedTree(html) {
  const document = new DOMParser().parseFromString(html, 'text/html');
  const walk = element => {
    const nodes = [];
    for (const child of element.childNodes) {
      if (child.nodeType === Node.TEXT_NODE) {
        if (child.textContent) nodes.push({ k: 't', t: child.textContent });
        continue;
      }
      if (child.nodeType !== Node.ELEMENT_NODE) continue;
      const tag = child.tagName.toLowerCase();
      const style = (child.getAttribute('style') || '').toLowerCase();
      if (['script', 'style', 'head', 'title', 'meta', 'template'].includes(tag) || /mso-list:\s*ignore/.test(style)) {
        if (/mso-list:\s*ignore/.test(style)) nodes.push({ k: 'marker', t: child.textContent.trim() });
        continue;
      }
      const inner = walk(child);
      const weight = /font-weight:\s*(\d+|bold)/.exec(style)?.[1];
      const heavy = weight === 'bold' || Number(weight) >= 600;
      const light = weight === 'normal' || (weight && Number(weight) < 600);
      const kind = /^h[1-6]$/.test(tag) ? 'h'
        : tag === 'p' && /mso-list:/.test(style) ? 'li'
        : { p: 'p', div: 'p', li: 'li', ul: 'ul', ol: 'ol', blockquote: 'q', br: 'br', a: 'a', strong: 'b', em: 'i', i: 'i', u: 'span', tr: 'p' }[tag]
          ?? (tag === 'b' ? (light ? 'span' : 'b') : heavy ? 'b' : /font-style:\s*italic/.test(style) ? 'i' : 'span');
      const node = { k: kind, c: inner };
      if (kind === 'a') node.h = child.getAttribute('href') || '';
      if (kind === 'span' && heavy) node.k = 'b';
      if (kind === 'span' && /font-style:\s*italic/.test(style)) node.c = [{ k: 'i', c: inner }];
      nodes.push(node);
    }
    return nodes;
  };
  return { k: 'p', c: walk(document.body) };
}

export function attach(host, field, value, label, placeholderText, emojiJson) {
  const emoji = JSON.parse(emojiJson);
  let reportedFormats = '';
  let sending = false;
  const report = view => {
    const formats = activeFormats(view.state);
    if (formats !== reportedFormats) {
      reportedFormats = formats;
      field.invokeMethodAsync('FormatsAsync', formats);
    }
  };
  const send = view => {
    if (sending) return;
    sending = true;
    queueMicrotask(() => {
      sending = false;
      field.invokeMethodAsync('ChangedAsync', view.state.doc.toString());
    });
  };
  const run = format => () => {
    field.invokeMethodAsync('FormatAsync', format);
    return true;
  };
  let view;
  const actions = {
    remove: at => {
      const range = rangeAt(view.state, at);
      if (range) view.dispatch({ changes: removal(view.state, range), userEvent: 'delete' });
      view.focus();
    },
    replace: at => field.invokeMethodAsync('MediaWantedAsync', 'replace', at),
    add: at => field.invokeMethodAsync('MediaWantedAsync', 'add', at),
    caption: (at, caption) => {
      const range = rangeAt(view.state, at);
      if (!range) return;
      const line = view.state.doc.line(range.items[0].line);
      const item = parseMediaLine(line.text);
      if (!item) return;
      view.dispatch({ changes: { from: line.from, to: line.to, insert: mediaLine({ ...item, caption }) }, userEvent: 'input.caption' });
    },
    after: at => {
      const range = rangeAt(view.state, at);
      if (!range) return;
      let position = Math.min(view.state.doc.length, range.to + 1);
      if (range.to >= view.state.doc.length) {
        view.dispatch({ changes: { from: range.to, insert: '\n\n' } });
        position = range.to + 2;
      }
      view.dispatch({ selection: { anchor: position }, scrollIntoView: true });
      view.focus();
    },
  };
  view = new EditorView({
    parent: host,
    state: EditorState.create({
      doc: value,
      extensions: [
        markdown,
        mediaField,
        mediaDecorations(actions),
        atomicMedia,
        ViewPlugin.fromClass(class {
          constructor(view) { this.decorations = decorate(view, emoji); }
          update(update) {
            if (update.docChanged || update.selectionSet || update.focusChanged || update.viewportChanged || syntaxTree(update.startState) !== syntaxTree(update.state)
              || update.startState.field(mediaField) !== update.state.field(mediaField))
              this.decorations = decorate(update.view, emoji);
          }
        }, { decorations: plugin => plugin.decorations }),
        history(),
        EditorView.lineWrapping,
        EditorView.scrollMargins.of(() => ({ bottom: document.documentElement.classList.contains('keyboard-open') ? 72 : 16 })),
        EditorView.contentAttributes.of({ 'aria-label': label, 'aria-multiline': 'true', autocapitalize: 'sentences', spellcheck: 'true' }),
        placeholder(placeholderText),
        Prec.high(keymap.of([
          { key: 'Mod-b', run: run('bold') },
          { key: 'Mod-i', run: run('italic') },
          { key: 'Mod-k', run: run('link') },
          { key: 'Mod-Shift-x', run: run('strike') },
          { key: 'Mod-Shift-h', run: run('highlight') },
          { key: 'Mod-e', run: run('code') },
          { key: 'Mod-Alt-0', run: run('text') },
          { key: 'Mod-Alt-1', run: run('h1') },
          { key: 'Mod-Alt-2', run: run('h2') },
          { key: 'Mod-Alt-3', run: run('h3') },
          { key: 'Mod-Shift-7', run: run('numbers') },
          { key: 'Mod-Shift-8', run: run('bullets') },
          { key: 'Mod-Shift-9', run: run('quote') },
          { key: 'Enter', run: continueList },
        ])),
        keymap.of([...defaultKeymap, ...historyKeymap]),
        EditorView.domEventHandlers({
          paste: (event, view) => {
            const html = event.clipboardData?.getData('text/html');
            const plain = event.clipboardData?.getData('text/plain') ?? '';
            if (!html && plain.split('\n').some(line => mediaPattern.test(line.trim()))) {
              event.preventDefault();
              const at = view.state.selection.main;
              view.dispatch({ changes: [{ from: at.from, to: at.to }, blockInsertAt(view.state, at.from, plain.trim())], userEvent: 'input.paste', scrollIntoView: true });
              return true;
            }
            if (!html) return false;
            event.preventDefault();
            field.invokeMethodAsync('PastedAsync', JSON.stringify(pastedTree(html)))
              .then(markdown => {
                if (markdown) view.dispatch(view.state.replaceSelection(markdown), { scrollIntoView: true, userEvent: 'input.paste' });
              });
            return true;
          },
          dragover: event => {
            if (!event.dataTransfer?.types.includes('application/x-gsbc-media')) return false;
            event.preventDefault();
            event.dataTransfer.dropEffect = 'move';
            return true;
          },
          drop: (event, view) => {
            const moving = event.dataTransfer?.getData('application/x-gsbc-media');
            if (!moving) return false;
            event.preventDefault();
            const { from, to } = JSON.parse(moving);
            const position = view.posAtCoords({ x: event.clientX, y: event.clientY });
            if (position == null || (position >= from && position <= to)) return true;
            const text = view.state.doc.sliceString(from, to);
            const target = view.state.doc.lineAt(position);
            const box = view.coordsAtPos(target.from);
            const below = box && event.clientY > (box.top + box.bottom) / 2 && target.length > 0;
            const insertAt = below ? Math.min(view.state.doc.length, target.to) : target.from;
            const insert = below ? `\n\n${text}` : `${text}\n\n`;
            view.dispatch({ changes: [removal(view.state, { from, to }), { from: insertAt, insert }], userEvent: 'move.drop', scrollIntoView: true });
            view.focus();
            return true;
          },
        }),
        EditorView.updateListener.of(update => {
          if (update.docChanged) send(update.view);
          if (update.docChanged || update.selectionSet) report(update.view);
        }),
      ],
    }),
  });
  const tools = host.closest('.rich-text-field')?.querySelector('.rich-text-field__bar');
  const keepFocus = event => {
    if (event.target.closest('button') && !event.target.closest('.rich-text-panel, .menu__panel')) event.preventDefault();
  };
  tools?.addEventListener('pointerdown', keepFocus);
  tools?.addEventListener('mousedown', keepFocus);
  return {
    text: () => view.state.doc.toString(),
    selection: () => `${view.state.selection.main.from},${view.state.selection.main.to}`,
    setValue: text => {
      if (text === view.state.doc.toString()) return;
      view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: text } });
    },
    setMedia: json => {
      const previous = view.state.field(mediaField).info;
      view.dispatch({ effects: setMediaInfo.of({ media: JSON.parse(json), version: previous.version + 1 }) });
    },
    placeMedia: (mode, at, lines) => {
      const range = rangeAt(view.state, at);
      if (!range) return;
      if (mode === 'replace') {
        const line = view.state.doc.line(range.items[0].line);
        const old = parseMediaLine(line.text);
        const fresh = parseMediaLine(lines);
        view.dispatch({ changes: { from: line.from, to: line.to, insert: mediaLine({ ...fresh, caption: old?.caption ?? '' }) }, userEvent: 'input' });
      } else {
        view.dispatch({ changes: { from: range.to, insert: `\n${lines}` }, userEvent: 'input' });
      }
      view.focus();
    },
    toggleOff: format => {
      const node = enclosing(view.state, inlineKinds[format]);
      return node ? unwrap(view, node) : false;
    },
    apply: (text, start, end) => {
      const current = view.state.doc.toString();
      let from = 0;
      while (from < current.length && from < text.length && current[from] === text[from]) from++;
      let currentEnd = current.length;
      let textEnd = text.length;
      while (currentEnd > from && textEnd > from && current[currentEnd - 1] === text[textEnd - 1]) { currentEnd--; textEnd--; }
      view.dispatch({
        changes: { from, to: currentEnd, insert: text.slice(from, textEnd) },
        selection: EditorSelection.range(start, end),
        scrollIntoView: true,
        userEvent: 'format',
      });
      view.focus();
    },
    focus: () => view.focus(),
    dispose: () => {
      tools?.removeEventListener('pointerdown', keepFocus);
      tools?.removeEventListener('mousedown', keepFocus);
      view.destroy();
    },
  };
}
