self.importScripts('./service-worker-assets.js');

const cachePrefix = 'gsbc-offline-';
const cacheName = `${cachePrefix}${self.assetsManifest.version}`;
const scope = new URL(self.registration.scope);
const shellUrl = new URL('index.html', scope).href;

const include = [/\.dll$/, /\.wasm$/, /\.js$/, /\.mjs$/, /\.json$/, /\.css$/, /\.woff2?$/, /\.png$/, /\.svg$/,
                 /\.jpg$/, /\.webp$/, /\.ico$/, /\.webmanifest$/, /\.dat$/, /\.rss$/];
const exclude = [/^service-worker\.js$/, /^index\.html$/, /\.pdb$/];

const assets = self.assetsManifest.assets
    .filter(a => include.some(p => p.test(a.url)) && !exclude.some(p => p.test(a.url)));
const assetUrls = new Set(assets.map(a => new URL(a.url, scope).href));

self.addEventListener('install', event => event.waitUntil((async () => {
    const cache = await caches.open(cacheName);
    // Cloudflare's edge and the zone's 4-hour browser TTL can hand back an older copy, which fails the integrity
    // check and the whole install; the build's version in the query gets past both.
    const bust = `__v=${encodeURIComponent(self.assetsManifest.version)}`;
    await Promise.all(assets.map(async a => {
        const url = new URL(a.url, scope).href;
        const response = await fetch(new Request(`${url}?${bust}`, { integrity: a.hash, cache: 'no-cache' }));
        if (!response.ok) throw new Error(`${a.url}: HTTP ${response.status}`);
        await cache.put(url, response);
    }));
    await cache.put(shellUrl, await fetch(`${shellUrl}?${bust}`, { cache: 'no-cache' }));
})()));

self.addEventListener('message', event => { if (event.data === 'skip-waiting') self.skipWaiting(); });

self.addEventListener('activate', event => event.waitUntil((async () => {
    for (const key of await caches.keys())
        if (key.startsWith(cachePrefix) && key !== cacheName)
            await caches.delete(key);
    await self.clients.claim();
})()));

self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET' || !request.url.startsWith(scope.href))
        return;
    if (request.mode === 'navigate' && !assetUrls.has(request.url)) {
        event.respondWith((async () => (await (await caches.open(cacheName)).match(shellUrl)) ?? fetch(request))());
        return;
    }
    const url = request.url.split('?')[0];
    if (assetUrls.has(url))
        event.respondWith((async () => (await (await caches.open(cacheName)).match(url)) ?? fetch(request))());
});
