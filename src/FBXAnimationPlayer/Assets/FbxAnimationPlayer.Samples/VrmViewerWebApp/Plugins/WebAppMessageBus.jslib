mergeInto(LibraryManager.library, {
  /**
   * C# の WebAppMessageBus.Emit() から呼ばれる。
   * 'unity:message' カスタムイベントとして window に発火する。
   * Vue.js 側は window.addEventListener('unity:message', ...) で受信する。
   *
   * event.detail の構造: { type: string, payload: string }
   */
  DispatchEvent: function(typePtr, payloadPtr) {
    var type    = UTF8ToString(typePtr);
    var payload = UTF8ToString(payloadPtr);
    window.dispatchEvent(new CustomEvent('unity:message', {
      detail: { type: type, payload: payload }
    }));
  }
});
