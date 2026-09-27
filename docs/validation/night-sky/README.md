# Black sky with animated meteors

The visible sky now uses a Resources-backed URP shader/material (`Harare/Night Sky`). It draws a black background, sparse subtly twinkling stars, and one tapered meteor during each 18-second window. Its direction and start delay change per window; each meteor lasts 1.2 seconds. Frequency is chosen for game ambience, not an astronomical simulation.

Sky directions are world anchored. Animation uses Unity's scaled game time. Buildings occlude the sky normally. No additional realtime lights, particle objects, textures or per-frame CPU allocations are introduced. Existing character ambient lighting and street lights remain; distance fog is now near-black instead of purple.

Validation: graphical Unity run passed with a supported, error-free shader, 49 characters/147 skinned meshes and minimum six-direction ambient value 0.401. Reviewed the normal gameplay capture and meteor captures at 3.25 and 3.70 seconds, confirming a changed meteor position and tapered trail. Captures are from the Editor, not an iPhone. Test preferences and saves are restored after validation.

Sources:
- Unity skybox setup: https://docs.unity3d.com/6000.0/Documentation/Manual/skyboxes-using.html
- Unity shader time variables: https://docs.unity3d.com/6000.0/Documentation/Manual/SL-UnityShaderVariables.html
- NASA meteor appearance: https://science.nasa.gov/solar-system/meteors-meteorites/

Deployment: Unity compiled and serialized Harare/Night Sky for iOS. Native framework and Flutter release builds succeeded; installation and launch on the connected iPhone completed successfully. Build logs: `/private/tmp/harare-ios-device.sUDy0k/`. On-phone visual appearance has not been captured.
