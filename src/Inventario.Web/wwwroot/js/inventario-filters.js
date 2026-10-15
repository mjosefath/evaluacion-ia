/**
 * Filtros + URL query (PG6–PG7) + lookups Tom Select (PG9–PG10).
 * Depende de: TomSelect (global), InventarioUi (site.js).
 */
(() => {
  const PAGE_SIZES = [20, 50, 100];
  const DEBOUNCE_MS = 320;

  /** @param {string} name @param {string|null|undefined} value */
  function setQueryParam(name, value) {
    const url = new URL(window.location.href);
    if (value === null || value === undefined || String(value).trim() === "") {
      url.searchParams.delete(name);
    } else {
      url.searchParams.set(name, String(value));
    }
    return url;
  }

  /**
   * @param {Record<string, string|number|null|undefined>} patch
   * @param {{ replace?: boolean }} [opts]
   */
  function patchQuery(patch, opts) {
    const url = new URL(window.location.href);
    Object.entries(patch).forEach(([key, value]) => {
      if (value === null || value === undefined || String(value).trim() === "") {
        url.searchParams.delete(key);
      } else {
        url.searchParams.set(key, String(value));
      }
    });
    const next = url.pathname + url.search + url.hash;
    if (opts?.replace) {
      window.history.replaceState({}, "", next);
    } else {
      window.history.pushState({}, "", next);
    }
    return url;
  }

  /** @param {string} name */
  function getQueryParam(name) {
    return new URL(window.location.href).searchParams.get(name);
  }

  function debounce(fn, ms) {
    /** @type {ReturnType<typeof setTimeout> | null} */
    let t = null;
    return (/** @type {any[]} */ ...args) => {
      if (t) clearTimeout(t);
      t = setTimeout(() => fn(...args), ms);
    };
  }

  /**
   * @param {HTMLElement} root
   * @param {{ navigate?: "reload"|"event" }} [options]
   */
  function bindFilters(root, options) {
    const navigate =
      options?.navigate ||
      root.getAttribute("data-list-navigate") ||
      "reload";

    const filterBar = root.querySelector("[data-filter-bar]");
    const paginator = root.querySelector("[data-paginator]");

    function collectFields() {
      return Array.from(
        root.querySelectorAll("[data-filter-field]")
      ).filter((el) => el instanceof HTMLElement);
    }

    function fieldValue(el) {
      if (el instanceof HTMLSelectElement || el instanceof HTMLInputElement) {
        return el.value;
      }
      return el.getAttribute("value") || "";
    }

    function fieldLabel(el) {
      return el.getAttribute("data-filter-label") || el.getAttribute("name") || "";
    }

    function displayValue(el) {
      if (el instanceof HTMLSelectElement) {
        const opt = el.selectedOptions[0];
        if (opt && opt.value) return opt.textContent?.trim() || opt.value;
        // Tom Select
        const ts = el.tomselect;
        if (ts) {
          const v = ts.getValue();
          if (!v) return "";
          const item = ts.options[v];
          return item ? item.text || item.name || String(v) : String(v);
        }
      }
      return fieldValue(el);
    }

    function renderBadges() {
      const host = filterBar?.querySelector("[data-filter-badges]");
      if (!host) return;

      host.innerHTML = "";
      let any = false;

      collectFields().forEach((el) => {
        if (el.getAttribute("data-filter-skip-badge") === "true") return;
        const name = el.getAttribute("data-filter-field");
        if (!name) return;
        const raw = fieldValue(el);
        if (!raw || !String(raw).trim()) return;

        any = true;
        const badge = document.createElement("span");
        badge.className = "filter-badge";
        badge.dataset.filterBadge = name;

        const text = document.createElement("span");
        text.className = "filter-badge__text";
        text.textContent = `${fieldLabel(el)}: ${displayValue(el)}`;

        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "filter-badge__remove";
        btn.setAttribute("aria-label", `Quitar ${fieldLabel(el)}`);
        btn.innerHTML =
          '<svg class="icon" aria-hidden="true" focusable="false"><use href="#lucide-x"></use></svg>';
        btn.addEventListener("click", () => {
          clearField(el);
          applyChange({ resetPage: true });
        });

        badge.append(text, btn);
        host.appendChild(badge);
      });

      host.hidden = !any;
    }

    /** @param {HTMLElement} el */
    function clearField(el) {
      if (el instanceof HTMLSelectElement && el.tomselect) {
        el.tomselect.clear(true);
        return;
      }
      if (el instanceof HTMLInputElement || el instanceof HTMLSelectElement) {
        el.value = "";
      }
    }

    function clearAll() {
      collectFields().forEach((el) => {
        if (el.getAttribute("data-filter-skip-badge") === "true") return;
        if (el.getAttribute("data-filter-field") === "pageSize") return;
        clearField(el);
      });
      applyChange({ resetPage: true, clearAll: true });
    }

    /**
     * @param {{ resetPage?: boolean, clearAll?: boolean, page?: number, pageSize?: number }} [opts]
     */
    function applyChange(opts) {
      /** @type {Record<string, string|number|null|undefined>} */
      const patch = {};

      collectFields().forEach((el) => {
        const name = el.getAttribute("data-filter-field");
        if (!name) return;
        if (opts?.clearAll && name === "pageSize") return;
        const v = fieldValue(el);
        patch[name] = v && String(v).trim() ? v : null;
      });

      if (opts?.resetPage || opts?.clearAll) {
        patch.page = 1;
      }
      if (opts?.page != null) {
        patch.page = opts.page;
      }
      if (opts?.pageSize != null) {
        patch.pageSize = opts.pageSize;
      }

      patchQuery(patch, { replace: false });
      renderBadges();

      root.dispatchEvent(
        new CustomEvent("inventario:filters-changed", {
          bubbles: true,
          detail: { patch, url: window.location.href }
        })
      );

      if (navigate === "reload") {
        window.location.assign(window.location.pathname + window.location.search);
      }
    }

    const debouncedSearch = debounce(() => applyChange({ resetPage: true }), DEBOUNCE_MS);

    filterBar
      ?.querySelector("[data-filter-clear]")
      ?.addEventListener("click", () => clearAll());

    collectFields().forEach((el) => {
      const name = el.getAttribute("data-filter-field");
      if (!name) return;

      // Lookups Tom Select: onChange propio (evita doble navigate).
      if (el.closest("[data-catalog-lookup]")) return;

      if (el instanceof HTMLInputElement && el.type === "search") {
        el.addEventListener("input", () => debouncedSearch());
        el.addEventListener("keydown", (e) => {
          if (e.key === "Enter") {
            e.preventDefault();
            applyChange({ resetPage: true });
          }
        });
        return;
      }

      if (el.getAttribute("data-paginator-size") != null) {
        el.addEventListener("change", () => {
          const size = Number(fieldValue(el));
          applyChange({
            resetPage: true,
            pageSize: PAGE_SIZES.includes(size) ? size : 20
          });
        });
        return;
      }

      el.addEventListener("change", () => applyChange({ resetPage: true }));
    });

    paginator
      ?.querySelector("[data-paginator-prev]")
      ?.addEventListener("click", () => {
        const page = Number(paginator.getAttribute("data-page") || "1");
        if (page > 1) applyChange({ page: page - 1 });
      });

    paginator
      ?.querySelector("[data-paginator-next]")
      ?.addEventListener("click", () => {
        const page = Number(paginator.getAttribute("data-page") || "1");
        const total = Number(paginator.getAttribute("data-total-pages") || "1");
        if (page < total) applyChange({ page: page + 1 });
      });

    paginator?.querySelectorAll("[data-paginator-page]").forEach((btn) => {
      btn.addEventListener("click", () => {
        const p = Number(btn.getAttribute("data-paginator-page") || "1");
        applyChange({ page: p });
      });
    });

    initCatalogLookups(root, () => applyChange({ resetPage: true }));
    renderBadges();

    return { applyChange, renderBadges, clearAll };
  }

  /**
   * @param {HTMLElement} root
   * @param {() => void} onChange
   */
  function initCatalogLookups(root, onChange) {
    if (typeof TomSelect === "undefined") {
      console.warn("TomSelect no cargado; lookups de catálogo omitidos.");
      return;
    }

    root.querySelectorAll("[data-catalog-lookup]").forEach((wrap) => {
      if (!(wrap instanceof HTMLElement)) return;
      const select = wrap.querySelector("[data-catalog-select]");
      if (!(select instanceof HTMLSelectElement) || select.tomselect) return;

      const catalog = wrap.getAttribute("data-catalog") || "categories";
      const activeOnly = wrap.getAttribute("data-active-only") === "true";
      const placeholder =
        select.getAttribute("placeholder") || "Buscar…";

      const baseUrl = `/catalogs/${catalog}`;

      /** @param {string} query @param {number} page */
      function buildUrl(query, page) {
        const u = new URL(baseUrl, window.location.origin);
        u.searchParams.set("page", String(page));
        u.searchParams.set("pageSize", "20");
        if (query) u.searchParams.set("search", query);
        if (activeOnly) u.searchParams.set("activeOnly", "true");
        return u.pathname + u.search;
      }

      let userReady = false;

      const ts = new TomSelect(select, {
        valueField: "id",
        labelField: "text",
        searchField: ["text"],
        preload: "focus",
        maxItems: 1,
        allowEmptyOption: true,
        placeholder,
        plugins: ["virtual_scroll", "clear_button"],
        firstUrl(query) {
          return buildUrl(query, 1);
        },
        load(query, callback) {
          const url = this.getUrl(query);
          fetch(url, { headers: { Accept: "application/json" } })
            .then((r) => {
              if (r.status === 401) {
                window.location.href = "/Account/Login?sessionExpired=true";
                return null;
              }
              if (!r.ok) throw new Error("catalog lookup failed");
              return r.json();
            })
            .then((json) => {
              if (!json) {
                callback();
                return;
              }
              const page = Number(
                new URL(url, window.location.origin).searchParams.get("page") || "1"
              );
              const totalPages = Number(json.totalPages || 0);
              if (page < totalPages) {
                this.setNextUrl(query, buildUrl(query, page + 1));
              }
              const items = (json.items || []).map(mapCatalogItem);
              callback(items);
            })
            .catch(() => callback());
        },
        render: {
          option(data, escape) {
            const inactive =
              data.statusId != null && Number(data.statusId) === 2
                ? ' <span class="catalog-lookup__inactive">(Inactivo)</span>'
                : "";
            return `<div class="catalog-lookup__option">${escape(data.text)}${inactive}</div>`;
          },
          item(data, escape) {
            return `<div>${escape(data.text)}</div>`;
          },
          no_results() {
            return '<div class="no-results">Sin resultados</div>';
          }
        },
        onChange() {
          if (!userReady) return;
          onChange();
        }
      });

      // Evita navigate al hidratar opción preseleccionada / preload.
      queuePromise.resolve().then(() => {
        userReady = true;
      });

      select.tomselect = ts;
    });
  }

  /** @param {any} item */
  function mapCatalogItem(item) {
    const id = item.id;
    let text = item.name || item.code || String(id);
    if (item.code && item.name && item.code !== item.name) {
      text = `${item.code} — ${item.name}`;
    }
    return {
      id: String(id),
      text,
      name: item.name,
      code: item.code,
      statusId: item.statusId,
      statusName: item.statusName
    };
  }

  const api = {
    bind: bindFilters,
    patchQuery,
    setQueryParam,
    getQueryParam,
    initCatalogLookups
  };

  window.InventarioUi = window.InventarioUi || {};
  window.InventarioUi.filters = api;
  window.InventarioUi.urlQuery = {
    patch: patchQuery,
    get: getQueryParam,
    set: setQueryParam
  };
})();
