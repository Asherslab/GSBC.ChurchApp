(() => {
    const root = document.documentElement;
    const framed = window.parent !== window;
    const pages = /^\/(events|sermons)(\/|$)/;
    let inWebsite = false;

    const tell = () => window.parent.postMessage({
        type: "gsbc-embed",
        height: Math.ceil(root.getBoundingClientRect().height),
        path: location.pathname + location.search,
        title: document.title,
    }, "*");

    const status = (button, words) => {
        const label = button.querySelector("[data-share-label]");
        if (!label) return;
        const before = label.textContent;
        label.textContent = words;
        setTimeout(() => { label.textContent = before; }, 2500);
    };

    const wire = scope => {
        for (const button of scope.querySelectorAll("[data-share-embed]")) {
            button.hidden = false;
            button.addEventListener("click", async () => {
                const url = inWebsite ? button.dataset.shareWebsite : button.dataset.shareEmbed;
                if (navigator.share) {
                    try { await navigator.share({ title: document.title, url }); } catch { }
                    return;
                }
                try {
                    await navigator.clipboard.writeText(url);
                    status(button, "Link copied");
                } catch {
                    window.prompt("Copy this link", url);
                }
            });
        }

        const audio = scope.querySelector("audio[data-title]");
        if (audio && "mediaSession" in navigator) {
            audio.addEventListener("play", () => {
                navigator.mediaSession.metadata = new MediaMetadata({
                    title: audio.dataset.title,
                    artist: audio.dataset.artist,
                    album: audio.dataset.album,
                    artwork: audio.dataset.artwork ? [{ src: audio.dataset.artwork }] : [],
                });
            });
        }
    };

    let showing = 0;
    const show = async (url, how) => {
        const ticket = ++showing;
        let page;
        try {
            const response = await fetch(url, { credentials: "same-origin" });
            page = new DOMParser().parseFromString(await response.text(), "text/html");
            if (!response.ok && response.status !== 404) throw new Error(response.statusText);
        } catch {
            location.assign(url);
            return;
        }
        const next = page.querySelector("main.embed");
        const current = document.querySelector("main.embed");
        if (ticket !== showing || !next || !current) {
            if (!next || !current) location.assign(url);
            return;
        }
        const swap = () => {
            current.replaceWith(document.adoptNode(next));
            document.title = page.title;
            if (how === "push") history.pushState(null, "", url);
            if (how !== "pop") window.scrollTo(0, 0);
            wire(next);
            if (framed) tell();
        };
        if (document.startViewTransition) document.startViewTransition(swap);
        else swap();
    };

    const ours = url => url.origin === location.origin && pages.test(url.pathname);

    document.addEventListener("click", event => {
        const link = event.target.closest?.("a[href]");
        if (!link || event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
        if ((link.target && link.target !== "_self") || link.hasAttribute("download")) return;
        const url = new URL(link.href, location.href);
        if (!ours(url) || (url.pathname === location.pathname && url.search === location.search && url.hash)) return;
        event.preventDefault();
        show(url.href, "push");
    });

    document.addEventListener("submit", event => {
        const form = event.target;
        if (event.defaultPrevented || (form.method || "get").toLowerCase() !== "get") return;
        const url = new URL(form.action, location.href);
        if (!ours(url)) return;
        event.preventDefault();
        url.search = new URLSearchParams(new FormData(form)).toString();
        show(url.href, "push");
    });

    addEventListener("popstate", () => show(location.href, "pop"));

    if (framed) {
        root.classList.add("in-frame");
        new ResizeObserver(tell).observe(root);
        addEventListener("load", tell);
        addEventListener("message", event => {
            if (event.source === window.parent && event.data?.type === "gsbc-embed-website") {
                inWebsite = true;
                root.classList.add("in-website");
            }
        });
        tell();
    }

    wire(document);
})();
