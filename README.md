# Garden Snake

A small 3D Snake game for the Pikoya mini-game assignment. Pick apples, grow longer, and avoid the
garden edge and your own tail. The scope stays deliberately small on purpose: one readable board,
responsive controls, short replayable runs, and as much attention as possible on how it feels.

## Play

Open this project with **Unity 6000.6.0f1**, open `Assets/GardenSnake/Scenes/GardenSnake.unity`, and
press Play.

| Action | Controls |
| --- | --- |
| Start / play again | Space, Enter, or the main button |
| Steer | Arrow keys, WASD, or swipe |
| Pause / resume | P, Escape, or the pause button |
| Sound | M or the sound button |

Movement follows the visible grid. Reversing directly into the neck is ignored. Two upcoming corners
can be buffered. A run starts after a short beat so there is time to read the board. Each apple adds
one segment and slightly increases speed, up to a fixed cap; the vignette closes in a little as the
pace climbs, which is the only readout for speed. Switching away from the game pauses the run. Best
score and sound preference are saved locally.

## Feel

The reaction layer is [Feel](https://feel.moremountains.com/) (MMFeedbacks). Five players cover the
beats a player can notice — pickup, death, run start, new best, turn — and each one owns its camera
shake, freeze frame, screen flash, slow motion and UI spring. `FeedbackManager` is the only seam
between the simulation and the noise it makes, so the game reports *what* happened and never *how
loud*. Nothing holds a reference to it: turn that one object off and the game is still a correct,
silent game.

Around that: an expanding shockwave ring and a glowing ground pool drawn with the All In 1 Sprite
Shader, confetti in four hues, a sparkle trail that thickens with speed, a meadow of flowers swayed
by one shared wind clock, a camera that sits back on the menus and pushes in during a run, and a
death that clears the board before the results card lands.

All audio is synthesized from scratch by `Tools/generate_audio.py` — six effects and a seamless
16-second ambient bed — so the project ships no licensed sound.

## Web build

Install **Web Build Support** for the same Unity version. Choose **Garden Snake > Build WebGL**. The
generated website is in `Builds/WebGL`.

```powershell
python Tools/serve_web.py
```

Open **http://localhost:8080**. Do not open `index.html` directly as a file. The build uses
uncompressed files so a basic static HTTP server works without custom compression headers. Upload
the complete `Builds/WebGL` folder to a static host or package it for an HTML5 game host.
Hosting/publication is separate from building; no remote deployment is performed automatically.

The first pass targets desktop browsers in landscape. Swipe steering is included, but small portrait
layouts need a dedicated UI pass.

## Structure

Data flows one way. Each layer announces what it did; the layer below listens. Nothing reaches
back up.

| Layer | Component | Job |
| --- | --- | --- |
| Input | `PlayerController` | Reads the keyboard and the pointer, announces what the player asked for, and holds no game state. |
| Rules | `GameLoopManager` | Owns the simulation, the fixed movement step, the record between runs, and the pause and sound preferences. |
| Visuals | `SnakeManager` | Draws the animal — body poses, continuous skin, face, blink, growth, death — from whatever the loop says is true. |
| Reactions | `FeedbackManager` | Observes the loop and answers each beat: sound, Feel players, board waves, the pickup ring, the sparkle trail, the pace vignette, HUD toasts. |

The compiler enforces the layering. Each layer is its own assembly, and an assembly can only
reference the ones beneath it:

    Assets/Scripts/Core          deterministic grid simulation, no Unity references
    Assets/Scripts/Gameplay      GameLoopManager, PlayerController, the garden's beat channel
    Assets/Scripts/Presentation  SnakeManager, the skin and face, apple, camera, HUD, gauge
    Assets/Scripts/Feedback      FeedbackManager
    Assets/Scripts/Garden        the meadow's wind and the wildlife
    Assets/Editor                authoring, build and Play mode drivers

Reaction beats travel on `GardenBeats`, a static channel. `FeedbackManager` strikes them; the
meadow and the wildlife listen through a shared `GardenDweller` base. Neither side knows the other
exists, so the scenery can be deleted without touching the game.

Third-party packages are quarantined in `Assets/Externals` — Feel, the All In 1 Sprite Shader and
TextMesh Pro. Everything else under `Assets` is this game.

Authoring lives in `Assets/Editor`: `GardenBuilder` rebuilds the materials, prefabs, scene and web
build; `GardenPalette` holds every colour in one place; `GardenHud` and `GardenSprites` generate the
interface straight into PNG assets so no UI art is imported; `GardenFeel` authors the MMFeedbacks
players; `GardenPlaythrough` drives real input in Play mode and `GardenPlaytest` plays unattended.

Outside the project: `Tools/Blender` holds the editable model source and its export scripts, and
`Tools/Harness/gs.py` drives the Editor from a terminal.

## Rebuild assets and scene

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python Tools/Blender/create_assets.py
python Tools/generate_audio.py
```

In Unity, choose **Garden Snake > Rebuild game scene**. Save or discard any unsaved scene edits
first. Rebuilding replaces the generated Garden Snake scene; adjust the builder for changes you want
to preserve across rebuilds. Ordinary tuning can be done on the four components of the `Snake Game`
object in the Inspector.

The `.blend` source stays outside `Assets` so another developer can import the game without
installing Blender. Unity uses the exported FBX files. Materials are remapped explicitly to URP
assets, and serialized references are wired by the builder.

## Verification

Use Unity Test Runner in Edit mode with filter `GardenSnake.Tests`. Ten tests cover movement gating,
pause, reverse rejection, buffered corners, apple growth/spawning, wall and self collision, entering
a vacated tail cell, reset, deterministic randomness, and a full-board win.

For the integration check, enter Play mode from a fresh start screen, then select
**Garden Snake > Verify controls in Play mode**. Keep the Game view focused during the check. The
driver reads game prompts, sends keyboard and mouse input, checks pickup/growth, pause, sound,
resume, collision, and restart. It writes `Artifacts/playthrough.txt` and screenshots.

For an unattended look at the game, drive the Editor from a terminal:

```powershell
python Tools/Harness/gs.py playtest run 40 8
```

That enters Play mode, steers with real Input System events, restarts after each death, and writes a
filmstrip to `Artifacts/shots` plus a report to `Artifacts/playtest.txt`. Synthetic input verifies
behaviour; final game-feel judgement still benefits from a human playtest.

### Performance

The game is built to hold 60fps in a browser. Three probes keep it honest, each run from a Play
mode session:

- `Tools/Harness/perf_probe.cs` — frame cost and allocation percentiles over a scripted run.
- `Tools/Harness/alloc_hunt.cs` — switches one system off at a time to attribute allocation.
- `Tools/Harness/skin_load.cs` — stages fixed body lengths and measures the snake skin, which is
  the heaviest per-frame work and the only thing that scales with how well the player is doing.
  Run it after any change to `SnakeSkin`.

Measured in the shipped web player at 1600x950: a locked 16.7ms frame, and across 5,738 frames of
play not one frame over 20ms. Pickups are clean too — 480 frames sampled around 17 pickups, worst
frame 16.9ms.

See [model polish and verification](Docs/ModelPolish.md) for the model audit and capture workflow.

See [technical notes](Docs/TechnicalNotes.md) for asset provenance, AI disclosure, and next steps.
