# Animated launch

The Flutter launch overlay now reveals the existing game logo over a warm radial glow, a rising skyline and perspective road lines, with a travelling amber light. The animation lasts 1.8 seconds, then fades away only when Unity has reported readiness. Native loading runs concurrently; the progress bar remains indeterminate rather than inventing completion percentages.

The scenery uses a lightweight CustomPainter, not a video or additional generated bitmap. It is isolated in a RepaintBoundary. Reduced-motion users get a static composition and no animated progress indicator. Relaunching a session replays the animation and rearms the delayed connection-retry message. Layout remains scrollable on compact/landscape screens.

Each session has a distinct launch widget key so a native readiness event arriving before the next Flutter frame cannot skip the splash. Tests cover already-ready startup, early readiness, restart, slow-load retry, portrait/landscape and reduced motion. All 31 Flutter tests pass; static analysis is clean.

Exit behavior remains platform-specific pending the user's choice on local iPhone force termination: Android calls SystemNavigator.pop after ending/saving the session; iOS currently ends the session and shows Game closed. Apple explicitly discourages exit(), and Flutter recommends SystemNavigator.pop over process termination. Neither provides a supported way for this root iPhone app to gracefully terminate itself.

Sources: [Apple QA1561](https://developer.apple.com/library/archive/qa/qa1561/_index.html), [Flutter SystemNavigator.pop](https://api.flutter.dev/flutter/services/SystemNavigator/pop.html).
