# Juice and polish sweep — 2026-09-10

Verification uses the running Unity Editor, synthetic keyboard and mouse input through the Input System, and composited Game view captures. No unit tests were added or run.

## Pass 1: Instructions inside the cards

The external instructions bar crowded the lower edge. Start and Pause now share a three-column controls inset: steering with WASD/arrows or swipe, P for pause/resume, and M for sound. The card has explicit spacing between its heading, description, controls, and primary action. Results retain their compact layout.

- Inspected 76 baseline frames and 75 updated frames, including full-size Start, Pause, gameplay transitions, and results captures.
- The real-input controls walkthrough passed all 10 checks: start, pickup/growth, turn, pause, stationary pause, mute, resume, collision, results, and restart.
- All nine controls labels fit their text rectangles. No external Hints object remains.
- Evidence: `Artifacts/shots/polish/`, `Artifacts/shots/sweep-panels/`, `Artifacts/panel-playthrough.txt`, and `Artifacts/panel-layout.txt`.
- No new runtime errors; the console contained an older playthrough error from before this sweep.

Next observed issue: the corner Resume glyph is a blurry shape instead of a crisp triangle. Buttons also have only color feedback; add restrained hover/press motion and verify actual pointer interactions.
