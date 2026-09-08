# Garden Snake

A small 3D Snake game for the Pikoya mini-game assignment. Pick apples, grow longer, and avoid the garden edge and your own tail. The scope stays deliberately small: one readable board, responsive controls, and short replayable runs.

## Play

Open this project with **Unity 6000.6.0f1**, open `Assets/GardenSnake/Scenes/GardenSnake.unity`, and press Play.

| Action | Controls |
| --- | --- |
| Start / play again | Space, Enter, or the main button |
| Steer | Arrow keys, WASD, on-screen direction pad, or swipes |
| Pause / resume | P, Escape, or Pause / Resume |
| Sound | M or Sound On / Off |

Movement follows the visible grid. Reversing directly into the neck is ignored. Two upcoming corners can be buffered. Each apple adds one segment and slightly increases speed, up to a fixed cap. Switching away from the game pauses the run. Best score and sound preference are saved locally.

## Web build

Install **Web Build Support** for the same Unity version. Choose **Garden Snake > Build WebGL**. The generated website is in `Builds/WebGL`.

```powershell
python Tools/serve_web.py
```

Open **http://localhost:8080**. Do not open `index.html` directly as a file. The build uses uncompressed files so a basic static HTTP server works without custom compression headers. Upload the complete `Builds/WebGL` folder to a static host or package it for an HTML5 game host. Hosting/publication is separate from building; no remote deployment is performed automatically.

The first pass targets desktop browsers in landscape. Touch steering is included, but small portrait layouts need a dedicated UI pass.

## Structure

- `Assets/GardenSnake/Scripts/Core`: deterministic C# grid simulation, with no Unity references.
- `Assets/GardenSnake/Scripts/SnakeController.cs`: input, tick timing, model presentation, audio, feedback, persistence.
- `Assets/GardenSnake/Scripts/SnakeHud.cs`: HUD, start/pause/results flow, and UI feedback.
- `Assets/GardenSnake/Editor/GardenBuilder.cs`: repeatable material, prefab, scene, and WebGL build authoring.
- `Assets/GardenSnake/Editor/GardenPlaythrough.cs`: a Play mode driver that uses real keyboard and mouse input.
- `Assets/GardenSnake/Tests/Editor`: simulation regression tests.
- `Tools/Blender/GardenSnake.blend`: original editable Blender source.
- `Tools/Blender/create_assets.py`: deterministic model generation and FBX export.

## Rebuild assets and scene

```powershell
& 'C:\Program Files\Blender Foundation\Blender 3.5\blender.exe' --background --python Tools/Blender/create_assets.py
python Tools/generate_audio.py
```

In Unity, choose **Garden Snake > Rebuild game scene**. Save or discard any unsaved scene edits first. Rebuilding replaces the generated Garden Snake scene; adjust the builder for changes you want to preserve across rebuilds. Ordinary tuning can be done on the `Snake Game` component in the Inspector.

The `.blend` source stays outside `Assets` so another developer can import the game without installing Blender. Unity uses the exported FBX files. Materials are remapped explicitly to URP assets, and serialized references are wired by the builder.

## Verification

Use Unity Test Runner in Edit mode with filter `GardenSnake.Tests`. Ten tests cover movement gating, pause, reverse rejection, buffered corners, apple growth/spawning, wall and self collision, entering a vacated tail cell, reset, deterministic randomness, and a full-board win.

For the integration check, enter Play mode from a fresh start screen, then select **Garden Snake > Verify controls in Play mode**. Keep the Game view focused during the check. The driver reads game prompts, sends keyboard and mouse input, checks pickup/growth, pause, sound, resume, collision, and restart. It writes `Artifacts/playthrough.txt` and screenshots. Synthetic input verifies behavior; final game-feel judgement still benefits from a human playtest.

See [technical notes](Docs/TechnicalNotes.md) for asset provenance, AI disclosure, and next steps.
