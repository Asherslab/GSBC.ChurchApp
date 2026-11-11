(function () {
  const svg = (name, cls) => `<svg class="icon ${cls || "icon--sm"}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${(window.ICONS || {})[name] || ""}</svg>`;
  window.svg = svg;

  const NAV = [
    ["Sundays & sermons", [["Import from the website", "globe"], ["Sermons", "sermons"], ["Weekly digest", "megaphone"]]],
    ["Calendar & news", [["Calendar", "calendar"], ["Announcements", "send"]]],
    ["The app", [["App styling", "pencil"], ["Insights", "sparkles"], ["Media", "image"], ["Places in the app", "link"]]],
    ["People", [["Chats", "message-square"], ["Life Groups", "book-open"], ["Ministries", "users"], ["People", "users"], ["Prayer", "prayer"], ["Safe Ministry Check", "refresh"], ["Safeguarding", "shield-check"]]],
    ["Technical", [["Feedback", "message-square"], ["App review", "smartphone"], ["App health", "shield-check"]]],
  ];

  function sidebar(current, icons) {
    const ic = (title, fallback) => (icons && icons[title]) || fallback;
    const link = (title, icon, on) => `<div class="module-nav__row"><a href="#" class="module-nav__link" ${on ? 'aria-current="page"' : ""}>${svg(ic(title, icon))}<span class="module-nav__title">${title}</span></a></div>`;
    const areas = (window.SK_NAV || NAV).map(([area, pages]) => {
      const open = pages.some(([t]) => t === current);
      return `<div class="module-nav__group"><div class="module-nav__area ${open ? "module-nav__area--open" : ""}"><button type="button" class="module-nav__area-head" aria-expanded="${open}">${svg("chevron-right", "icon--sm module-nav__chevron")}<span>${area}</span><span class="module-nav__pages">${pages.length}</span></button><div class="module-nav__area-pages" ${open ? "" : "hidden"}>${pages.map(([t, i]) => link(t, i, t === current)).join("")}</div></div></div>`;
    }).join("");
    return `<aside class="admin-sidebar"><div class="admin-sidebar__head"><a class="admin-brand" href="#"><span class="brand-mark" aria-hidden="true" style="width:28px"></span><span>Good Shepherd</span></a></div>
      <div class="admin-sidebar__nav"><nav class="module-nav" aria-label="Main"><button type="button" class="module-nav__jump">${svg("search")}<span>Jump to…</span><kbd>⌘K</kbd></button>
      <div class="module-nav__group">${link("Home", "home", current === "Home")}</div>${areas}
      <div class="module-nav__foot" data-sk-foot>${link("Send feedback", ic("Send feedback", "message-square"))}${link("Account", "user")}</div></nav></div></aside>`;
  }
  window.sidebar = sidebar;

  function themeToggle() {
    const root = document.documentElement;
    const want = new URLSearchParams(location.search).get("scheme") || (matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
    root.dataset.scheme = want;
    const bar = document.createElement("div");
    bar.className = "sk-theme";
    bar.innerHTML = `<button data-s="light">Light</button><button data-s="dark">Dark</button>`;
    const sync = () => bar.querySelectorAll("button").forEach(b => b.setAttribute("aria-pressed", String(b.dataset.s === root.dataset.scheme)));
    bar.addEventListener("click", e => { const s = e.target.dataset.s; if (s) { root.dataset.scheme = s; sync(); } });
    sync();
    document.body.appendChild(bar);
  }

  function fillIcons(scope) {
    scope.querySelectorAll("i[data-icon]").forEach(i => { i.outerHTML = svg(i.dataset.icon, i.dataset.size === "lg" ? "icon--lg" : i.dataset.size === "md" ? "" : "icon--sm"); });
  }
  window.fillIcons = fillIcons;

  document.addEventListener("DOMContentLoaded", () => {
    const body = document.body;
    if (body.classList.contains("sk-admin")) {
      const main = body.querySelector("main");
      const shell = document.createElement("div");
      shell.className = "admin-shell";
      shell.innerHTML = sidebar(body.dataset.current, window.SK_ICONS);
      main.classList.add("admin-main");
      shell.appendChild(main);
      body.prepend(shell);
    }
    if (body.classList.contains("sk-phone")) {
      const tabs = (body.dataset.tabs || "Home:home,Listen:headphones,Prayer:heart,Events:calendar,More:menu").split(",");
      const on = body.dataset.tab;
      body.insertAdjacentHTML("beforeend", `<div class="sk-status"><span>9:41</span><span>●●● ▮</span></div><nav class="sk-tabbar">${tabs.map(t => { const [l, i] = t.split(":"); return `<span class="${l === on ? "on" : ""}">${svg(i, "")}${l}</span>`; }).join("")}</nav>`);
    }
    fillIcons(document);
    themeToggle();
  });
})();
