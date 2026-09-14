# Garden model sweep — September 14, 2026

The completed sweep covers all 20 game-owned FBX models and the five ambient
wildlife species. The direction is rounded garden toys: a pale butter hero,
bright fruit, soft foliage, lilac flowers, glazed clay and aqua tools.
External package art was excluded.

## Retained first pass

The earlier agent's model and material work is retained: cupped flowers and
curved leaves, fuller tools, softened rock and foliage silhouettes, clearer
body markings, seated tree fruit, expressive brows and paired eye glints.
Shared materials distinguish glossy apples and eyes from satin skin, foliage,
wood and clay. Those assets were already committed before this continuation.

## Completed continuation

- Smoothed the head, muzzle, eye whites, pupils, cheeks and glints in Blender.
  The head increases from 25,536 to 42,432 triangles; this is one hero instance.
  Object names, mesh names, parents, material slots and all animation pivots
  remain unchanged.
- Replaced the ladybug's floating button spots and protruding seam with one
  surface-conforming markings mesh. A smooth shell and small eyes give it the
  same toy character as the surrounding wildlife. Existing five movement rigs
  remain in place. Painted markings do not cast a duplicate shell shadow.
- Rebuilt the six turtle scutes around a consistent circular edge, reducing
  the scalloped rim while retaining separate shell colors and narrow seams.
- Made wildlife scene rebuilds retain the existing Blender meshes and authored
  materials. The generator still provides initial meshes when assets are absent.
- Updated art tools for the reorganized `Assets` paths and `GameLoopManager`.
  Inspection captures now use a nearby isolated layer. The former 10,000-unit
  staging location introduced precision artifacts on tiny details.

## Verification

Unity 6000.6.0f1 Play Mode was used throughout. No unit tests were added or run.

- Inspected all 20 imported models from front and rear, plus front/rear views of
  bunny, turtle, bird, butterfly and ladybug and animated hero closeups.
- All 141 imported FBX mesh references remain valid. No missing meshes,
  materials or unsupported shaders were found in the live game.
- Keyboard-driven checks passed Ready/start, anticipation, excited eyes,
  pause, frozen facial pose, resume, pickup/swallow, mouth and eye recovery,
  wall collision, results and restart with score/length reset.
- Gameplay capture runs collected up to 11 apples. The final detail loop
  included a collision/restart and reported zero runtime errors.
- A rebuild integrity check called `GardenWildlifeMeshes.Prepare()` and found
  all 17 authored wildlife mesh files byte-for-byte unchanged.
- The scene was saved by Unity; no scene or native mesh YAML was hand-edited.
  Unity also normalized serialized TMP/cache fields during the scene save.

Local evidence (generated, ignored by Git):

- `Artifacts/model-sweep/final-gallery/front-sheet.png` and `back-sheet.png`
- `Artifacts/model-sweep/resume-final/` — wildlife and animated head views
- `Artifacts/model-sweep/final-mouth-controls.txt` — nine passing control stages
- `Artifacts/model-sweep/final-mouth-check-*.png` — full game control captures
- `Artifacts/model-sweep/final-preview.png` — compact art preview

## Current authoring workflow

Wildlife moved to five complete FBX models on September 15. See
[Wildlife authoring](WildlifeAuthoring.md) for the Blender export and Unity import
workflow. The native mesh buffer workflow described by this historical pass has
been retired; its scripts and intermediate sources remain available in Git history.

`Tools/Blender/create_assets.py` still rebuilds the garden prop FBX set and
`GardenSnake.blend`. The model gallery and mouth verification tools remain usable.
