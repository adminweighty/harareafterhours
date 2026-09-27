# First Street realism pass

Scope: improve the existing First Street/Bata corner rather than replace the
city. Primary visual references are supplied images 10 and 11 (First Street),
with image 12 (Jason Moyo) for daytime colour and atmospheric context.

## Changes

- Existing Bata/reference building retained: same footprint, floor count,
  arcade, windows, entrances, signs and collision layout.
- First Street wall/cladding materials now use photographed colour, normal
  and roughness maps at a two-metre repeat. This approximates the pale, jointed
  façade finish; it is not a scan of this Harare building.
- Glass is darker and reflective; one 128px local probe captures the street
  once, spread across frames, rather than continuously rendering six views.
- Natural afternoon sun, a procedural atmospheric sky, neutral ambient fill
  and lighter distance haze replace the purple presentation. This pass uses
  the daytime reference, not the wet night scene. It does not add a day/night cycle.
- Photographic tar retained, with a cooler/darker material tint.
- Street/shop lettering now uses depth-tested rendering; the First Street
  board has separate correctly oriented text on both sides, rather than
  backwards text showing through its back.
- Two canopy/vendor tables, kerb drain slots and two shade trees added.
  New vendor module is a reusable prefab with combined per-material meshes,
  a distance LOD and one simple collider. Three trees now use a new lightweight
  three-LOD jacaranda prefab with textured bark and alpha-cutout leaf cards,
  replacing the solid rounded prototype canopies. The original tree is retained
  inactive for rollback.

## Assets

Unity: `Assets/Environment/ReferenceRealism/FirstStreetVendor.prefab`, five
shared prop materials and five combined meshes; three 1K wall maps in
`Assets/Environment/Textures/PhotographicWall/`; resource sky material
`HarareEnvironment/HarareAfternoonSky.mat`.
Also added: `JacarandaMobile.prefab`, six tree meshes, two tree materials, three
foliage/bark maps, and an alpha-tested foliage shader with matching shadows.
The existing `FirstStreetCorner.prefab` gains a labelled detail root.
Run `HarareAfterHours.EditorTools.ReferenceCityRealismInstaller.Install` after
the older First Street/Ground installers to reproduce this pass.

## Sources and constraints

[Concrete Wall 004, Poly Haven](https://polyhaven.com/a/concrete_wall_004),
photography Charlotte Baglioni, processing Dario Barresi,
[CC0 licence](https://polyhaven.com/license). Unmodified official 1K JPG colour,
OpenGL normal and roughness maps. Mobile import: ASTC 6x6, mipmaps, trilinear,
anisotropy 4. No displacement geometry added.

[Unity lighting guidance](https://docs.unity.com/en-us/engine/6000.0/manual/lighting-overview/lighting)
and [reflection performance guidance](https://docs.unity3d.com/6000.0/Documentation/Manual/RefProbePerformance.html)
informed the lighting/reflection setup. No expensive screen-space reflections
or new per-frame reflection captures were added.

[Jacaranda texture source](https://polyhaven.com/a/jacaranda_tree): Rico Cilliers,
with guidance by Rob Tuytel, CC0. Only the original leaf colour/alpha and bark
colour maps are shipped. The downloadable source geometry was too heavy for
this mobile scene; the game's tree geometry is newly authored, not that model
or a photogrammetric tree. This is an inferred locally appropriate silhouette,
not identification of the exact trees in the low-resolution reference.

Tree LOD triangle budgets: 1,320 / 880 / 650 per instance. Three instances share
the same meshes and materials. Cutout leaves cast alpha-tested shadows; they
are not transparent blended billboards or solid rounded canopy blobs.

## Verification

Ground/material tests pass, including sky availability, one 128px reflection
probe, clear walking-lane samples, ground collision, walking, jumping and
landing. Final editor frame sample: median 16.69 ms, p95 17.20 ms; this is not a
physical-phone performance benchmark. Before/after images and results are
in `docs/environment/realism/`.

Ground and encounter regressions passed. The environment was visually checked
in the simulator before the final sign correction. Both final Unity export and
native/Flutter builds succeeded. The final Flutter launch then reported "Lost
connection to device"; the follow-up visual inspection was blocked because the
Mac was locked. Final live confirmation therefore requires unlocking the Mac.
Removed approximately 2.6 GB of regenerable build/download staging; the prior
framework backup and reference/validation evidence remain available.

The supplied panoramas are only 836×188 pixels. They cannot establish exact
building depth, unseen elevations, shop interiors or material scans. Vendor
and tree positions are inferred to maintain the existing game layout and
walking lanes; no road or mission was relocated. Background city blocks are
not rebuilt in this pass. Further landmark modelling needs larger multi-angle
photographs and potentially Blender work; this is not full-city photorealism.
