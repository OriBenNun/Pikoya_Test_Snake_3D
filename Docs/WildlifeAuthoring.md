# Wildlife authoring

Wildlife uses the same Blender → FBX → Unity workflow as the garden props.
The editable source is `Tools/Blender/Wildlife.blend`, with a collection for
Bird, Bunny, Turtle, Butterfly and Ladybug. Each collection exports a complete
character to `Assets/Art/Models/Wildlife/<Species>.fbx`.

## Editing and export

Edit the meshes in Blender and save the source. Keep mesh/object names and the
`rig_id` custom properties stable: Unity's existing scene rigs reference those
imported meshes. Re-export with:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' -b --python Tools/Blender/wildlife_models.py
```

Unity imports the updated geometry automatically. To initialize or restore the
import configuration and shared material remaps, outside Play Mode run:

```powershell
unity command --project-path . eval_file --file Tools/Harness/import_wildlife_models.cs --timeout 30000 --json
```

The export includes `Rigs.json`, which records part identities, original
hierarchy names, material names, shadow settings and motion bindings. Blender
keeps the editable parenting. FBX contains the visible parts at their assembled
rest transforms; the existing Unity hierarchy supplies motion pivots. This avoids
the FBX axis-baking issues encountered with nested empty transforms.

Mesh edits update through the imported references. Intentional changes to pivot
placement or character layout also need matching changes to the Unity rig.
The migration does not replace the species animation components or their settings.

## Verification — September 15, 2026

- Replaced 270 scene mesh references: 269 animal parts and the bird perch leaf.
  All five species, including their former primitive eyes, feet and wings, now
  reference FBX geometry.
- Compared 101 distinct part pairs before replacement: triangle counts, bounds,
  vertex positions and normals matched. Imported complete-character transforms
  matched their original rest assemblies, with maximum matrix error `1.2e-7`.
- The scene diff contains exactly 270 mesh-reference replacements. Existing
  transforms, material assignments, motion components and tuning were preserved.
- Inspected front/rear Play Mode captures of all five species.
- A 40-second live check observed 14 animals and 98 animated transforms, including
  bird/butterfly flight, bunny hopping/grazing and turtle/ladybug crawling.
  It recorded zero runtime errors.
- Verified start, apple pickup and pause through the existing primary/pause
  control handlers. The older unattended keyboard playtest remained on Ready
  in this Editor session and was not counted as a passing gameplay check.
- Removed the 17 unreferenced native mesh assets, the temporary axis probe,
  the old buffer generator and its separate detail source. A final Play Mode
  check found all 269 animal parts valid after removal, and the saved scene
  has no legacy wildlife mesh dependencies.
- A fresh-scene reload was skipped because the Editor scene was dirty at that
  point; unsaved scene state was preserved. No unit tests were added or run.

Local evidence is under `Artifacts/wildlife-fbx/` and
`Artifacts/model-sweep/fbx-after/`. Those generated files are ignored by Git.

For a repeatable live check, enter Play Mode and execute
`Tools/Harness/verify_wildlife_fbx.cs`. `Tools/Harness/model_detail_capture.cs`
provides visual captures. The original migration helper,
`Tools/Harness/migrate_wildlife_fbx.cs`, defaults to verification only in a new
Editor session; it is not needed for normal Blender edits.
