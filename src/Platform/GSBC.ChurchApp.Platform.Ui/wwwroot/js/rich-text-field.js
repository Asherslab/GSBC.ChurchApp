import {
  EditorState, EditorSelection, RangeSetBuilder, Compartment, Prec, EditorView, Decoration, ViewPlugin, WidgetType, keymap, placeholder,
  history, defaultKeymap, historyKeymap, Language, defineLanguageFacet, LanguageSupport, syntaxTree, markdownParser,
} from './codemirror.js';

const unsupported = ['SetextHeading', 'IndentedCode', 'FencedCode', 'HTMLBlock', 'LinkReference', 'HorizontalRule', 'HTMLTag', 'Entity', 'InlineCode', 'Image', 'Autolink'];
const parser = markdownParser.configure({ remove: unsupported });
const markdown = new LanguageSupport(new Language(defineLanguageFacet(), parser, [], 'markdown'));

const inlineKinds = { bold: 'StrongEmphasis', italic: 'Emphasis', link: 'Link' };
const lineKinds = { ATXHeading2: 'heading', ATXHeading3: 'subheading', BulletList: 'bullets', OrderedList: 'numbers', Blockquote: 'quote' };

class Bullet extends WidgetType {
  eq() { return true; }
  toDOM() {
    const dot = document.createElement('span');
    dot.className = 'cm-md-bullet';
    dot.textContent = '•';
    return dot;
  }
  ignoreEvent() { return false; }
}

const hidden = Decoration.replace({});
const faint = Decoration.mark({ class: 'cm-md-mark' });
const bullet = Decoration.replace({ widget: new Bullet() });
const blank = Decoration.line({ class: 'cm-md-blank' });
const blankInList = Decoration.line({ class: 'cm-md-blank-in-list' });
const listLine = /^\s*(?:[-*]|\d{1,3}[.)])\s/;
const styles = {
  StrongEmphasis: Decoration.mark({ class: 'cm-md-strong' }),
  Emphasis: Decoration.mark({ class: 'cm-md-em' }),
  Link: Decoration.mark({ class: 'cm-md-link' }),
};
const lines = {
  ATXHeading2: Decoration.line({ class: 'cm-md-h2' }),
  ATXHeading3: Decoration.line({ class: 'cm-md-h3' }),
  Blockquote: Decoration.line({ class: 'cm-md-quote' }),
  ListItem: Decoration.line({ class: 'cm-md-li' }),
};

function touches(selection, from, to) {
  return selection !== null && selection.ranges.some(range => range.from <= to && range.to >= from);
}

function trailingSpace(state, to) {
  return to < state.doc.length && state.doc.sliceString(to, to + 1) === ' ' ? to + 1 : to;
}

function decorate(view) {
  const { state } = view;
  const selection = view.hasFocus ? state.selection : null;
  const marks = [];
  const lineMarks = new Map();
  const markLine = (position, decoration) => {
    const line = state.doc.lineAt(position);
    if (!lineMarks.has(line.from)) lineMarks.set(line.from, decoration);
  };
  for (const { from, to } of view.visibleRanges) {
    syntaxTree(state).iterate({
      from, to,
      enter: node => {
        const name = node.name;
        if (styles[name]) {
          marks.push(styles[name].range(node.from, node.to));
          const active = touches(selection, node.from, node.to);
          for (let child = node.node.firstChild; child; child = child.nextSibling) {
            if (child.name === 'EmphasisMark' || child.name === 'LinkMark' || child.name === 'URL') {
              if (child.from < child.to) marks.push((active ? faint : hidden).range(child.from, child.to));
            }
          }
        } else if (lines[name] && name !== 'ListItem' && name !== 'Blockquote') {
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
          if (!ordered) {
            const active = touches(selection, node.from, node.to);
            marks.push((active ? faint : bullet).range(node.from, active ? node.to : trailingSpace(state, node.to)));
          }
        } else if (name === 'Escape') {
          if (!touches(selection, node.from, node.to)) marks.push(hidden.range(node.from, node.from + 1));
        }
      },
    });
  }
  for (const { from, to } of view.visibleRanges) {
    for (let position = from; position <= to;) {
      const line = state.doc.lineAt(position);
      if (line.length === 0 && line.number > 1 && line.number < state.doc.lines && !(selection && touches(selection, line.from, line.to)) && !lineMarks.has(line.from))
        lineMarks.set(line.from, listLine.test(state.doc.line(line.number - 1).text) && listLine.test(state.doc.line(line.number + 1).text) ? blankInList : blank);
      position = line.to + 1;
    }
  }
  const builder = new RangeSetBuilder();
  const all = [...marks, ...[...lineMarks].map(([from, decoration]) => decoration.range(from))];
  all.sort((a, b) => a.from - b.from || a.value.startSide - b.value.startSide || a.to - b.to);
  for (const range of all) builder.add(range.from, range.to, range.value);
  return builder.finish();
}

