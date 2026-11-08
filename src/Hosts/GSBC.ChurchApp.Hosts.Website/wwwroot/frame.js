(() => {
    const root = document.documentElement;
    const framed = window.parent !== window;
    let inWebsite = false;

    const tell = () => window.parent.postMessage({
        type: "gsbc-embed",
        height: Math.ceil(root.getBoundingClientRect().height),
        path: location.pathname + location.search,
        title: document.title,
    }, "*");

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

    const status = (button, words) => {
        const label = button.querySelector("[data-share-label]");
        if (!label) return;
        const before = label.textContent;
        label.textContent = words;
        setTimeout(() => { label.textContent = before; }, 2500);
    };

    for (const button of document.querySelectorAll("[data-share-embed]")) {
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

    const audio = document.querySelector("audio[data-title]");
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
})();
