(() => {
    "use strict";

    const keyName = "_idempotencyKey";

    function newKey() {
        if (globalThis.crypto?.randomUUID) {
            return globalThis.crypto.randomUUID();
        }

        const bytes = new Uint8Array(16);
        globalThis.crypto.getRandomValues(bytes);
        return Array.from(bytes, x => x.toString(16).padStart(2, "0")).join("");
    }

    function wireConfirmationForms() {
        document.querySelectorAll("form[data-confirm]").forEach(form => {
            form.addEventListener("submit", event => {
                const message = form.dataset.confirm;

                if (message && !globalThis.confirm(message)) {
                    event.preventDefault();
                }
            });
        });
    }

    function wirePrintButtons() {
        document.querySelectorAll("[data-print-report]").forEach(button => {
            button.addEventListener("click", () => {
                globalThis.print();
            });
        });
    }

    document.addEventListener("DOMContentLoaded", () => {
        wirePrintButtons();

        wireConfirmationForms();

        document.querySelectorAll("form").forEach(form => {
            if ((form.method || "get").toLowerCase() !== "post") {
                return;
            }

            if (form.querySelector(`input[name="${keyName}"]`)) {
                return;
            }

            const input = document.createElement("input");
            input.type = "hidden";
            input.name = keyName;
            input.value = newKey();
            form.appendChild(input);
        });
    });
})();
