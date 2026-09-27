# Shared vehicle repair — 23 September 2026

## What changed

All four active city cars now instantiate `Assets/Resources/HarareVehicles/Prefabs/SharedSUV.prefab` in the existing Unity project at `unity/unity_game`. They share geometry, wheel rig, collision setup and materials, with per-instance paint colours. `VehicleAppearance.SetAppearance(Color, Texture2D)` supports body-texture variants without duplicating the base model. Existing legacy car assets were retained.

The base is the supplied Mercedes GLS Blender source in `car-prototype/source/mercedes_gls`, not a newly downloaded vehicle. It is normalized to 5.207 metres long. The export separates four wheels, transparent windows and material categories; welds coincident vertices; preserves UVs; applies simplified geometry and weighted normals. Previously the export and material assignment collapsed nearly every surface into body paint.

Three LODs contain 25,280 / 23,222 / 18,683 triangles. Simulator review caught torn body panels in the first aggressively reduced distant meshes. The corrected LODs preserve the verified near-model shell, simplify wheels and remove distant interior/tiny details. Shared PBR materials distinguish paint, glass, tyres, interior, black trim, metal, wheel metal, headlights and tail lamps. Simplified chassis collision is separate from the wheel suspension. Editable export and measurements are in `/private/tmp/harare-vehicle-repair/export`; the reproducible exporter is `tool/vehicle_repair/export_shared_suv.py`.

## Driving and riding

- Walk close to a stopped car. **USE / E** enters the driving seat; **RIDE / R** enters as a passenger.
- Drive with direction controls / WASD; GO accelerates, down reverses, BRAKE stops. Opposite throttle brakes before reversing. Mobile input supports separate steering and throttle fingers.
- **USE / E** exits only at low speed with a clear safe exit. Brake first when driving. As a passenger, STOP or USE requests an AI stop; press USE again once stopped.
- Ambient cars can be taken over. Passenger rides have a visible AI driver. Driving temporarily suspends that car's autopilot.

WheelCollider suspension, ground friction, torque and braking replace direct velocity pushing. The front wheels steer, all wheels rotate and suspension moves their visual pivots. Steering reduces with speed; braking illuminates the rear lamps.

Characters remain visible, face forward and use Driving_Loop / Sitting_Idle_Loop from the existing CC0 Quaternius animation source. Seat positioning aligns the hips and keeps heads below the roof. Driver hand IK targets the steering-wheel grips. Leaving restores on-foot movement and animation. The supplied cockpit is **left-hand drive**; its seat layout has been preserved to match the steering wheel rather than claiming an unsupported right-hand-drive conversion.

## Verification

`SharedVehicleValidation.Run` passed all checks in [gameplay-validation.txt](gameplay-validation.txt): shared models and LODs; grounded suspension; visible correctly oriented seating; acceleration (13.1 km/h after the test interval); physical wheel rotation; steering and stability; braking/reverse; blocked and moving-exit rejection; passenger rides and stops; ambient-car takeover; delivery completion; 23 pedestrians; no runtime errors.

Review images: [exterior / LOD0](suv-exterior.png), [LOD1](suv-lod1.png), [LOD2](suv-lod2.png), [seated driver](suv-driver.png). All three levels were visually inspected after correcting the over-reduction defect. These are Unity editor review renders, not promises of photographic quality.

The existing [character regression](character-regression.txt) and [city regression](city-regression.txt) also passed. Walking remains 1.80 m/s, sprinting 5.50 m/s, with a tested jump apex of 1.44 m. The city check retained the reference block, 21 streetlights, three moving ambient cars, 23 walking NPCs, combat, progression, audio, UI and mission. Its editor frame sample was 16.69 ms median / 16.71 ms p95, not a device benchmark. All three Flutter tests passed.

The final Unity iOS export, UnityFramework Xcode build and Flutter simulator build succeeded. The rebuilt city was visually checked on the iPhone 16 Pro simulator: the previously torn car panels were intact. The subsequent manual touch-control check was inconclusive because the simulator capture became obscured; driver/passenger interaction is covered by the passing automated gameplay suite, not claimed as manually verified. The app was returned to its mission menu and the debugger detached. Only this task's regenerable DerivedData/framework staging directories were removed afterwards (about 2.4 GiB); the active framework, source exports and previous-framework backup remain.

## Limits and follow-up

The original Blender source refers to an external `model/texture.png` that was not supplied. This repair uses separated PBR surfaces and preserved UVs, **not a newly hand-painted texture set**. Custom livery textures can be supplied through the existing texture override. The original vehicle's commercial-use licence must be confirmed before distribution; the vehicle is not covered by the animation pack's CC0 licence.

The LODs and shared assets reduce cost, but this is not a physical-device GPU or battery benchmark. The lowest LOD still has about 18k triangles: preserving this source's thin disconnected panels is more important than meeting an unsafe reduction ratio. Further manual retopology/atlas work is appropriate if fleet density grows. Traffic remains the existing lightweight route system, not a full traffic simulation; no city layout or backend/database changes were made for this task.

## Technical references

Unity editor entry points (namespace `HarareAfterHours.EditorTools`):

- `SharedVehicleInstaller.Install`: rebuild the shared prefab/materials and install seated clips.
- `SharedVehicleInstaller.Review`: generate the studio review renders.
- `SharedVehicleValidation.Run`: run the new vehicle play-mode checks.
- `SharedCharacterPlayValidation.Run` and `FirstStreetGameplayValidation.Run`: existing regression suites.

Runtime changes are in `Assets/Scripts/VehicleController.cs`, `VehicleAppearance.cs`, `VehicleSeatIK.cs`, `TrafficAndPedestrians.cs`, `ThirdPersonController.cs`, `CharacterRoster.cs`, `GameplayHud.cs`, `MobileControls.cs`, `CombatSystem.cs`, `MissionDirector.cs` and `HarareAfterHoursBootstrap.cs` in the Unity project. The previous active simulator framework and initial runtime backups are retained at `/private/tmp/harare-vehicle-repair/before`.

Implementation follows Unity's [WheelCollider reference](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/wheelcollider), [wheel-collider setup tutorial](https://docs.unity.com/en-us/engine/6000.7/manual/physics-section/physics-overview/collision-section/collider-shapes/wheel-colliders/wheel-collider-tutorial) and [URP Lit material reference](https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/lit-shader). Seated animation source: [Quaternius Universal Animation Library](https://quaternius.com/packs/universalanimationlibrary.html), with the existing licence retained in Unity.
