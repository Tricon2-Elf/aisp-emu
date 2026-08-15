document.querySelectorAll("[data-modal-target]").forEach((trigger) => {
    trigger.addEventListener("click", () => {
        const dialog = document.getElementById(trigger.dataset.modalTarget);
        if (dialog instanceof HTMLDialogElement) {
            dialog.showModal();
            dialog.querySelector("textarea")?.focus();
        }
    });
});

document.querySelectorAll("[data-modal-close]").forEach((trigger) => {
    trigger.addEventListener("click", () => {
        trigger.closest("dialog")?.close();
    });
});

document.querySelectorAll("dialog").forEach((dialog) => {
    dialog.addEventListener("click", (event) => {
        if (event.target === dialog) {
            dialog.close();
        }
    });
});
