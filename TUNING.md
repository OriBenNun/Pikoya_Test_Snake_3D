# Garden Snake tuning

Almost nothing is tuned on a component any more. Each set of related settings is a ScriptableObject
in `Assets/Tuning`, so the values live in one asset with tooltips instead of being spread across the
Hierarchy, and a component's Inspector shows only its wiring plus the handful of levers that are
worth reaching for mid-look.

Edit tuning assets **outside Play Mode**. The game reads them when it starts; nothing is designed to
be dragged while playing, and Play Mode edits to scene components are discarded on stop as usual.

## The assets

| `Assets/Tuning/…` | What it holds |
| --- | --- |
| `Run Rules` | Starting length, turn buffer, first apple distance, frame rate, restart delay |
| `Swipe` | What counts as a swipe, in pixels and as a fraction of screen height |
| `Snake Skin` | Head/body/tail proportions, body shape, markings, digestion bulge, mesh quality, optional material overrides |
| `Snake Mouth` | Anticipation, jaw speed, the swallow, the excited eyes, and the geometry of every soft piece of the face |
| `Snake Motion` | Banking, slither, growth, idle breathing, blinking, the death |
| `Apple Motion` | Apple size, drop-in, hover, spin, and the pool of light under it |
| `Board Waves` | One preset per wave pattern, how each is shaped, and the board's limits |
| `HUD Chrome` | Corner controls, wordmark fade, record flash |
| `HUD Toast` | The `+1` that pops where an apple was eaten, and the milestone banner |
| `HUD Card` | How the card arrives, and where everything on it sits |
| `HUD Copy` | Every word the game says, and the scores behind each verdict |
| `Button Feel` | Hover, press, settle and release for every button |
| `Speed Gauge` | Needle kick, pace bands and captions, and the whole procedural face |
| `Sound Mix` | Pitch and level of every one-shot |
| `Feel Beats` | How hard each beat is allowed to land |
| `Wave Cues` | Which wave the board runs for each moment, and the milestone interval |
| `Reactions` | Pickup ring, head trail, pace vignette, death puff |
| `Wind` | Sway, gusts, turbulence, plant weights, and how a beat crosses the meadow |
| `Animal Motion` | What every animal shares: variation, travel, startling, body, head and limb response |
| `Bird`, `Bunny`, `Turtle`, `Butterfly`, `Ladybug` | One asset per species: its pace and its own gait |

## What is still on a component

Only wiring, the authored route of one animal, and these levers:

| Select | Levers |
| --- | --- |
| `Snake Game` / `GameLoopManager` | Opening step, fastest step, speed gained per apple, opening beat |
| `Snake Game` / `FeedbackManager` | Resting zoom and zoom seconds for the camera |
| `Snake Hud` / `SnakeHud` | Results delay: the beat after a death before the card |
| `SpeedGauge` | Needle spring and damping |
| Board / `GridCellWaves` | Wave height: one multiplier over every wave, 0 for a flat board |
| `Meadow` / `GardenWind` | Wind amplitude and beat pulse amplitude |
| Each wildlife object | Its rig, its patch (`Ground A`/`Ground B`) and its phase |

`Tools/Harness/bind_tuning.cs` creates any missing asset and assigns it everywhere it is read:

```powershell
python Tools/Harness/gs.py script Tools/Harness/bind_tuning.cs
```

An empty slot is never fatal: the component falls back to a throwaway carrying the class defaults,
so the game still runs correctly with nothing assigned.

## What is deliberately not a setting

The board is **21 x 12**, as a constant in `GameLoopManager`. The scene carries one authored grid of
patches and nothing rebuilds it, so a different number there would leave the rules playing on a
board the garden does not have. `GridCellWaves` reads the same constant, and its `cells` array *is*
that grid, row by row from the bottom left.

The same reasoning keeps these in code: the names the wind looks for (`Tree`, `Bush`, `Sprout…`) and
the face-part names the snake's head is built from, which come from the Blender source; the one body
index per step that digestion advances by; and the frame-catch-up guard that stops a stalled browser
frame moving the snake through several cells unseen.

## Framing

There is no camera rig. The garden is framed by the resolution in Player Settings (1600 x 900), and
the camera in the scene is authored to fill it - its orthographic size *is* the framing while a run
is going. `FeedbackManager` only eases that size between two values with a coroutine: pushed in
while playing, sitting back by `Resting Zoom` on the menus, on a pause and after a death.

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
(`Sides`, `Samples Per Cell` on `Snake Skin`) is what that cost is bought with: raising either
sharpens the silhouette and costs frame time in direct proportion. Re-run the probe after changing
them.

Judge the result by looking at the captures, and finish with a human playtest - synthetic input
proves behaviour, not feel.
