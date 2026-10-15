/**
 * Captura de movimientos (F2): Tom Select tipo/producto, submit AJAX, conserva tipo.
 */
(() => {
  function parseUnobtrusive(form) {
    if (!form || !window.jQuery) return;
    const $ = window.jQuery;
    if ($.validator && $.validator.unobtrusive) {
      $(form).removeData("validator");
      $(form).removeData("unobtrusiveValidation");
      $.validator.unobtrusive.parse(form);
    }
  }

  function initTypeSelect(form) {
    const select = form.querySelector("[data-movement-type]");
    if (!(select instanceof HTMLSelectElement) || typeof TomSelect === "undefined") return null;
    if (select.tomselect) return select.tomselect;

    const ts = new TomSelect(select, {
      maxItems: 1,
      allowEmptyOption: false,
      plugins: [],
      onChange() {
        syncHints(form);
      }
    });
    select.tomselect = ts;
    return ts;
  }

  function syncHints(form) {
    const type =
      form.querySelector("[data-movement-type]")?.tomselect?.getValue() ||
      form.querySelector("[data-movement-type]")?.value ||
      "";
    const qtyHint = form.querySelector("[data-quantity-hint]");
    const reasonHint = form.querySelector("[data-reason-hint]");
    const isAjuste = String(type).toUpperCase() === "AJUSTE";
    if (qtyHint) {
      qtyHint.textContent = isAjuste
        ? "Variación entera distinta de 0 (puede ser negativa, p. ej. -3)."
        : "Entero mayor que 0.";
    }
    if (reasonHint) {
      reasonHint.textContent = isAjuste
        ? "Obligatorio en ajuste."
        : "Opcional en entrada y salida.";
    }
  }

  function updateStockHint(form) {
    const hint = form.querySelector("[data-movement-stock]");
    const select = form.querySelector("[data-movement-product]");
    if (!(hint instanceof HTMLElement) || !(select instanceof HTMLSelectElement)) return;
    const ts = select.tomselect;
    const value = ts ? ts.getValue() : select.value;
    if (!value) {
      hint.hidden = true;
      return;
    }
    const opt = ts?.options?.[value];
    const stock = opt?.stock;
    const strong = hint.querySelector("strong");
    if (stock == null) {
      hint.hidden = true;
      return;
    }
    if (strong) strong.textContent = String(stock);
    hint.hidden = false;
  }

  function resetCaptureFields(form, keepType) {
    const typeValue =
      form.querySelector("[data-movement-type]")?.tomselect?.getValue() ||
      form.querySelector("[data-movement-type]")?.value;

    const product = form.querySelector("[data-movement-product]");
    if (product?.tomselect) {
      product.tomselect.clear(true);
    } else if (product) {
      product.value = "";
    }

    const qty = form.querySelector("[data-movement-quantity]");
    if (qty instanceof HTMLInputElement) qty.value = "";

    const reason = form.querySelector("[data-movement-reason]");
    if (reason instanceof HTMLInputElement) reason.value = "";

    form.querySelectorAll(".form-error").forEach((el) => {
      el.textContent = "";
    });
    form.querySelectorAll(".input-validation-error").forEach((el) => {
      el.classList.remove("input-validation-error");
    });
    const summary = form.querySelector(".validation-summary-errors");
    if (summary) summary.innerHTML = "";

    if (keepType && typeValue) {
      const typeSelect = form.querySelector("[data-movement-type]");
      if (typeSelect?.tomselect) {
        typeSelect.tomselect.setValue(typeValue, true);
      } else if (typeSelect) {
        typeSelect.value = typeValue;
      }
    }

    updateStockHint(form);
    syncHints(form);

    const focusEl =
      form.querySelector("[data-movement-product]")?.tomselect?.control_input ||
      form.querySelector("[data-movement-product]") ||
      form.querySelector("[data-modal-initial-focus]");
    if (focusEl instanceof HTMLElement) {
      focusEl.focus();
    }
  }

  function wireForm(form) {
    initTypeSelect(form);
    if (window.InventarioUi?.filters?.initCatalogLookups) {
      window.InventarioUi.filters.initCatalogLookups(form, () => updateStockHint(form));
    }
    syncHints(form);
    updateStockHint(form);

    form.addEventListener("submit", async (e) => {
      e.preventDefault();
      const ui = window.InventarioUi;
      ui?.showLoading?.(true);
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
            ui?.notify?.(data.notify.type || "info", data.notify.message);
          }
          if (data && typeof data.redirectUrl === "string" && data.redirectUrl) {
            window.location.assign(data.redirectUrl);
            return;
          }
          if (response.ok) {
            resetCaptureFields(form, data?.keepType !== false);
          }
          return;
        }

        if (contentType.includes("text/html")) {
          const html = await response.text();
          const host = form.closest("[data-movement-page]") || form.parentElement;
          if (host) {
            host.innerHTML = html;
            const newForm = host.querySelector("[data-movement-form]");
            if (newForm instanceof HTMLFormElement) {
              parseUnobtrusive(newForm);
              wireForm(newForm);
            }
          }
          return;
        }

        ui?.notify?.(
          "error",
          "No se pudo registrar el movimiento. Intenta de nuevo."
        );
      } catch {
        ui?.notify?.(
          "error",
          "No hay conexión con el servidor. Comprueba la red e inténtalo de nuevo."
        );
      } finally {
        ui?.hideLoading?.();
      }
    });
  }

  document.addEventListener("DOMContentLoaded", () => {
    const form = document.querySelector("[data-movement-form]");
    if (form instanceof HTMLFormElement) {
      parseUnobtrusive(form);
      wireForm(form);
    }
  });
})();
