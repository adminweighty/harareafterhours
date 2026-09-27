# Straight navigation validation

Forward walks along the character's facing direction; back reverses along the same line. Left/right turn the character and camera in place. Holding forward while turning steers a curve. Sprint remains a held action and backwards movement is slower. Sliding one finger between arrows changes direction without requiring a lift. Hip fire preserves the player's heading.

Unity Editor validation passed for unarmed, pistol and rifle movement, stationary turning, camera follow, no sideways drift while steering, aiming steering, touch direction sliding and sprint release. Shooting regression checks passed at 2, 5 and 50 metres, weapon range limits, wall obstruction, aim assistance and tracer arrival. The fixture backs up and restores real Editor preferences/saves.

Rendering checks passed for 49 characters and 147 skinned meshes. The attached Editor captures confirm readable WALK/BACK/TURN labels and retained combat controls. An unrelated airborne ambient vehicle remains visible in the scene; these changes do not resolve that issue. Phone installation does not establish on-device visual or performance QA.

Guidance consulted: [Apple game controls](https://developer.apple.com/design/human-interface-guidelines/game-controls) for predictable touch controls; [Unity CharacterController.Move](https://docs.unity3d.com/ja/current/ScriptReference/CharacterController.Move.html) for collision-constrained displacement. The straight-walk mapping is a response to the user's requested controls.

Deployment: Unity iOS release export and native framework build succeeded (logs: `/private/tmp/harare-ios-device.IUs5Sp`). Flutter release Xcode build succeeded. Apple devicectl successfully installed the new Runner.app on Udean’s iPhone (bundle `com.udeanmbano.harareAfterHours`). Launch was blocked by the device being locked (FBSOpenApplicationErrorDomain 7); the user must unlock and open the installed app. On-device gameplay has not been visually verified.
