export function attach(root, field, composer, enterSends) {
  const sendButton = (event) => event.target.closest?.('.composer__send');

  const keepFieldFocused = (event) => {
    if (sendButton(event) && document.activeElement === field) event.preventDefault();
  };

  const sendByTouch = (event) => {
    const button = sendButton(event);
    if (!button || document.activeElement !== field) return;
    const touch = event.changedTouches[0];
    const box = button.getBoundingClientRect();
    if (touch.clientX < box.left || touch.clientX > box.right || touch.clientY < box.top || touch.clientY > box.bottom) return;
    event.preventDefault();
    if (!button.disabled) button.click();
    field.focus({ preventScroll: true });
  };

  const keydown = (event) => {
    if (event.isComposing || event.keyCode === 229) return;
    if (event.key === 'Enter' && !event.shiftKey && !event.altKey && !event.ctrlKey && !event.metaKey) {
      event.preventDefault();
      composer.invokeMethodAsync('SendFromKeyboardAsync', field.value);
    } else if (event.key === 'Escape') {
      composer.invokeMethodAsync('EscapeAsync');
    }
  };

  root.addEventListener('mousedown', keepFieldFocused);
  root.addEventListener('touchend', sendByTouch);
  if (enterSends) field.addEventListener('keydown', keydown);
  return {
    dispose() {
      root.removeEventListener('mousedown', keepFieldFocused);
      root.removeEventListener('touchend', sendByTouch);
      field.removeEventListener('keydown', keydown);
    },
  };
}
