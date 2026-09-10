# Juice and polish sweep — 2026-09-10

Verification uses the running Unity Editor, synthetic keyboard and mouse input through the Input System, and composited Game view captures. No unit tests were added or run.

## Pass 1: Instructions inside the cards

The external instructions bar crowded the lower edge. Start and Pause now share a three-column controls inset: steering with WASD/arrows or swipe, P for pause/resume, and M for sound. The card has explicit spacing between its heading, description, controls, and primary action. Results retain their compact layout.

- Inspected 76 baseline frames and 75 updated frames, including full-size Start, Pause, gameplay transitions, and results captures.
- The real-input controls walkthrough passed all 10 checks: start, pickup/growth, turn, pause, stationary pause, mute, resume, collision, results, and restart.
- All nine controls labels fit their text rectangles. No external Hints object remains.
- Evidence: `Artifacts/shots/polish/`, `Artifacts/shots/sweep-panels/`, `Artifacts/panel-playthrough.txt`, and `Artifacts/panel-layout.txt`.
- No new runtime errors; the console contained an older playthrough error from before this sweep.

## Pass 2: Button feedback and the Resume glyph

The corner Resume glyph was blurred because its distance field used normalized coordinates with pixel-sized antialiasing. Converting the distance to pixels restores a crisp triangle. All three buttons now ease up on hover, compress on press, and settle on release using unscaled time.

- Inspected 76 flow frames and the full-size Pause screen, plus a held-press frame from a 28-frame mouse interaction sequence.
- The real-input controls walkthrough again passed all 10 checks.
- Actual Sound-button input recorded idle scale 1.000, hover 1.035, press 0.942, and return to 1.000 after pointer exit. Release toggled audio exactly once; the harness restored the original mute state.
- Evidence: `Artifacts/shots/sweep-buttons/`, `Artifacts/shots/button-motion/index.txt`, and `Artifacts/button-playthrough.txt`.

## Pass 3: Exaggerated appetite and excited eyes

Anticipation starts within 3.2 cells. The fully open mouth is 1.8 units wide, with a stretchy cream rim, a broader lower jaw, and a growing tongue. The upper face rises while the eyes spread, stretch to 1.8 times their resting height, and bounce slightly out of phase. The widened jaw and raised eyes remain visible in silhouette when the snake faces away.

Eye whites, pupils, glints, and brows now share an eye rig so they enlarge together. Blinks use the imported model's vertical axis, yield to the anticipation pose, and freeze during Pause. All mouth and eye controls remain editable in `Assets/GardenSnake/Tuning/Snake Mouth.asset`.

- Inspected the original mouth, two intermediate versions, the wider final version, and a speed-cap variant through contact sheets and selected full-size Game frames. Refinement added a cream outline and widened the jaw after reviewing the first captures.
- Nine real-keyboard checks passed: resting face, anticipation before eating, pause, frozen facial transforms, resume/pickup, recovery and swallowed-apple cleanup, collision, results, and restart. Runtime errors: zero.
- A temporary runtime speed variant reached 18 apples at the normal 0.115-second step cap (8.7 cells/sec). Inspected its approach, swallowing, turns, and collision frames. Runtime errors: zero. The temporary speed change was discarded by leaving Play Mode.
- Final front and rear examples: `Artifacts/shots/mouth-big-wide/068.png` and `052.png`. Side examples appear in `mouth-big-wide/011.png` and `mouth-speed-cap/018.png`.
- Evidence: `Artifacts/mouth-controls.txt`, `Artifacts/shots/mouth-big-wide/`, `Artifacts/shots/mouth-speed-cap/`, and their contact sheets. Storyboard runs restore the PlayerPrefs values present when each run starts.

The sweep was wrapped up at the user's request after these three validated passes. Play Mode is stopped, the scene has no unsaved edits, and the saved best score remains 32. Pre-existing build-profile, generated-build, icon-metadata, and probe-file changes were left untouched.
