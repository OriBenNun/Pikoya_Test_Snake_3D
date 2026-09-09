# Garden Snake tuning

Open `Assets/GardenSnake/Scenes/GardenSnake.unity`. Gameplay and presentation tuning lives in serialized Inspector fields; current defaults preserve the existing look and timing.

| Select | Controls |
| --- | --- |
| `Snake Game` / `SnakeController` | Pace, opening delay, starting length, input buffer, digestion speed, proportions, slither, growth, blink, death, apple motion, camera framing, ring/trail, event wave patterns and intensities, sound pitch/volume |
| `Assets/GardenSnake/Tuning/Snake Skin.asset` | Body shape, curve tension, belly, markings, digestion bulge size/length, mesh quality and optional material overrides |
| `Assets/GardenSnake/Tuning/Snake Mouth.asset` | Anticipation range, jaw speed, swallow duration/spin/shrink, face lift and mouth/jaw/tongue geometry |
| Object with `GardenWind` | Sway amplitude/frequency/direction, turbulence, spatial variation, tree/bush weights, gusts and feedback propagation/decay |
| Board / `GridCellWaves` | Four presets, height/depth/strength limits, concurrent waves, ripple modulation, checker contrast, bloom falloff and sweep direction |
| Each wildlife object / `GardenAnimal` | Species, routes/perch, travel/rest variation, reactions, hops, wings, body/head/limb/ear animation |
| `SnakeFeel` and its linked `MMF_Player` objects | Beat intensities and individual Feel effects |
| Canvas / `SnakeHud` | Card copy/layout, toast/banner behavior, colors, fading, result timing and score thresholds |
| `SpeedGauge` | Needle spring/damping, pace bands/captions, colors, face geometry and mesh quality |
| Existing cameras, lights, volumes, materials, prefabs, AudioSources and ParticleSystems | Their normal Unity Inspector settings |

Use the Hierarchy search `t:GardenWind`, `t:GridCellWaves`, etc. to locate components. The shared skin/mouth assets are assigned under **Shared snake tuning** on `Snake Game`; duplicate an asset and assign the copy to create another preset. Runtime-created skin/mouth objects read those references instead of hiding their only editable settings in Play Mode.

Most animation values update while playing. Wave presets affect newly triggered waves; wildlife duration ranges apply when the next activity starts. Rules, plant discovery, wave buffer capacity, mesh quality, material overrides and wildlife random seed/tempo are initialized when Play Mode starts. Restart Play Mode after changing these. Board dimensions and cell references must match the authored board; changing numbers alone does not rebuild its geometry.

Scene component edits made during Play Mode are temporary. To keep them, copy the component values before stopping and paste them back in Edit Mode, then save the scene. ScriptableObject edits change the asset itself; save or revert them deliberately. Setting wind **Amplitude** and **Pulse Amplitude** to zero removes all wind motion; disabling `GardenWind` restores plant rotations. Setting a wave preset's amplitude or an event's wave intensity to zero suppresses that lift.

`GardenTuning.Bind` reuses existing assets and preserves assigned overrides when the explicit scene rebuild tool is used. Routine tuning requires no rebuild.

## Play Mode verification

The harness requires a connected Unity Editor. No unit tests are involved.

```powershell
python Tools/Harness/gs.py compile
python Tools/Harness/gs.py play
unity command eval_file --file Tools/Harness/tuning_play.cs --json
```

After the live checks finish, inspect `Artifacts/tuning-verification.txt` and `Artifacts/shots/tuning/`. This script compares zero/strong wind, low/high wave amplitudes and limits, live skin/mouth SO edits, wildlife travel, and unchanged simulation coordinates. It uses temporary SO clones and restores original component values. Stop Play Mode afterward.

Run `polish_waves.cs`, `digestion_play.cs` and `digestion_final_apple.cs` in separate fresh Play Mode sessions for all wave patterns, overlapping digestion/turns/pause/restart, and final-apple victory. Inspect their reports and captures under `Artifacts/`.
