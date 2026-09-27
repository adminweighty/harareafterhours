# Walking controls and shooting reliability

Changes:
- Direction input walks at 1.8 m/s (previously 3.3 m/s). Explicit sprint is 4.5 m/s. Aiming moves at 1.5 m/s. Faster deceleration gives a controlled stop.
- Sprint is a held touch action, supports the D-pad and no longer latches. Releasing it returns to walking. Legacy automatic analog sprint is disabled while preserving saved control positions; the existing optional setting remains available.
- Normal third-person framing looks farther down the street, while retaining the complete player in frame.
- Visible live hostile targets close to the reticle can receive aim assistance (10 degrees while firing without aim mode; 3 degrees when aiming). Direct enemy hits retain priority. Both the camera and muzzle paths must reach that enemy, so walls and cars continue to block shots. No assistance for officers, defeated targets, out-of-range targets or targets outside the cone.
- Existing pistol/rifle ranges remain 100/250 m. The visual tracer previously expired after 0.14 s at 300 m/s (about 42 m); it now lasts through its full travel time plus a short final interval.

Validation:
- Graphical Unity run passed all four cardinal movement directions with pistol/rifle, camera follow and straight held movement.
- Touch input: direction alone walks, sprint held with D-pad runs, releasing sprint returns to walking, input release stops within 0.25 m.
- Actual StreetActor damage via TryAttack at 2, 5, 50, 98 m (pistol) and 248 m (rifle). Beyond-range and wall-blocked shots do not damage the enemy.
- Default-camera firing hits the visible hostile at 50 m with both guns; outside-cone targets are not selected.
- Tracer lifetime reaches the impact time. Sampling at travel completion confirms the rendered line endpoint reaches the actual distant hit point.
- 49 characters / 147 skinned meshes retain valid rendering state. Normal gameplay capture visually reviewed.
- Tests preserve local preference and mission/progression values; no phone saves are accessed. Screenshots are Unity Editor captures. Native frame rate and phone visual appearance were not measured.

References checked:
- Unity CharacterController.Move: https://docs.unity3d.com/ja/current/ScriptReference/CharacterController.Move.html
- Unity raycast query ordering: https://docs.unity3d.com/cn/6000.0/ScriptReference/Physics.html
- Unity ray origin/collider behaviour: https://docs.unity3d.com/es/2020.2/ScriptReference/Physics.Raycast.html

Deployment: Unity export and native framework compiled successfully. Framework copying initially ran out of disk space; regenerable DerivedData/framework output from three earlier builds was removed, preserving source and backup exports. Copy then completed and binary comparison passed. CocoaPods and Flutter release build succeeded, and the update installed/launched on the connected iPhone. Build logs: `/private/tmp/harare-ios-device.KgHWoB/`.
