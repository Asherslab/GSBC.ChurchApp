export function wakeOnReturn(receiver) {
  const wake = () => receiver.invokeMethodAsync('Wake');
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') wake();
  });
  window.addEventListener('online', wake);
}
