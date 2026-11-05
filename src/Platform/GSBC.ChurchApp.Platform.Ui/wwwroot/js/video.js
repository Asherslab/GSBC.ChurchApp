export function listen(frame, receiver) {
  const onMessage = (event) => {
    if (event.source !== frame.contentWindow || event.data?.source !== 'gsbc-youtube') return;
    receiver.invokeMethodAsync('OnVideoState', event.data.state, event.data.code ?? null);
  };
  window.addEventListener('message', onMessage);
  return { dispose: () => window.removeEventListener('message', onMessage) };
}
