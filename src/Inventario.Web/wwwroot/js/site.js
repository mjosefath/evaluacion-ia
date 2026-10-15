(() => {
  const loadingBar = document.getElementById("app-loading-bar");
  const loadingOverlay = document.getElementById("app-loading-overlay");
  const alertsRoot = document.getElementById("app-alerts");

  /** @type {HTMLElement | null} */
  let modalTrigger = null;
  /** @type {(() => void) | null} */
  let modalOnClose = null;

  /** @type {((value: boolean) => void) | null} */
  let confirmResolver = null;

  /** @type {ReturnType<typeof setTimeout> | null} */
  let stillProcessingTimer = null;

  const ALERT_ICONS = {
    success: "lucide-circle-check",
    info: "lucide-info",
    warning: "lucide-triangle-alert",
    error: "lucide-circle-alert"
  };

  /**
   * Backdrop del action-modal: no cierra al clic (evita perder datos del formulario).
   * Cierre: Esc (nativo), data-modal-close / Cancelar, o InventarioUi.closeModal.
   * ChangePassword (T03-02): submit AJAX → partial HTML con errores o JSON { redirectUrl }.
   * Avisos: InventarioUi.notify (W3–W4). Confirm: InventarioUi.confirm (W6).
   */

  function uiMessage(key, fallback) {
    const fromData = alertsRoot?.getAttribute(key);
    return fromData && fromData.trim() ? fromData : fallback;
  }

  function ensureAlertsVisible() {
    if (!alertsRoot) return;
    alertsRoot.hidden = false;
    alertsRoot.removeAttribute("hidden");
  }

  function syncAlertsVisibility() {
    if (!alertsRoot) return;
    const has = alertsRoot.querySelector("[data-alert]");
    if (has) {
      ensureAlertsVisible();
    } else {
      alertsRoot.hidden = true;
    }
  }

  function dismissAlert(alertEl) {
    if (!(alertEl instanceof HTMLElement)) return;
    const t = alertEl._alertTimer;
    if (t) clearTimeout(t);
    alertEl.remove();
    syncAlertsVisibility();
  }

  function scheduleAutoDismiss(alertEl) {
    const ms = Number(alertEl.getAttribute("data-alert-autodismiss") || "0");
    if (!ms || ms < 1) return;
    alertEl._alertTimer = setTimeout(() => dismissAlert(alertEl), ms);
  }

  /**
   * @param {"success"|"info"|"warning"|"error"} type
   * @param {string} message
   */
  function notify(type, message) {
    if (!alertsRoot || !message) return;

    const kind = ["success", "info", "warning", "error"].includes(type)
      ? type
      : "info";
    const iconId = ALERT_ICONS[kind] || ALERT_ICONS.info;

    const el = document.createElement("div");
    el.className = `alert alert-${kind}`;
    el.dataset.alert = "";
    el.dataset.alertType = kind;
    if (kind === "error") {
      el.setAttribute("role", "alert");
    }
    if (kind === "success" || kind === "info") {
      el.setAttribute("data-alert-autodismiss", "5000");
    }

    el.innerHTML =
      `<svg class="icon alert-icon" aria-hidden="true" focusable="false"><use href="#${iconId}"></use></svg>` +
      `<div class="alert-content"></div>` +
      `<button type="button" class="alert-dismiss" data-alert-dismiss aria-label="Cerrar aviso">` +
      `<svg class="icon" aria-hidden="true" focusable="false"><use href="#lucide-x"></use></svg>` +
      `</button>`;

    const content = el.querySelector(".alert-content");
    if (content) content.textContent = message;

    alertsRoot.appendChild(el);
    ensureAlertsVisible();
    scheduleAutoDismiss(el);

    if (kind === "error" || kind === "warning") {
      el.scrollIntoView({ block: "nearest", behavior: "smooth" });
    }
  }

  function wireServerAlerts() {
    alertsRoot?.querySelectorAll("[data-alert]").forEach((el) => {
      if (el instanceof HTMLElement) scheduleAutoDismiss(el);
    });
  }

  function resetConfirmUi() {
    const dialog = document.getElementById("app-confirm-dialog");
    if (!dialog) return;
    const titleEl = dialog.querySelector("[data-confirm-title]");
    const messageEl = dialog.querySelector("[data-confirm-message]");
    const cancelBtn = dialog.querySelector("[data-confirm-cancel]");
    const acceptBtn = dialog.querySelector("[data-confirm-accept]");
    if (titleEl) titleEl.textContent = "Confirmar";
    if (messageEl) messageEl.textContent = "¿Deseas continuar?";
    if (cancelBtn instanceof HTMLButtonElement) {
      cancelBtn.textContent = "Cancelar";
    }
    if (acceptBtn instanceof HTMLButtonElement) {
      acceptBtn.textContent = "Confirmar";
      acceptBtn.className = "btn btn-danger";
    }
  }

  /**
   * @param {{
   *   title?: string,
   *   message?: string,
   *   acceptText?: string,
   *   cancelText?: string,
   *   tone?: "danger"|"neutral"
   * }} [options]
   * @returns {Promise<boolean>}
   */
  function confirm(options = {}) {
    const dialog = document.getElementById("app-confirm-dialog");
    if (!dialog || typeof dialog.showModal !== "function") {
      return Promise.resolve(false);
    }

    if (confirmResolver) {
      confirmResolver(false);
      confirmResolver = null;
    }

    const titleEl = dialog.querySelector("[data-confirm-title]");
    const messageEl = dialog.querySelector("[data-confirm-message]");
    const cancelBtn = dialog.querySelector("[data-confirm-cancel]");
    const acceptBtn = dialog.querySelector("[data-confirm-accept]");
    const tone = options.tone === "neutral" ? "neutral" : "danger";

    if (titleEl && typeof options.title === "string") {
      titleEl.textContent = options.title;
    }
    if (messageEl && typeof options.message === "string") {
      messageEl.textContent = options.message;
    }
    if (cancelBtn instanceof HTMLButtonElement) {
      cancelBtn.textContent =
        typeof options.cancelText === "string" ? options.cancelText : "Cancelar";
    }
    if (acceptBtn instanceof HTMLButtonElement) {
      acceptBtn.textContent =
        typeof options.acceptText === "string" ? options.acceptText : "Confirmar";
      acceptBtn.className = tone === "neutral" ? "btn btn-primary" : "btn btn-danger";
    }

    return new Promise((resolve) => {
      confirmResolver = resolve;
      if (!dialog.open) {
        dialog.showModal();
      }
      // W6: danger → foco en Cancelar (Enter no confirma).
      const focusTarget =
        tone === "danger" && cancelBtn instanceof HTMLElement
          ? cancelBtn
          : acceptBtn instanceof HTMLElement
            ? acceptBtn
            : null;
      focusTarget?.focus();
    });
  }

  function settleConfirm(accepted) {
    const dialog = document.getElementById("app-confirm-dialog");
    const resolve = confirmResolver;
    confirmResolver = null;
    dialog?.close();
    resetConfirmUi();
    resolve?.(!!accepted);
  }

  async function runConfirmFromTrigger(trigger) {
    const accepted = await confirm({
      title: trigger.getAttribute("data-confirm-title") || undefined,
      message: trigger.getAttribute("data-confirm-message") || undefined,
      acceptText: trigger.getAttribute("data-confirm-accept") || undefined,
      cancelText: trigger.getAttribute("data-confirm-cancel") || undefined,
      tone: trigger.getAttribute("data-confirm-tone") === "neutral" ? "neutral" : "danger"
    });
    if (!accepted) return;

    const action = trigger.getAttribute("data-confirm-action");
    if (action) {
      const target = document.querySelector(action);
      if (target instanceof HTMLFormElement) {
        target.requestSubmit();
        return;
      }
      if (target instanceof HTMLElement) {
        target.click();
        return;
      }
    }

    const href = trigger.getAttribute("data-confirm-href");
    if (href) {
      window.location.assign(href);
      return;
    }

    trigger.dispatchEvent(
      new CustomEvent("inventario:confirmed", { bubbles: true, cancelable: true })
    );
  }

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
        if (data?.notify && typeof data.notify.message === "string") {
          notify(data.notify.type || "info", data.notify.message);
        }
        if (data && typeof data.redirectUrl === "string" && data.redirectUrl) {
          window.location.assign(data.redirectUrl);
          return;
        }
        if (data?.refresh === true) {
          window.location.reload();
          return;
        }
        if (dialog?.open) {
          closeModal(dialog);
        }
        return;
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
        notify(
          "error",
          uiMessage("data-msg-modal-failed", "No se pudo completar la acción. Intenta de nuevo.")
        );
      }
    } catch {
      notify(
        "error",
        uiMessage("data-msg-connection", "No hay conexión con el servidor. Comprueba la red e inténtalo de nuevo.")
      );
    } finally {
      window.InventarioUi.hideLoading();
    }
  }

  /*
   * Nav toggle — mapa de iconos / aria (T05-07):
   * - Narrow drawer cerrado: menu, aria-expanded=false, "Abrir menú de navegación"
   * - Narrow drawer abierto: X, aria-expanded=true, "Cerrar menú de navegación"
   * - Desktop expandido (textos): X, aria-expanded=true, "Colapsar menú lateral"
   * - Desktop icon-rail (nav-collapsed): menu, aria-expanded=false, "Expandir menú lateral"
   * Icono X visible cuando el panel está "expandido" (drawer open o sidebar ancho).
   */
  const NAV_COLLAPSED_KEY = "inventario.navCollapsed";
  const NAV_MQ = window.matchMedia("(max-width: 800px)");
  const navToggle = document.getElementById("app-nav-toggle");
  const navOverlay = document.getElementById("app-nav-overlay");
  const navSidebar = document.getElementById("app-sidebar");
  const navIconMenu = navToggle?.querySelector(".app-nav-toggle-icon-open");
  const navIconClose = navToggle?.querySelector(".app-nav-toggle-icon-close");

  function isNarrowNav() {
    return NAV_MQ.matches;
  }

  function readStoredNavCollapsed() {
    try {
      return localStorage.getItem(NAV_COLLAPSED_KEY) === "1";
    } catch {
      return false;
    }
  }

  function writeStoredNavCollapsed(collapsed) {
    try {
      localStorage.setItem(NAV_COLLAPSED_KEY, collapsed ? "1" : "0");
    } catch {
      /* ignore */
    }
  }

  function isNavCollapsed() {
    return document.documentElement.classList.contains("nav-collapsed");
  }

  function isDrawerOpen() {
    return document.body.classList.contains("nav-open");
  }

  /** Sync iconos + aria según breakpoint (drawer vs icon-rail). */
  function syncNavToggleUi() {
    if (!navToggle) return;

    let expanded;
    let label;

    if (isNarrowNav()) {
      expanded = isDrawerOpen();
      label = expanded
        ? "Cerrar menú de navegación"
        : "Abrir menú de navegación";
      if (navOverlay) {
        navOverlay.hidden = !expanded;
      }
    } else {
      expanded = !isNavCollapsed();
      label = expanded ? "Colapsar menú lateral" : "Expandir menú lateral";
      if (navOverlay) {
        navOverlay.hidden = true;
      }
    }

    navToggle.setAttribute("aria-expanded", expanded ? "true" : "false");
    navToggle.setAttribute("aria-label", label);
    navToggle.setAttribute("title", label);

    if (navIconMenu instanceof HTMLElement) {
      navIconMenu.hidden = expanded;
    }
    if (navIconClose instanceof HTMLElement) {
      navIconClose.hidden = !expanded;
    }
  }

  function openNav() {
    if (!isNarrowNav()) return;
    document.body.classList.add("nav-open");
    syncNavToggleUi();
    const firstLink = navSidebar?.querySelector(".app-nav-link");
    if (firstLink instanceof HTMLElement) {
      firstLink.focus();
    }
  }

  function closeNav() {
    const wasOpen = isDrawerOpen();
    document.body.classList.remove("nav-open");
    syncNavToggleUi();
    if (wasOpen && navToggle instanceof HTMLElement && isNarrowNav()) {
      navToggle.focus();
    }
  }

  function toggleNav() {
    if (isDrawerOpen()) {
      closeNav();
    } else {
      openNav();
    }
  }

  function setNavCollapsed(collapsed) {
    if (isNarrowNav()) return;
    const next = !!collapsed;
    document.documentElement.classList.toggle("nav-collapsed", next);
    writeStoredNavCollapsed(next);
    syncNavToggleUi();
  }

  function toggleNavCollapsed() {
    if (isNarrowNav()) return;
    setNavCollapsed(!isNavCollapsed());
  }

  function onNavToggleClick(e) {
    e.preventDefault();
    if (isNarrowNav()) {
      toggleNav();
    } else {
      toggleNavCollapsed();
    }
  }

  function onNavViewportChange() {
    if (isNarrowNav()) {
      document.documentElement.classList.remove("nav-collapsed");
      syncNavToggleUi();
    } else {
      closeNav();
      setNavCollapsed(readStoredNavCollapsed());
    }
  }

  const THEME_KEY = "inventario.theme";

  function getTheme() {
    const attr = document.documentElement.getAttribute("data-theme");
    if (attr === "dark" || attr === "light") return attr;
    try {
      const stored = localStorage.getItem(THEME_KEY);
      if (stored === "dark") return "dark";
    } catch {
      /* ignore */
    }
    return "light";
  }

  function syncThemeToggleUi(theme) {
    const isDark = theme === "dark";
    document.querySelectorAll("[data-theme-toggle]").forEach((btn) => {
      if (!(btn instanceof HTMLElement)) return;
      btn.setAttribute(
        "aria-label",
        isDark ? "Activar modo claro" : "Activar modo oscuro"
      );
      btn.setAttribute("title", isDark ? "Modo claro" : "Modo oscuro");
      const moon = btn.querySelector(".theme-toggle-icon-moon");
      const sun = btn.querySelector(".theme-toggle-icon-sun");
      if (moon instanceof HTMLElement) moon.hidden = isDark;
      if (sun instanceof HTMLElement) sun.hidden = !isDark;
    });
  }

  function setTheme(theme) {
    const next = theme === "dark" ? "dark" : "light";
    document.documentElement.setAttribute("data-theme", next);
    try {
      localStorage.setItem(THEME_KEY, next);
    } catch {
      /* ignore */
    }
    syncThemeToggleUi(next);
  }

  function toggleTheme() {
    setTheme(getTheme() === "dark" ? "light" : "dark");
  }

  window.InventarioUi = {
    showLoading(withOverlay = false) {
      loadingBar?.classList.add("is-active");
      if (withOverlay) {
        loadingOverlay?.classList.add("is-active");
      }
      if (stillProcessingTimer) clearTimeout(stillProcessingTimer);
      stillProcessingTimer = setTimeout(() => {
        notify(
          "info",
          uiMessage("data-msg-still-processing", "Sigue procesando…")
        );
      }, 10000);
    },
    hideLoading() {
      loadingBar?.classList.remove("is-active");
      loadingOverlay?.classList.remove("is-active");
      if (stillProcessingTimer) {
        clearTimeout(stillProcessingTimer);
        stillProcessingTimer = null;
      }
    },
    /** @deprecated Preferir InventarioUi.confirm(options). */
    openConfirm(options) {
      void confirm(options || {});
    },
    closeConfirm() {
      settleConfirm(false);
    },
    confirm,
    notify,
    showAlert: notify,
    openModal,
    closeModal,
    openNav,
    closeNav,
    toggleNav,
    setNavCollapsed,
    toggleNavCollapsed,
    getTheme,
    setTheme,
    toggleTheme
  };

  document.querySelectorAll("[data-theme-toggle]").forEach((btn) => {
    btn.addEventListener("click", (e) => {
      e.preventDefault();
      toggleTheme();
    });
  });

  syncThemeToggleUi(getTheme());

  navToggle?.addEventListener("click", onNavToggleClick);

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
    if (!isDrawerOpen()) return;
    if (document.querySelector("dialog.action-modal[open], dialog.confirm-dialog[open]")) {
      return;
    }
    e.preventDefault();
    closeNav();
  });

  if (typeof NAV_MQ.addEventListener === "function") {
    NAV_MQ.addEventListener("change", onNavViewportChange);
  } else if (typeof NAV_MQ.addListener === "function") {
    NAV_MQ.addListener(onNavViewportChange);
  }

  // Estado inicial: narrow limpia icon-rail; desktop aplica storage (boot ya pudo setear clase).
  if (isNarrowNav()) {
    document.documentElement.classList.remove("nav-collapsed");
    closeNav();
  } else {
    document.body.classList.remove("nav-open");
    setNavCollapsed(readStoredNavCollapsed());
  }
  syncNavToggleUi();

  document.addEventListener("click", (e) => {
    const target = e.target;
    if (!(target instanceof Element)) return;

    const dismissEl = target.closest("[data-alert-dismiss]");
    if (dismissEl) {
      e.preventDefault();
      const alertEl = dismissEl.closest("[data-alert]");
      if (alertEl instanceof HTMLElement) dismissAlert(alertEl);
      return;
    }

    const confirmOpen = target.closest("[data-confirm-open]");
    if (confirmOpen) {
      e.preventDefault();
      void runConfirmFromTrigger(confirmOpen);
      return;
    }

    const confirmCancel = target.closest("[data-confirm-cancel]");
    if (confirmCancel) {
      e.preventDefault();
      settleConfirm(false);
      return;
    }

    const confirmAccept = target.closest("[data-confirm-accept]");
    if (confirmAccept) {
      e.preventDefault();
      settleConfirm(true);
      return;
    }

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

  document.addEventListener("close", (e) => {
    const dialog = e.target;
    if (!(dialog instanceof HTMLDialogElement)) return;
    if (dialog.id !== "app-confirm-dialog") return;
    if (confirmResolver) {
      const resolve = confirmResolver;
      confirmResolver = null;
      resetConfirmUi();
      resolve(false);
    }
  });

  wireServerAlerts();

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
