# Character and garden art review — September 9, 2026

The previous pass improved individual primitives but left the overall construction obvious: the snake resembled separate beads, the bunny's feet did not participate in its hop, turtle scutes floated above the shell, and bird and butterfly wings were flattened ovals. The bird's perch was detached from the trunk. Small pink spheres on bushes read as stray dots.

## Changes

- The snake now uses a continuous curved surface with a tapered tail, cream underside, and surface-following dorsal markings. Its Blender head remains intact. Existing visual poses drive the surface; grid rules and collision cells are unchanged. Empty pose objects replace hundreds of prewarmed body-model instances.
- Bunny anatomy now has a pear-shaped torso, haunches, hinged forelegs and hind feet, shaped ears, and a smaller seated muzzle. Hops include ground contact, anticipation, flight, landing compression, and delayed ear motion.
- Turtle scutes conform to the shell. Muted shell colors and thin seams replace raised green dots. Hiding retracts the neck instead of shrinking the head almost to nothing. Limb motion uses diagonal pairs.
- Birds have shaped beaks, scalloped flight wings, separate tail feathers, articulated feet, and flight/glide/fold poses. Butterflies have lobed wings, dark borders, eyespots, and antennae. Ladybugs retain their small silhouette with alternating leg timing.
- The perch connects to the trunk through two overlapping branch sections, with a supporting twig and leaf. It inherits tree motion.
- Each bush has 11 blossoms with five petals, yellow centers, and leaf accents. Pink dots are removed. The Blender source and exported Bush FBX are updated. Petals are joined into material groups to avoid a renderer for every petal.

## Verification

All checks ran in Unity Play Mode; no unit tests were added or run.

- Inspected wildlife motion sheets, the imported bush from both sides, the branch attachment, and full game frames.
- The final controller/UI scenario collected 13 apples, made 23 turns, and captured 41 frames. Start, pickup, growth, pause/resume, wall collision, death, and restart passed. The runtime error listener recorded zero errors. The harness restores the PlayerPrefs keys it touches.
- The concurrent 23-second wildlife probe sampled 139 times: zero board intrusions, zero animal proximity violations, zero wildlife colliders. All 14 animals reacted. All 877 observed vegetation transforms moved. The probe included explicit feedback beats as well as actual gameplay events; it is supplemental motion verification, not an input test.
- Earlier isolated feedback verification observed turtle hiding, bunny hopping/grazing, bird flight/rest, and insect flight/crawl/rest.
- Reviewed the actual game camera through straight movement, curved corners, growth, and death. Rechecked after correcting the bird's resting wing-fold direction.

Generated evidence stays in ignored `Artifacts/`: `motion-audit/`, `model-audit/unity/Bush-*.png`, `shots/presentation-*.png`, `presentation-verification.txt`, and `fluff-audit.txt`.

## Reproduce

With the saved GardenSnake scene open, `python Tools/Harness/gs.py script Tools/Harness/install_wildlife.cs` imports the bush and reinstalls wildlife. The scene installer saves its generated changes and refuses an already dirty scene.

Enter Play Mode with `python Tools/Harness/gs.py play`. Run `presentation_play.cs` for controller/UI verification, `motion_audit.cs` for supplemental forced-travel pose samples, `perch_rest.cs` for a natural resting pose, and `fluff_audit.cs` for longer ambient observation. Do not stop Play Mode before an observation finishes. These harness scripts run through `python Tools/Harness/gs.py script <path>`.

`Tools/Blender/update_bush.py` updates only the bush in the source sheet and FBX. Full `create_assets.py` rebuilds retain the blossoms through `polish_assets.py`. This pass originally baked wildlife as native Unity meshes. Wildlife now uses five Blender-authored FBX models; see [Wildlife authoring](WildlifeAuthoring.md). Animation remains procedural in Unity. Browser build and target-device performance were not tested in this pass.
