const CACHE = 'gsbc-pwa';

self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', (event) => event.waitUntil(self.clients.claim()));

self.addEventListener('fetch', (event) => {
  const request = event.request;
  if (request.method !== 'GET' || new URL(request.url).origin !== self.location.origin) return;
  // baptist.com.au's Cloudflare zone stretches css/js to a 4-hour browser cache; revalidate so staff see each build.
  const fresh = request.mode === 'navigate' ? fetch(request) : fetch(request, { cache: 'no-cache' });
  event.respondWith(
    fresh
      .then((response) => {
        if (response.ok) {
          const copy = response.clone();
          caches.open(CACHE).then((cache) => cache.put(request, copy));
        }
        return response;
      })
      .catch(() => caches.match(request).then((hit) => hit ?? caches.match('/'))),
  );
});
