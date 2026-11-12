export function enterSends(field, composer) {
  const keydown = (event) => {
    if (event.isComposing || event.keyCode === 229) return;
    if (event.key === 'Enter' && !event.shiftKey && !event.altKey && !event.ctrlKey && !event.metaKey) {
      event.preventDefault();
      composer.invokeMethodAsync('SendFromKeyboardAsync', field.value);
    } else if (event.key === 'Escape') {
      composer.invokeMethodAsync('EscapeAsync');
    }
  };
  field.addEventListener('keydown', keydown);
  return {
    dispose() {
      field.removeEventListener('keydown', keydown);
    },
  };
}