const livePreview = ViewPlugin.fromClass(class {
  constructor(view) { this.decorations = decorate(view); }
  update(update) {
    if (update.docChanged || update.selectionSet || update.focusChanged || update.viewportChanged || syntaxTree(update.startState) !== syntaxTree(update.state))
      this.decorations = decorate(update.view);
  }
}, { decorations: plugin => plugin.decorations });

function enclosing(state, kind) {
  const position = state.selection.main.head;
  for (let node = syntaxTree(state).resolveInner(position, -1); node; node = node.parent) {
    if (node.name === kind) return node;
  }
  for (let node = syntaxTree(state).resolveInner(position, 1); node; node = node.parent) {
    if (node.name === kind) return node;
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
    if (child.name === 'EmphasisMark') changes.push({ from: child.from, to: child.to, insert: '' });
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
  const match = /^(\s*)(?:([-*])|(\d{1,3})([.)]))(\s+)/.exec(line.text) ?? /^(\s*)(>)(\s?)/.exec(line.text);
  if (!match) return false;
  const marker = match[0];
  if (line.text.trim() === marker.trim()) {
    view.dispatch({ changes: { from: line.from, to: line.to, insert: '\n' }, selection: { anchor: line.from + 1 }, userEvent: 'input' });
    return true;
  }
  const next = match[3] && match[4] ? `${match[1]}${Number(match[3]) + 1}${match[4]}${match[5]}` : marker;
  view.dispatch(state.replaceSelection('\n' + next), { scrollIntoView: true, userEvent: 'input' });
  return true;
}

function headingFromHash(view, from, to, text) {
  if (text !== ' ') return false;
  const line = view.state.doc.lineAt(from);
  if (line.text !== '#' || from !== line.to || !view.dom.closest('[data-headings="true"]')) return false;
  view.dispatch({ changes: { from: line.from, to: line.to, insert: '## ' }, selection: { anchor: line.from + 3 }, userEvent: 'input' });
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

export function attach(host, field, value, label, placeholderText) {
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
  const run = format => view => {
    field.invokeMethodAsync('FormatAsync', format);
    return true;
  };
  const view = new EditorView({
    parent: host,
    state: EditorState.create({
      doc: value,
      extensions: [
        markdown,
        livePreview,
        history(),
        EditorView.lineWrapping,
        EditorView.scrollMargins.of(() => ({ bottom: document.documentElement.classList.contains('keyboard-open') ? 72 : 16 })),
        EditorView.contentAttributes.of({ 'aria-label': label, 'aria-multiline': 'true', autocapitalize: 'sentences', spellcheck: 'true' }),
        placeholder(placeholderText),
        EditorView.inputHandler.of(headingFromHash),
        Prec.high(keymap.of([
          { key: 'Mod-b', run: run('bold') },
          { key: 'Mod-i', run: run('italic') },
          { key: 'Mod-k', run: run('link') },
          { key: 'Enter', run: continueList },
        ])),
        keymap.of([...defaultKeymap, ...historyKeymap]),
        EditorView.domEventHandlers({
          paste: (event, view) => {
            const html = event.clipboardData?.getData('text/html');
            if (!html) return false;
            event.preventDefault();
            field.invokeMethodAsync('PastedAsync', JSON.stringify(pastedTree(html)))
              .then(markdown => {
                if (markdown) view.dispatch(view.state.replaceSelection(markdown), { scrollIntoView: true, userEvent: 'input.paste' });
              });
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
    if (event.target.closest('button') && !event.target.closest('.rich-text-panel')) event.preventDefault();
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
