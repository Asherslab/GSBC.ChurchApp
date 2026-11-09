(() => {
    const script = document.currentScript;
    const origin = script ? new URL(script.src).origin : "";

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

    const keepsItsColumn = frame =>
        frame.dataset.gsbcWidth === "column" || frame.classList.contains("gsbc-column");

    const widen = frame => {
        const container = frame.closest(".container");
        const parent = frame.parentElement;
        if (!container || !parent || keepsItsColumn(frame)) return;
        const box = container.getBoundingClientRect();
        const style = getComputedStyle(container);
        const left = box.left + parseFloat(style.paddingLeft);
        const width = box.width - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
        const inner = parent.getBoundingClientRect();
        const parentStyle = getComputedStyle(parent);
        const naturalLeft = inner.left + parseFloat(parentStyle.paddingLeft);
        const naturalWidth = inner.width - parseFloat(parentStyle.paddingLeft) - parseFloat(parentStyle.paddingRight);
        if (width <= naturalWidth + 1) {
            frame.style.removeProperty("margin-left");
            frame.style.removeProperty("max-width");
            frame.style.width = "100%";
            return;
        }
        frame.style.display = "block";
        frame.style.maxWidth = "none";
        frame.style.width = `${Math.round(width)}px`;
        frame.style.marginLeft = `${Math.round(left - naturalLeft)}px`;
    };

    const frames = [...document.querySelectorAll("iframe")].filter(frame => {
        try { return frame.hasAttribute("data-gsbc-embed") || (origin && new URL(frame.src, location.href).origin === origin); }
        catch { return false; }
    });

    for (const frame of frames) {
        const base = new URL(frame.src, location.href);
        const first = toEmbedPath(base, location.search);
        if (first !== base.pathname + base.search) frame.src = new URL(first, base.origin).href;
        if (!frame.title) frame.title = base.pathname.startsWith("/events") ? "Events" : "Sermons";
        frame.style.border = "0";
        let lastPath = null;

        widen(frame);
        addEventListener("resize", () => widen(frame));
        const container = frame.closest(".container");
        if (container) new ResizeObserver(() => widen(frame)).observe(container);

        addEventListener("message", event => {
            if (event.source !== frame.contentWindow || event.origin !== base.origin || event.data?.type !== "gsbc-embed") return;
            frame.contentWindow.postMessage({ type: "gsbc-embed-website" }, base.origin);
            if (event.data.height > 0) {
                frame.setAttribute("scrolling", "no");
                frame.style.height = `${event.data.height}px`;
            }
            if (event.data.path === lastPath) return;
            const moved = lastPath !== null;
            lastPath = event.data.path;
            const query = toWebsiteQuery(base, event.data.path);
            const address = location.pathname + (query ? `?${query}` : "") + location.hash;
            if (address !== location.pathname + location.search + location.hash) history.replaceState(null, "", address);
            const top = frame.getBoundingClientRect().top;
            if (moved && top < 0) {
                const still = matchMedia("(prefers-reduced-motion: reduce)").matches;
                window.scrollTo({ top: Math.max(0, window.scrollY + top - 16), behavior: still ? "auto" : "smooth" });
            }
        });
    }
})();
