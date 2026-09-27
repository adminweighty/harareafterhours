# Character visibility fix — 27 September 2026

The evening sky remains, while the city receives a stable, explicit ambient probe in `AmbientMode.Custom`. An initial render check caught Unity replacing the probe when the mode remained Trilight. The final run measured a minimum ambient value of 0.401 across six directions.

Changes in the Unity project:
- Neutral-coloured 4800 K main light, stronger cool fill and reduced shadow strength.
- Neutral tone mapping, +0.55 EV exposure and zero added contrast.
- Removed the extra dark multiplier applied to already-dark outfit textures. Skin colours are unchanged.
- The player retains its highest LOD; NPCs retain their last LOD farther away.

Validation:
- Graphical Unity run: 49 characters, 147 skinned meshes, supported materials and no forcibly hidden bodies. Passed.
- Reviewed `gameplay.png` and `character.png` from the running scene: player clothes, street NPCs, vehicles and pavements are readable.
- EveningSceneValidation: default full-body camera framing, 50+ fixtures, capped shadowless nearby street lights and Balanced graphics. Passed.
- Tests preserve and restore local preferences and mission/progression values. They do not access phone saves.

These are Unity Editor captures, not iPhone screenshots. Native iPhone appearance still needs observation after installation.

References checked:
- https://docs.unity3d.com/6000.0/Documentation/ScriptReference/RenderSettings-ambientProbe.html
- https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.AmbientMode.Custom.html
- https://docs.unity3d.com/6000.0/Documentation/Manual/urp/shader-stripping.html

Deployment: Unity iOS release export and native framework build succeeded. Flutter release build installed and launched successfully on the connected iPhone 13 Pro Max. CoreDevice confirmed Runner running (PID 5888). Native build logs: `/private/tmp/harare-ios-device.HN7aUX/`.
