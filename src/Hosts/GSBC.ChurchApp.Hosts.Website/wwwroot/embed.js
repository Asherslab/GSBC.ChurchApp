(() => {
    const kinds = [
        ["sermon", /^\/sermons\/(?!series$|speakers$)([^/]+)$/, id => `/sermons/${id}`],
        ["series", /^\/sermons\/series\/([^/]+)$/, id => `/sermons/series/${id}`],
        ["speaker", /^\/sermons\/speakers\/([^/]+)$/, id => `/sermons/speakers/${id}`],
        ["event", /^\/events\/([^/]+)$/, id => `/events/${id}`],
    ];
    const views = [["series", "/sermons/series"], ["speakers", "/sermons/speakers"]];

    const toEmbedPath = (base, query) => {
        const params = new URLSearchParams(query);
        let path = base.pathname;
        for (const [key, , build] of kinds) {
            const id = params.get(key);
            if (id) { path = build(encodeURIComponent(id)); params.delete(key); break; }
        }
        const view = views.find(([name]) => name === params.get("view"));
        if (view) { path = view[1]; params.delete("view"); }
        const merged = new URLSearchParams(base.search);
        for (const [key, value] of params) merged.set(key, value);
        const search = merged.toString();
        return path + (search ? `?${search}` : "");
    };

    const toWebsiteQuery = (base, embedPath) => {
        const url = new URL(embedPath, base);
        const params = new URLSearchParams();
        const kind = kinds.find(([, pattern]) => pattern.test(url.pathname));
        const view = views.find(([, path]) => path === url.pathname);
        if (kind) params.set(kind[0], decodeURIComponent(url.pathname.match(kind[1])[1]));
        else if (view) params.set("view", view[0]);
        const limits = new URLSearchParams(base.search);
        for (const [key, value] of url.searchParams) if (limits.get(key) !== value) params.set(key, value);
        return params.toString();
    };

    for (const frame of document.querySelectorAll("iframe[data-gsbc-embed]")) {
        const base = new URL(frame.src);
        const first = toEmbedPath(base, location.search);
        if (first !== base.pathname + base.search) frame.src = new URL(first, base.origin).href;
        frame.style.height = frame.style.height || "1200px";
        frame.setAttribute("scrolling", "no");
        let lastPath = null;

        addEventListener("message", event => {
            if (event.source !== frame.contentWindow || event.origin !== base.origin || event.data?.type !== "gsbc-embed") return;
            frame.contentWindow.postMessage({ type: "gsbc-embed-website" }, base.origin);
            if (event.data.height > 0) frame.style.height = `${event.data.height}px`;
            if (event.data.path === lastPath) return;
            const moved = lastPath !== null;
            lastPath = event.data.path;
            const query = toWebsiteQuery(base, event.data.path);
            const address = location.pathname + (query ? `?${query}` : "") + location.hash;
            if (address !== location.pathname + location.search + location.hash) history.replaceState(null, "", address);
            if (moved && frame.getBoundingClientRect().top < 0) frame.scrollIntoView({ block: "start" });
        });
    }
})();
