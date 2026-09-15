# Garden Snake

A small 3D Snake game for the Pikoya mini-game assignment. Pick apples, grow longer, and avoid the
garden edge and your own tail. The scope stays deliberately small on purpose: one readable board,
responsive controls, short replayable runs, and as much attention as possible on how it feels.

## Play

**https://lukasgamesstudio.itch.io/garden-snake-3d?secret=BAjaAk3WaXrdwGutmrzqXCpSjg**

Runs in the browser. The layout is responsive, so it works on phones and tablets too.

| Action | Controls |
| --- | --- |
| Start / play again | Space, Enter, or the main button |
| Steer | Arrows, WASD, or swipe |
| Pause / resume | P, Escape, or the pause button |
| Sound | M or the sound button |

## Tech stack

- **Unity 6000.6.0f1**, **URP 17.7**
- **New Input System** — bindings live in `Assets/InputSystem_Actions.inputactions` (Garden Snake map), rebindable without touching code
- **Feel / MMFeedbacks** for juice (shake, freeze frame, flash, slow-mo, UI spring)
- **TextMesh Pro** for UI text
- Models authored in **Blender**, exported as FBX (sources in `Tools/Blender`)
- All audio synthesized by `Tools/generate_audio.py` — six SFX plus a looping ambient bed, no licensed sound
- Tuning values in ScriptableObjects under `Assets/Tuning` (see `TUNING.md`)

## Architecture

Four assemblies, one-way dependencies — each layer only references the ones beneath it:

| Layer | Component | Job |
| --- | --- | --- |
| Input | `PlayerController` | Reads input actions, reports what the player asked for. No state. |
| Rules | `GameLoopManager` | The snake, the board, the movement step, score, pause, food, win/lose. |
| Visuals | `SnakeManager` | Draws the snake from whatever the loop says is true. |
| Reactions | `FeedbackManager` | Sound, Feel players, board waves, pickup ring, vignette, HUD toasts. |

Nothing holds a reference to `FeedbackManager` — delete it and the game still runs, just silent.
Garden scenery (wind, wildlife) listens on a static `GardenBeats` channel and can be deleted too.
Third-party code is quarantined in `Assets/Externals`.

## More

- `TUNING.md` — what every tunable does
- `Docs/TechnicalNotes.md` — asset provenance, AI disclosure, next steps
- `Tools/Harness/gs.py` — drives the Editor from a terminal for scripted testing
