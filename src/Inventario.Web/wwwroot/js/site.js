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
   * ChangePassword (T03-02): submit AJAX → partial HTML con errores o JSON { redirectUrl }.
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

  function focusModal(dialog) {
    const focusables = focusableIn(dialog);
    const preferred =
      dialog.querySelector("[data-modal-initial-focus]") || focusables[0] || dialog;
    if (preferred instanceof HTMLElement) {
      preferred.focus();
    }
  }

  function parseUnobtrusive(form) {
    if (!form || !window.jQuery) return;
    const $ = window.jQuery;
    if ($.validator && $.validator.unobtrusive) {
      $(form).removeData("validator");
      $(form).removeData("unobtrusiveValidation");
      $.validator.unobtrusive.parse(form);
    }
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

    focusModal(dialog);
  }

  function closeModal(idOrElement) {
    const dialog = resolveActionModal(idOrElement);
    if (dialog && dialog.open) {
      dialog.close();
    }
  }

  function resetChangePasswordForm(dialog) {
    const form = dialog?.querySelector("#change-password-form");
    if (!(form instanceof HTMLFormElement)) return;
    form.reset();
    form.querySelectorAll(".form-error").forEach((el) => {
      el.textContent = "";
    });
    form.querySelectorAll(".input-validation-error").forEach((el) => {
      el.classList.remove("input-validation-error");
    });
  }

  function onActionModalClosed(dialog) {
    const cb = modalOnClose;
    modalOnClose = null;
    try {
      cb?.();
      resetChangePasswordForm(dialog);
    } finally {
      const trigger = modalTrigger;
      modalTrigger = null;
      if (trigger && typeof trigger.focus === "function" && document.contains(trigger)) {
        trigger.focus();
      }
    }
  }

  async function submitModalAjaxForm(form) {
    const dialog = form.closest("dialog.action-modal");
    window.InventarioUi.showLoading(true);

    try {
      const response = await fetch(form.action, {
        method: "POST",
        body: new FormData(form),
        headers: {
          "X-Requested-With": "XMLHttpRequest",
          Accept: "application/json, text/html"
        },
        credentials: "same-origin"
      });

      const contentType = response.headers.get("content-type") || "";

      if (contentType.includes("application/json")) {
        const data = await response.json();
        if (data && typeof data.redirectUrl === "string" && data.redirectUrl) {
          window.location.assign(data.redirectUrl);
          return;
        }
      }

      if (contentType.includes("text/html")) {
        const html = await response.text();
        const body = dialog?.querySelector("[data-modal-body]");
        if (body) {
          body.innerHTML = html;
          const newForm = body.querySelector("form");
          parseUnobtrusive(newForm);
        }
        if (dialog && !dialog.open) {
          openModal(dialog);
        } else if (dialog) {
          focusModal(dialog);
        }
        return;
      }

      if (!response.ok) {
        const body = dialog?.querySelector("[data-modal-body]");
        if (body && !body.querySelector(".form-error[data-modal-generic-error]")) {
          const p = document.createElement("p");
          p.className = "form-error";
          p.dataset.modalGenericError = "true";
          p.setAttribute("role", "alert");
          p.textContent = "No se pudo completar la acción. Intenta de nuevo.";
          body.prepend(p);
        }
      }
    } catch {
      const body = dialog?.querySelector("[data-modal-body]");
      if (body) {
        let alert = body.querySelector("[data-modal-generic-error]");
        if (!alert) {
          alert = document.createElement("p");
          alert.className = "form-error";
          alert.dataset.modalGenericError = "true";
          alert.setAttribute("role", "alert");
          body.prepend(alert);
        }
        alert.textContent = "No se pudo completar la acción. Intenta de nuevo.";
      }
    } finally {
      window.InventarioUi.hideLoading();
    }
  }

  const NAV_MQ = window.matchMedia("(max-width: 800px)");
  const navToggle = document.getElementById("app-nav-toggle");
  const navOverlay = document.getElementById("app-nav-overlay");
  const navSidebar = document.getElementById("app-sidebar");
  const navIconOpen = navToggle?.querySelector(".app-nav-toggle-icon-open");
  const navIconClose = navToggle?.querySelector(".app-nav-toggle-icon-close");

  function isNarrowNav() {
    return NAV_MQ.matches;
  }

  function syncNavToggleUi(open) {
    if (!navToggle) return;
    navToggle.setAttribute("aria-expanded", open ? "true" : "false");
    navToggle.setAttribute(
      "aria-label",
      open ? "Cerrar menú de navegación" : "Abrir menú de navegación"
    );
    if (navIconOpen instanceof HTMLElement) {
      navIconOpen.hidden = open;
    }
    if (navIconClose instanceof HTMLElement) {
      navIconClose.hidden = !open;
    }
    if (navOverlay) {
      navOverlay.hidden = !open;
    }
  }

  function openNav() {
    if (!isNarrowNav()) return;
    document.body.classList.add("nav-open");
    syncNavToggleUi(true);
    const firstLink = navSidebar?.querySelector(".app-nav-link");
    if (firstLink instanceof HTMLElement) {
      firstLink.focus();
    }
  }

  function closeNav() {
    const wasOpen = document.body.classList.contains("nav-open");
    document.body.classList.remove("nav-open");
    syncNavToggleUi(false);
    if (wasOpen && navToggle instanceof HTMLElement && isNarrowNav()) {
      navToggle.focus();
    }
  }

  function toggleNav() {
    if (document.body.classList.contains("nav-open")) {
      closeNav();
    } else {
      openNav();
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
    closeModal,
    openNav,
    closeNav,
    toggleNav
  };

  navToggle?.addEventListener("click", (e) => {
    e.preventDefault();
    toggleNav();
  });

  navOverlay?.addEventListener("click", () => {
    closeNav();
  });

  document.querySelectorAll(".app-nav-link").forEach((link) => {
    link.addEventListener("click", () => {
      if (isNarrowNav()) {
        closeNav();
      }
    });
  });

  document.addEventListener("keydown", (e) => {
    if (e.key !== "Escape") return;
    if (!document.body.classList.contains("nav-open")) return;
    if (document.querySelector("dialog.action-modal[open], dialog.confirm-dialog[open]")) {
      return;
    }
    e.preventDefault();
    closeNav();
  });

  function onNavViewportChange() {
    if (!isNarrowNav()) {
      closeNav();
    }
  }

  if (typeof NAV_MQ.addEventListener === "function") {
    NAV_MQ.addEventListener("change", onNavViewportChange);
  } else if (typeof NAV_MQ.addListener === "function") {
    NAV_MQ.addListener(onNavViewportChange);
  }

  syncNavToggleUi(false);

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
      const menu = openEl.closest("details.app-header-menu");
      if (menu) {
        menu.open = false;
      }
      const title = openEl.getAttribute("data-modal-title") || undefined;
      openModal(openEl.getAttribute("data-modal-open"), title ? { title } : {});
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

  document.addEventListener("submit", (e) => {
    const form = e.target;
    if (!(form instanceof HTMLFormElement)) return;
    if (!form.hasAttribute("data-modal-ajax-form")) return;
    e.preventDefault();
    void submitModalAjaxForm(form);
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

  const initialForm = document.getElementById("change-password-form");
  if (initialForm) {
    parseUnobtrusive(initialForm);
  }
})();
