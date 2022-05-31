mergeInto(LibraryManager.library, {
  unityScreenShareStarted: function () {
    window.dispatchReactUnityEvent(
      "unityScreenShareStarted"
    );
  },
  unityMultiplayerStarted: function () {
    window.dispatchReactUnityEvent(
      "unityMultiplayerStarted"
    );
  },
  unityScreenShareWSConnected: function () {
    window.dispatchReactUnityEvent(
      "unityScreenShareWSConnected"
    );
  },
  unityPresentationNextSlide: function () {
    window.dispatchReactUnityEvent(
      "unityPresentationNextSlide"
    );
  },
  unityPresentationPreviousSlide: function () {
    window.dispatchReactUnityEvent(
      "unityPresentationPreviousSlide"
    );
  },
  unityPresentationSendCursor: function (xPercent, yPercent ) {
    window.dispatchReactUnityEvent(
      "unityPresentationSendCursor",
      xPercent,
      yPercent
    );
  }
});