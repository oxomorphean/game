mergeInto(LibraryManager.library, {
  LinhThuPresentation: function (mode) {
    window.dispatchEvent(new CustomEvent('linhthu-mode', { detail: UTF8ToString(mode) }));
  }
});
