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

## Reproduce the art and review

`Tools/Blender/create_assets.py` rebuilds the FBX set and `GardenSnake.blend`.
`Tools/Blender/finish_wildlife.py` rebuilds the final ladybug and turtle detail
buffers and saves their editable source in `Tools/Blender/WildlifeDetails.blend`.
The previous wildlife pass remains editable in `Tools/Blender/Wildlife.blend`.

Import the final buffers outside Play Mode by setting the Unity session key
`ModelSweep.MeshSource` to `Artifacts/model-sweep/details-meshes`, then execute
`Tools/Harness/import_wildlife_models.cs` through `unity command eval_file`.
`GardenWildlifeBuilder.PolishLadybugs()` applies the art to existing scene rigs;
run it on the saved GardenSnake scene and save through Unity afterwards.

Use `Tools/Harness/model_import_audit.cs` for import references. In Play Mode,
`Tools/Harness/model_gallery.cs` captures every FBX prefab and
`Tools/Harness/model_detail_capture.cs` captures wildlife and the animated head.
Start from Ready before running `Tools/Harness/model_mouth_verify.cs`.
Supply `--project-path` to Unity commands when multiple projects are open.
