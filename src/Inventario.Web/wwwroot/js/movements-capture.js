/**
 * Captura de movimientos (F2): pills de tipo, Tom Select producto, submit AJAX.
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

  function syncTypePills(form) {
    const select = form.querySelector("[data-movement-type]");
    if (!(select instanceof HTMLSelectElement)) return;
    const value = select.value;
    form.querySelectorAll("[data-type-pill]").forEach((btn) => {
      if (!(btn instanceof HTMLElement)) return;
      const code = btn.getAttribute("data-type-pill") || "";
      const on = code === value;
      btn.classList.toggle("is-selected", on);
      btn.setAttribute("aria-pressed", on ? "true" : "false");
    });
  }

  function wireTypePills(form) {
    const select = form.querySelector("[data-movement-type]");
    if (!(select instanceof HTMLSelectElement)) return;

    form.querySelectorAll("[data-type-pill]").forEach((btn) => {
      btn.addEventListener("click", () => {
        const code = btn.getAttribute("data-type-pill");
        if (!code) return;
        select.value = code;
        select.dispatchEvent(new Event("change", { bubbles: true }));
        syncTypePills(form);
        syncHints(form);
      });
    });

    select.addEventListener("change", () => {
      syncTypePills(form);
      syncHints(form);
    });

    syncTypePills(form);
  }

  function syncHints(form) {
    const type =
      form.querySelector("[data-movement-type]")?.value || "";
    const qtyHint = form.querySelector("[data-quantity-hint]");
    const reasonHint = form.querySelector("[data-reason-hint]");
    const reasonInput = form.querySelector("[data-movement-reason]");
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
    if (reasonInput instanceof HTMLInputElement) {
      reasonInput.placeholder = isAjuste
        ? "Motivo del ajuste (obligatorio)"
        : "Opcional en entrada/salida";
    }
  }

  function updateStockHint(form) {
    const hint = form.querySelector("[data-movement-stock]");
    const select = form.querySelector("[data-movement-product]");
    if (!(hint instanceof HTMLElement) || !(select instanceof HTMLSelectElement)) return;
    const ts = select.tomselect;
    const value = ts ? ts.getValue() : select.value;
    const valueEl = hint.querySelector(".stock-chip__value") || hint.querySelector("strong");
    if (!value) {
      hint.hidden = true;
      return;
    }
    const opt = ts?.options?.[value];
    const stock = opt?.stock;
    if (stock == null) {
      hint.hidden = true;
      return;
    }
    if (valueEl) valueEl.textContent = String(stock);
    hint.hidden = false;
  }

  function resetCaptureFields(form, keepType) {
    const typeValue = form.querySelector("[data-movement-type]")?.value;

    const product = form.querySelector("[data-movement-product]");
    if (product?.tomselect) {
      product.tomselect.clear(true);
    } else if (product instanceof HTMLSelectElement) {
      product.selectedIndex = -1;
    }

    const qty = form.querySelector("[data-movement-quantity]");
    if (qty instanceof HTMLInputElement) {
      qty.value = "";
    }

    const reason = form.querySelector("[data-movement-reason]");
    if (reason instanceof HTMLInputElement) {
      reason.value = "";
    }

    if (keepType && typeValue) {
      const typeSelect = form.querySelector("[data-movement-type]");
      if (typeSelect instanceof HTMLSelectElement) {
        typeSelect.value = typeValue;
      }
    }

    syncTypePills(form);
    updateStockHint(form);
    syncHints(form);

    const focusEl =
      form.querySelector("[data-movement-product]")?.tomselect?.control_input ||
      form.querySelector("[data-movement-product]") ||
      form.querySelector("[data-type-pill].is-selected");
    if (focusEl instanceof HTMLElement) {
      focusEl.focus();
    }
  }

  function wireForm(form) {
    wireTypePills(form);
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
