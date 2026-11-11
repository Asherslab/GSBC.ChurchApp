export function hand(input, files) {
  if (!files.length || input.disabled) return;
  const transfer = new DataTransfer();
  for (const file of files) transfer.items.add(file);
  input.files = transfer.files;
  input.dispatchEvent(new Event('change', { bubbles: true }));
}

const filesIn = (list) => Array.from(list ?? []).filter((file) => file.size > 0);

export function listen(input, dropOn, takesPaste) {
  const zone = dropOn ? input.closest(dropOn) : null;
  const paste = (event) => {
    const pasted = filesIn(event.clipboardData?.files);
    if (!pasted.length) return;
    event.preventDefault();
    hand(input, pasted);
  };
  const over = (event) => {
    if (!Array.from(event.dataTransfer?.types ?? []).includes('Files')) return;
    event.preventDefault();
    zone.classList.add('is-dropping');
  };
  const leave = (event) => {
    if (!zone.contains(event.relatedTarget)) zone.classList.remove('is-dropping');
  };
  const drop = (event) => {
    zone.classList.remove('is-dropping');
    const dropped = filesIn(event.dataTransfer?.files);
    if (!dropped.length) return;
    event.preventDefault();
    hand(input, dropped);
  };
  if (takesPaste) document.addEventListener('paste', paste);
  zone?.addEventListener('dragover', over);
  zone?.addEventListener('dragleave', leave);
  zone?.addEventListener('drop', drop);
  return {
    dispose() {
      document.removeEventListener('paste', paste);
      zone?.removeEventListener('dragover', over);
      zone?.removeEventListener('dragleave', leave);
      zone?.removeEventListener('drop', drop);
    },
  };
}
