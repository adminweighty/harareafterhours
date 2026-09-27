# Steady turning

The on-foot turn buttons previously applied 100 degrees/second immediately (70 while aiming). A short 100ms tap rotated ten degrees, making fine correction difficult.

The new PlayerSteering integrator starts at 20 degrees/second and ramps to 45 over 0.4 seconds. A 100ms tap turns 2.3125 degrees; holding for one second turns 40 degrees. Aiming uses 55% of that speed. Releasing, pausing, locking player input, sliding or entering a vehicle clears the steering ramp. Reversing direction starts gently immediately, with no residual rotation in the previous direction. Forward/back movement, held sprint and vehicle steering are preserved.

Validation passed in Unity Play Mode, with the existing save/prefs backup-and-restore fixture. New checks cover matching turn distances at 30/60/120 fps, short taps, held-rate cap, immediate release/reversal, deadzone and aiming. Existing checks cover all three weapon states, stationary turning, camera follow, straight forward/back movement, no sideways drift, sprint release, enemy hits at 2/5/50 metres and weapon range/obstruction regression. Character rendering checks also passed.

The speed ramp is integrated over the frame interval instead of sampled once per frame, so different frame rates do not alter the total angle. This is gameplay tuning in response to user feedback, not a turning rate prescribed by the sources.

Sources: [Apple game controls](https://developer.apple.com/design/human-interface-guidelines/game-controls) and [Unity time-based movement](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/vector3/movetowards).

Unity iOS release framework and Flutter release build succeeded. Installed and launched on Udean’s iPhone. Build logs: `/private/tmp/harare-ios-device.EoUgnQ` and `/private/tmp/harare-steady-turn-deploy.log`. On-device feel still needs the player’s assessment.
