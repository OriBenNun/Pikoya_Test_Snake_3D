# Garden Snake tuning

Open `Assets/Scenes/GardenSnake.unity`. The `Snake Game` object carries the four layers, and each
one exposes only what belongs to it.

| Select | Controls |
| --- | --- |
| `Snake Game` / `PlayerController` | Swipe threshold in pixels and as a fraction of screen height |
| `Snake Game` / `GameLoopManager` | Board size, pace and speed gain, opening beat, starting length, turn buffer, first apple distance, target frame rate, restart delay |
| `Snake Game` / `SnakeManager` | The two models, the two shared tuning assets, and the head/body/tail proportions |
| `Snake Game` / `FeedbackManager` | Clips and sources, plus three grouped blocks: **Sound** (pitch and level per one-shot), **Feel** (the five MMF players and their beat intensities) and **Waves** (which board wave each moment runs, how far it carries, and the milestone interval) |
| `Apple` / `AppleView` | Apple scale; the marker and prefab references |
| `Camera Rig` / `GameCameraRig` | Edge padding, the two HUD bands, resting zoom and zoom speed |
| `Assets/Tuning/Snake Skin.asset` | Body shape, segment bumps, curve tension, belly, markings, digestion bulge size and length, mesh quality and optional material overrides |
| `Assets/Tuning/Snake Mouth.asset` | Anticipation range, jaw speed, swallow duration/spin/shrink, face lift and mouth/jaw/tongue geometry |
| Board / `GridCellWaves` | Four presets, height/depth/strength limits, concurrent waves, ripple modulation, checker contrast, bloom falloff and sweep direction |
| `Meadow` / `GardenWind` | Sway amplitude, frequency and direction, turbulence, spatial variation, tree and bush weights, gusts, and how a beat travels and decays |
| Each wildlife object / `GardenAnimal` | Species, routes and perch, travel and rest variation, reactions, hops, wings, body/head/limb/ear animation |
| Canvas / `SnakeHud` | Card copy and layout, toast and banner behaviour, colours, fading, result timing and score thresholds |
| `SpeedGauge` | Needle spring and damping, pace bands and captions, colours, face geometry and mesh quality |
| Existing cameras, lights, volumes, materials, prefabs, AudioSources and ParticleSystems | Their normal Unity Inspector settings |

Use the Hierarchy search `t:GardenWind`, `t:GridCellWaves`, etc. to locate components.

## What is no longer a setting

`SnakeManager` used to expose around fifty fields for slither, banking, blinking, idle breathing,
growth and the death animation, and `AppleView`'s motion and `FeedbackManager`'s ring and trail were
the same. Those are the animal's character rather than settings a designer trades off, so they are
now named constants at the top of each class, where they read as one description of how the creature
behaves instead of fifty sliders to keep consistent. Change them in code; the values are unchanged
from what the scene was serialising.

What stayed in the Inspector is what an art director or designer actually reaches for: proportions,
pace, rules, the audio mix, beat intensities, wave choices, and the two shared tuning assets.

## Live versus startup

Most animation values update while playing. Wave presets affect newly triggered waves; wildlife
duration ranges apply when the next activity starts. Rules, plant discovery, wave buffer capacity,
mesh quality, material overrides and wildlife random seed and tempo are read when Play Mode starts —
restart Play Mode after changing these. Board dimensions and cell references must match the authored
board; changing the numbers alone does not rebuild its geometry.

Scene component edits made during Play Mode are temporary. To keep them, copy the component values
before stopping and paste them back in Edit Mode, then save the scene. ScriptableObject edits change
the asset itself; save or revert them deliberately. Setting wind **Amplitude** and **Pulse
Amplitude** to zero removes all wind motion; disabling `GardenWind` restores plant rotations. Setting
a wave preset's amplitude or an event's wave intensity to zero suppresses that lift.

`GardenTuning.Bind` reuses existing assets and preserves assigned overrides when the explicit scene
rebuild tool is used. Routine tuning requires no rebuild.

Swallowed apples remain at their pickup cells while successive body segments pass over them.
Digestion therefore advances exactly one body index per movement step; its speed is not
independently adjustable. Growth happens after the tail reaches the pickup cell, with a short visual
settle. `Segment Bump` controls the smaller rounded shape on every body part; `Belly Bulge` and
`Bulge Half Length` control the larger temporary apple bump.

## Verification

The harness requires a connected Unity Editor.

```powershell
python Tools/Harness/gs.py compile
python Tools/Harness/gs.py playtest tuning 40 4
```

That plays the game unattended with real Input System events and writes a filmstrip to
`Artifacts/shots` plus a report to `Artifacts/playtest.txt`. For frame cost and garbage, run
`Tools/Harness/perf_probe.cs` during the session; `Tools/Harness/alloc_hunt.cs` attributes
allocation to one system at a time when the number looks wrong.

The snake skin is the only thing whose cost grows with the run, so `Tools/Harness/skin_load.cs`
stages fixed body lengths and measures it, including the frame the body grows on. Mesh quality
(`Sides`, `Samples Per Cell` on the skin asset) is what that cost is bought with: raising either
sharpens the silhouette and costs frame time in direct proportion. Re-run the probe after changing
them.

Judge the result by looking at the captures, and finish with a human playtest — synthetic input
proves behaviour, not feel.
