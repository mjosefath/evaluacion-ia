(() => {
  const loadingBar = document.getElementById("app-loading-bar");
  const loadingOverlay = document.getElementById("app-loading-overlay");

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
    }
  };

  document.querySelectorAll("[data-confirm-open]").forEach((el) => {
    el.addEventListener("click", (e) => {
      e.preventDefault();
      window.InventarioUi.openConfirm();
    });
  });
})();
