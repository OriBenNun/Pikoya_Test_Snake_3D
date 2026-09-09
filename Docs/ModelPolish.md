# Model polish — September 9, 2026

Blender was updated from 3.5 to **5.2.1 LTS**, the current stable release verified against Blender's release page and the Windows package manifest. The installed executable is `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`.

## Scope and review

All 34 FBX files were inventoried. All 20 Garden Snake models received front and rear renders in Blender and again through their imported Unity prefabs in Play Mode. The other 14 files belong to bundled Feel demos: 12 contain visible geometry and two contain animation rigs without meshes. The 12 demo meshes were also reviewed from two angles; no shape corrections were identified for their intended demo use. Their original files were preserved.

## Individual corrections

| Model | Result |
| --- | --- |
| Apple | Replaced overlapping flesh spheres with a continuous rounded surface and recessed stem seat. |
| SnakeHead | Replaced disconnected smile beads with one surface-following curve; seated brows, teeth, and freckles. |
| SnakeBody | Conformed the dorsal marking to the body surface and softened the belly edge. |
| SnakeTail | Conformed the marking and reduced the separate tip's protrusion. |
| Gnome | Rebuilt the bent hat around a level base seated on the brim. |
| WateringCan | Cut a recessed fill opening with a rolled lip; aligned the rose and its holes with the spout; triangulated the cut for reliable FBX import. |
| FlowerPot | Added an inner wall and recessed soil; smoothed the rim. |
| Spade | Gave the blade a tapered digging point and rounded edges. |
| Tree | Moved three apples out of the canopy surface so they remain visible. |
| Lavender | Staggered the flower spikes to match their branching stems. |
| Bush, Flower, Daisy, Tulip | Smoothed curved silhouettes while preserving their original shapes. |
| Rake | Smoothed round handles and tines while keeping flat end caps. |
| Tile, Planter, RimLong, RimShort | Checked bevels and silhouettes; migrated the export helper to Blender 5.2. |
| Rock | Checked both views; retained the intentional faceted shape. |

The source `.blend`, FBX files, object names, and material slots remain editable. `polish_assets.py` stores the individual corrections, and `create_assets.py` applies them before export so a rebuild retains the work. This is scripted mesh authoring with visual review, not a claim of interactive mouse editing in Blender.

## Verification

- Re-exported all 20 game models with Blender 5.2.1 and explicitly reimported them in Unity 6000.6.0f1.
- Checked imported meshes and material/shader references for all 20 models.
- Inspected 40 Blender views and 40 Unity prefab views, correcting the watering-can opening's FBX triangulation after reviewing the Unity result.
- Used the actual UI button callbacks in Play Mode to verify start, apple pickup, growth from three to four segments, pause, resume, wall collision, and restart.
- Captured and inspected the complete game frame as well as isolated model views. No unit tests were added or run.
- The existing unattended keyboard harness stayed on the Ready screen, so its run was not counted as a successful gameplay check. The UI-driven verification and captured states are recorded separately.

Local evidence is under `Artifacts/model-audit/` and `Artifacts/shots/models-final-*.png`. These generated captures are intentionally ignored by Git.

## Repeat the visual review

With Blender installed, run:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background Tools/Blender/GardenSnake.blend --python Tools/Blender/inspect_assets.py -- review
```

This writes both views of each source model to `Artifacts/model-audit/review` without saving changes to the source scene.

With Unity open in Play Mode, run:

```powershell
python Tools/Harness/gs.py script Tools/Harness/model_audit.cs
```

This captures all 20 imported models from two angles to `Artifacts/model-audit/unity`. The temporary camera and model instances are removed after capture. Exit Play Mode after verification; do not save transient scene changes.
