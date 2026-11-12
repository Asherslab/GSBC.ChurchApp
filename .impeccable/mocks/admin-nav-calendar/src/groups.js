(function () {
  const ICON = { "Import from the website": "globe", "Sermons": "sermons", "Weekly digest": "megaphone", "Calendar": "calendar", "Announcements": "send",
    "App styling": "pencil", "Insights": "sparkles", "Media": "image", "Places in the app": "link", "Chats": "message-square", "Life Groups": "book-open",
    "Ministries": "users", "People": "users", "Prayer": "prayer", "Safe Ministry Check": "refresh", "Safeguarding": "shield-check", "Feedback": "message-square",
    "App review": "smartphone", "App health": "shield-check", "Design gallery": "palette" };
  window.SK_NAV = window.GROUPS.map(([area, pages]) => [area, pages.map(p => [p, ICON[p] || "file"])]);
  document.addEventListener("DOMContentLoaded", () => {
    const map = document.querySelector("[data-map]");
    map.innerHTML = window.GROUPS.map(([area, pages, why]) => `<section class="gm"><h3>${area}<span>${pages.length}</span></h3><p>${why}</p><ul>${pages.map(p => `<li><i data-icon="${ICON[p] || "file"}"></i>${p}${(window.MOVED || []).includes(p) ? '<em>moved</em>' : ""}</li>`).join("")}</ul></section>`).join("");
    window.fillIcons(map);
  });
})();
