const shortcuts = { b: "bold", i: "italic", k: "link" };

export function attach(textarea) {
    textarea.closest(".rich-text-editor")?.addEventListener("keydown", event => {
        if (event.key !== "Enter" || !event.target.matches(".rich-text-panel input")) return;
        event.preventDefault();
        event.target.closest(".rich-text-panel").querySelector("[data-action='add-link']:not([disabled])")?.click();
    });
    textarea.addEventListener("keydown", event => {
        const action = (event.metaKey || event.ctrlKey) && !event.altKey && shortcuts[event.key.toLowerCase()];
        if (!action) return;
        const button = textarea.closest(".rich-text-editor")?.querySelector(`[data-action="${action}"]`);
        if (!button) return;
        event.preventDefault();
        button.click();
    });
}

export function selection(textarea) {
    return `${textarea.selectionStart},${textarea.selectionEnd},${textarea.value}`;
}

export function select(textarea, start, end) {
    textarea.focus();
    textarea.setSelectionRange(start, end);
}
