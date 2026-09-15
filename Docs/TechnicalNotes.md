# A Few Technical Notes

## Stack & Assets

**Engine:** Unity 6000.6.0f1, URP 17.7, C#. Built for WebGL and hosted on itch.io.

**Unity packages:** Input System (all input goes through `InputSystem_Actions.inputactions`, so it's rebindable without touching code), uGUI + TextMesh Pro for the HUD, and `com.unity.pipeline` — that one is only an Editor automation package, the game code never touches it.

**Third-party plugins:** just one — **Feel (MMFeedbacks + MMTools)** by More Mountains. It drives the whole reaction layer: shake, freeze frames, slow-mo, flash, UI springs. It's licensed and quarantined under `Assets/Externals` so it's easy to see what's mine and what isn't.

**Visual assets:** everything in the build is made for it.

- All the 3D models were authored in Blender through committed Python scripts (`Tools/Blender/`) and exported as FBX — snake head/body/tail, apple, tiles and rims, and the garden props (planter, tree, bush, rock, gnome, watering can, spade, rake, flowers, pot) plus the wildlife. The `.blend` files and the generator scripts are both in the repo, so the geometry is reproducible, not a bought pack.
- No UI art is imported. Every panel, pill, circle, the pause/play/sound glyphs, the glow and ring, and the lawn gradient were generated as signed distance fields and baked to PNGs (`Assets/UI/Generated/`).
- The grass and clay textures are small seamless maps generated the same way. Materials, lighting, post and scene composition are authored in-project.
- Font is Liberation Sans, which ships with Unity's TMP Essential Resources under the SIL OFL (license file is included).

**Audio:** the six SFX — turn, pickup, start, new best, click, lose — are synthesised from scratch by `Tools/generate_audio.py` (plain Python, sine partials through an envelope and a low-pass, so they all sound like one instrument). The background music is the one asset I didn't make: a free loop from Freesound — *"Pawsome Vibes"* by **Kjartan Abel** (Freesound #679355). I originally had a synthesised ambient bed there too, but it was dull, and a real track made the garden feel much better. Credit goes to them.

## AI Usage

I used AI heavily across the whole thing, and I'd rather be straight about it than hedge.

The project was about a week of work, and it ran in two passes:

1. **First pass — OpenAI Codex (GPT-6).** The foundation: the grid simulation and game loop, input, the initial scene, the Blender model-generation scripts, the audio synthesis script, the WebGL template, and the Editor tooling.
2. **Second pass — Claude Code (Opus 5).** Everything about how it looks and feels: palette and scene composition, the HUD and speed gauge, the procedural UI sprites, the Feel integration, the board wave system, the wind and wildlife, the snake's skin/mouth/expression work, the ScriptableObject tuning layer, the performance pass (pooling, killing per-frame allocations), and the layered refactor into four assemblies.

So: **most of the C# was written or substantially assisted by AI.** Same for the assets that come out of scripts — the Blender geometry, the generated UI sprites, and the SFX are all AI-assisted output. No image-generation service was used; there's no AI-generated art or concept art in the build.

What I actually did myself: picked the scope, set the visual and feel direction, decided what was good enough and what wasn't, tuned the numbers, and did hands-on cleanup passes between the AI passes (there are a few "manual cleanup" commits in the history that are exactly that). I reviewed and played every build.

One part of the workflow worth mentioning, because it changed the quality a lot: I wrote a small harness (`Tools/Harness/gs.py`) that drives the live Unity Editor from a terminal — enters Play mode, sends real Input System events, reads the simulation state, and captures Game view frames. That meant the AI could actually look at the running game and iterate on it, instead of guessing from source. The rest of `Tools/Harness/` is one-off probes from that same setup (frame cost, allocation hunting, model audits, the web build script).

No unit tests — verification was Play Mode runs and captured frames.

## Next Steps

If I had more time:

1. **Real playtesting.** Starting speed, the speed ramp, turn timing, how death reads — all of that is tuned by me alone right now. Other people's hands on it is the first thing I'd want.
2. **A proper portrait/mobile pass.** The layout adapts and swipe works, but it's a widescreen design that copes with phones rather than one built for them. Plus real device testing.
3. **Accessibility.** Remappable controls (the actions asset is already set up for it), a reduced-motion option, and a contrast option. There's a lot of motion in this game.
4. **More character in the snake.** A blink, a glance toward the apple, more idle personality — and a much bigger payoff for actually filling the board.
5. **Build size and load time.** The WebGL build is uncompressed because it made hosting simple. Compression and a profiling pass on real devices would cut the first load a lot.

What I'd deliberately *not* add yet: extra modes, obstacles, progression, leaderboards, any backend. The base loop should feel great first.
