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

Next pass follows the requested exaggerated pre-bite mouth and excited eyes, including visibility when facing away from the camera.
