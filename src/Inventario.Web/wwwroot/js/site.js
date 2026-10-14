(() => {
  const loadingBar = document.getElementById("app-loading-bar");
  const loadingOverlay = document.getElementById("app-loading-overlay");

  /** @type {HTMLElement | null} */
  let modalTrigger = null;
  /** @type {(() => void) | null} */
  let modalOnClose = null;

  /**
   * Backdrop del action-modal: no cierra al clic (evita perder datos del formulario).
   * Cierre: Esc (nativo), data-modal-close / Cancelar, o InventarioUi.closeModal.
   */

  function resolveActionModal(idOrElement) {
    if (idOrElement == null || idOrElement === "") {
      return document.getElementById("app-action-modal");
    }
    if (typeof idOrElement === "string") {
      return document.getElementById(idOrElement);
    }
    return idOrElement;
  }

  function focusableIn(container) {
    if (!container) return [];
    return Array.from(
      container.querySelectorAll(
        'button:not([disabled]):not([hidden]), [href], input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
      )
    ).filter((el) => {
      if (el.closest("[hidden]")) return false;
      if (el.getAttribute("aria-hidden") === "true") return false;
      const style = window.getComputedStyle(el);
      return style.display !== "none" && style.visibility !== "hidden";
    });
  }

  function openModal(idOrElement, options = {}) {
    const dialog = resolveActionModal(idOrElement);
    if (!dialog || typeof dialog.showModal !== "function") {
      return;
    }

    modalTrigger =
      document.activeElement instanceof HTMLElement ? document.activeElement : null;
    modalOnClose = typeof options.onClose === "function" ? options.onClose : null;

    if (typeof options.title === "string") {
      const titleEl = dialog.querySelector("[data-modal-title]");
      if (titleEl) {
        titleEl.textContent = options.title;
      }
    }

    if (!dialog.open) {
      dialog.showModal();
    }

    const focusables = focusableIn(dialog);
    const preferred =
      dialog.querySelector("[data-modal-initial-focus]") || focusables[0] || dialog;
    if (preferred instanceof HTMLElement) {
      preferred.focus();
    }
  }

  function closeModal(idOrElement) {
    const dialog = resolveActionModal(idOrElement);
    if (dialog && dialog.open) {
      dialog.close();
    }
  }

  function onActionModalClosed(dialog) {
    const cb = modalOnClose;
    modalOnClose = null;
    try {
      cb?.();
    } finally {
      const trigger = modalTrigger;
      modalTrigger = null;
      if (trigger && typeof trigger.focus === "function" && document.contains(trigger)) {
        trigger.focus();
      }
    }
  }

  window.InventarioUi = {
    showLoading(withOverlay = false) {
      loadingBar?.classList.add("is-active");
      if (withOverlay) {
        loadingOverlay?.classList.add("is-active");
      }
    },
    hideLoading() {
      loadingBar?.classList.remove("is-active");
      loadingOverlay?.classList.remove("is-active");
    },
    openConfirm() {
      const dialog = document.getElementById("app-confirm-dialog");
      if (dialog && typeof dialog.showModal === "function") {
        dialog.showModal();
      }
    },
    closeConfirm() {
      const dialog = document.getElementById("app-confirm-dialog");
      dialog?.close();
    },
    openModal,
    closeModal
  };

  document.querySelectorAll("[data-confirm-open]").forEach((el) => {
    el.addEventListener("click", (e) => {
      e.preventDefault();
      window.InventarioUi.openConfirm();
    });
  });

  document.addEventListener("click", (e) => {
    const target = e.target;
    if (!(target instanceof Element)) return;

    const openEl = target.closest("[data-modal-open]");
    if (openEl) {
      e.preventDefault();
      openModal(openEl.getAttribute("data-modal-open"));
      return;
    }

    const closeEl = target.closest("[data-modal-close]");
    if (closeEl) {
      const dialog = closeEl.closest("dialog.action-modal");
      if (dialog) {
        e.preventDefault();
        closeModal(dialog);
      }
    }
  });

  document.addEventListener(
    "close",
    (e) => {
      const dialog = e.target;
      if (!(dialog instanceof HTMLDialogElement)) return;
      if (!dialog.classList.contains("action-modal")) return;
      onActionModalClosed(dialog);
    },
    true
  );

  // Focus trap básico dentro del action-modal abierto.
  document.addEventListener("keydown", (e) => {
    if (e.key !== "Tab") return;
    const dialog = document.querySelector("dialog.action-modal[open]");
    if (!(dialog instanceof HTMLDialogElement)) return;

    const focusables = focusableIn(dialog);
    if (focusables.length === 0) {
      e.preventDefault();
      dialog.focus();
      return;
    }

    const first = focusables[0];
    const last = focusables[focusables.length - 1];
    if (e.shiftKey && document.activeElement === first) {
      e.preventDefault();
      last.focus();
    } else if (!e.shiftKey && document.activeElement === last) {
      e.preventDefault();
      first.focus();
    }
  });
})();
