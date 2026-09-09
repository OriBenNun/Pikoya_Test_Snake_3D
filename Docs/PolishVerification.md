# Polish verification — 2026-09-09

Verified in Unity 6000.6.0f1 Play Mode using real keyboard and pointer input, runtime observations, and inspected screenshots. No unit tests were added or run after the requested Play Mode-only workflow was established.

- First-ever run: five apples, no record celebration or record whisper.
- Previous best of one: 30 apples, one large celebration, 28 quieter record apples; tying the best stayed quiet. The run reached the speed cap.
- Previous zero-score run: four apples, one large celebration and three whispers.
- Grid waves: 17 checks passed across Ripple, Bloom, Sweep, CheckerHop and overlapping sources. Source and opposite corner reached approximately 0.25 units; all cells returned to rest. Overlapping waves stayed within the eight-wave buffer and 0.48-unit height limit. Simulation coordinates did not change.
- Wave pause/resume: both checks passed.
- Controls walkthrough: ten checks passed, covering start, pickup/growth, turning, pause, stationary paused state, Sound click, Resume click, wall collision, results, and restart.
- Pointer swipe: a real mouse drag turned the snake upward.
- The final controls/swipe run produced no new console errors. Earlier attempts exposed and resolved the scrim input interception and weak/skipped ripple feedback. Unity also emitted an Editor-only DefaultScenario assertion during a compile/Play Mode transition.

Inspected captures include `Artifacts/polish-final.png`, `Artifacts/zero-record-whisper.png`, `Artifacts/record-record-crossing.png`, and `Artifacts/wave-0.png` through `wave-4.png`. Reports remain in `Artifacts/*-verification.txt` and `Artifacts/playthrough.txt`.

The original pre-polish PlayerPrefs were restored after testing (best score 23; no HasPlayed or Muted key). Play Mode was stopped. Scene file contents remained unchanged during the final verification session; the existing uncommitted scene edits were preserved.
