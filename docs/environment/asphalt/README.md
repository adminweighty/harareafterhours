# Photographic tar road surface

Replaced the generated road albedo with Poly Haven's photographed Asphalt 03:
https://polyhaven.com/a/asphalt_03

Photography: Charlotte Baglioni; processing: Dario Barresi.
CC0 licence: https://polyhaven.com/license

Uses unmodified 1K colour, OpenGL normal and roughness maps at 2.05 metres
per repeat, with subtle normal strength (0.45). Roughness controls the PBR
reflection response; there is no displacement geometry or extra collision mesh.
Maps use mipmaps, repeat wrapping, trilinear filtering, 4x anisotropy and mobile
ASTC 6x6 compression. The existing shared Asphalt material is retained.

Unity sources are in `unity/unity_game/Assets/Environment/Textures/PhotographicAsphalt`.
Run `HarareAfterHours.EditorTools.GroundSurfaceInstaller.Install` to reproduce
the material setup. Other surfaces still use the existing reference-led atlas.
Road geometry, markings, collisions, vehicles and gameplay scripts are unchanged.

Validation: photographic maps, scale, shader compilation, ground support,
walking and jumping passed; four cars and 23 pedestrians retained. See
`validation.txt` and `road-preview.png`. Editor timing is not a phone benchmark.
This is a real asphalt surface, not a location-specific photograph of Harare.

iOS simulator export, native UnityFramework compilation and Flutter launch
succeeded. Visually checked the textured roads in the running iPhone 16 Pro
simulator. App left running with debugger detached. No physical-device GPU
benchmark was performed.
