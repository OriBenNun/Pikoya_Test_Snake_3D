## first prompt (Astra Medium):

start implementing a simple Snake 3D game. create the 3D assets using Blender. follow the instructions. your focus is the code, infra, and models. add some juice and polish, but focus on the game itself.

`D:\Dev\Tests\Pikoya\Pikoya_Test_Snake_3D\Docs\Home Assignment_ Mini-Game Demo 8.26.pdf`

## second prompt (Opus 5 High):

continue Codex work. do a visual sweep - improve UI, remove unneeded elements to keep it clean, make it look more like a real polished game rather than a web slop game. also make the field bigger. do this iteration in a loop until you're out of tokens:

1. play the game using the CLI and custom harness (build one if needed) and pay attention to every little detail
2. take notes for everything that can be improved.
3. finish the playtest
4. implement improvements and fixes
5. repeat

### [quick steer prompt]

also focus on:

1. feedbacks and feel (added the MMF package)
2. sprite shaders (added the AllInOneShader package)
3. here's a reminder from the assignment doc on what's important:

   > The goal isn't to build a large or complex game. Keep the core mechanic simple and focus your time on making it feel great to play - responsive interactions, satisfying feedback, animations, particles, effects, transitions, sound, and all the small details that bring a game to life.

### [another quick steer prompt]:

make sure to commit and keep a clean git history as you go

### [Steer prompt after 30 mins]:

the grid should be visible

### [Steer prompt after 45 mins]:

now the BG renders as black. it should be juicy and joyful, not dark and moody

also, no need for on-screen arrow buttons

### [Steer prompt after 50 mins]:

1. too bright, tune it down a bit.
2. the "game over" snake animation stops mid-animation when the popup opens [Image #1]

### [another steer prompt after 52 mins]:

also:

1. make sure the UI overlay elements doesn't overlap with other game elements like the board
2. scatter way more flowers in the bg, and add global wind "dance" animation.

### [Steer after 62 mins]:

fix compile error: `Assets\GardenSnake\Editor\GardenBuilder.cs(57,37): error CS0103: The name 'CreateAppleMarker' does not exist in the current context`

### [Steer after 65 mins]:

there's a visible small lag when eating an apple, implement simple object pooling to avoid runtime instantiation (and allocation in general)

### [Steer after 75 mins]:

the field should take more space of the screen (about 60%-80%), it doesn't have to be a square. do a zoom camera movement when starting and finishing a game (zoom out before play, zoom in during play)

### [tiny Steer after 115 mins]:

console is spammed with

```
NullReferenceException: Object reference not set to an instance of an object
GardenSnake.SnakeHud.Update () (at Assets/GardenSnake/Scripts/SnakeHud.cs:201)
UnityEngine.StackTraceUtility:ExtractStringFromExceptionInternal(Object, String&, String&)
```

### [Steer at 130 mins]:

there are still visible lags, especially when growing. use profiler to diagnose and fix it

### [After it ran out of tokens during a webgl build bash which i finished manually]:

the webgl is open at http://localhost:50784/ (open in chrome). run a final verification pass, ***fix bugs, then wrap up (including editor code tools cleanup before i start manual changes in scenes and prefabs), commit and push***

### [quick Steering once seeing Claude messing with the index.html file to fix a screen fitting bug]:

it didn't happen to me when i used Build And Run, check your playwright settings first

*... [Claude understood its mistake, but claimed the index.html change to allow responsiveness is a good one, so i pushed back]*

ok, but the fix shouldn't come from changing the build output. change the player settings instead.

## [After manual playtesting, prompted GPT 6 Astra Medium]:

polish pass:

1. add a juicy speed gauge to the HUD (generate the needed sprites or create a 3D gauge if easier and more juicy)
2. new best changes: have two different celebrations feedbacks: one for when passing the current best (once per run, never in the first ever run), and one for each apple past the last best (whispering compared to the big celebration).
3. add a "grid cells wave" system to control the height of the grid's cells to form waves and other juicy patterns (create a few presets) from different source cells (so it can start from the player). use it for big feedbacks
4. improve the current models and add more stuff to the bg to make it look like an actual sunny cute garden (gnomes, tools, trees, bushes, more flower variants, etc.)
5. improve the snake models to give it more character and make it more silly and cute
6. the board models are very rough. make them smooth with more interesting materials and textures

Important: don't aim for realistic high poly, keep everything low poly but with a very juicy style

implement all improvements, test it in playmode, identify issues and possible improvements, repeat

### [quick steer at 4 mins]:

dont forget to commit as you go

### [quick steer at 5 mins, after seeing Astra start writing unit tests]:

no need for unitesting, keep it in memory, playtime tests and captures is the reliable way to verify integrity

## [After i gave the scene a deep visual inspection]:

some 3d models has quirks in them. first - update blender to the latest version, then go through all the models, look for visual issues, and fix them manually. it should look polished

## [adding fluff to the scene, GPT 6 Astra]:

now a fluff pass - add non-gameplay fluff to the scene:

1. make sure all foliage is moving with the wind
2. add animated birds with fly + rest (on ground or tree) states
3. add animated critters to the grass
4. add cute animated animals like bunnies and turtles to the garden
5. make sure everything reacts to the game's feedbacks so it feels more alive, reactive and juicy!

### [Steer after 4 mins]:

too many overlap. too uniform movement

### [told Astra High to improve models and animations]:

you can do better with these models and animations. be highly critique about it.

### [after tokens reset, still Astra High]:

continue, also - the bird rests on a branch that doesn't attached to anything. also - there are small pink dots on the bushes - it looks akward, make it look more like flowery bush.

## [adding more fluff]:

i want the snake to have a mouth, then it opens it when approaching to eat apple, then the apple should "rool through its body" until it adds a new snake body part at the tail (a reference to the little princess snake getting big in the belly)

### [after ran out of tokens, new agent]:

continue the previous agent, and make sure everything is adjustable through the inspector/SOs with serialized fields (including wind, waves, etc)

### [quick steer]:

dont forget to commit as you go

### [another agent after session died]:

continue last agent, and i want the digested apple to "stay in place" until the tail reaches it, with a small bump on every body part. iterate on it until it looks amazing

## [after i played the game and it looked good to me, Astra High]:

start a juice + polish sweep. use this loop:

1. play in editor and capture many frames
2. go through captures, and identify places that can be improved, or places that don't have juice and polish yet.
3. implement fixes and improvements
4. verify in play mode using captures
5. commit
6. repeat

### [steer]:

i dont like the instrctions bar, move the text inside the Start and Pause panels and organize it in a clean, nice way

### [steer]:

the mouth should get stupidly big before eating (make it funny and silly), and the eyes should get excited. it should be somewhat visible even if the snake isn't facing the camera

### [fix tweak, Astra Medium]:

when mouth is fully open, there's a big visual gap between the body and the head that doesn't look good. fix it by setting the head pivot point correctly, or by extending the body to the head (less optimized) [Image #1]

## [once i was pleased with the polish level for now and everything worked as expected, i looked at the project codebase architecture and re-designed it by myself. Opus 5 High]:

architectural and codebase refactor:

1. game has these core classes, with clear SOP and decoupling. data flowing dow:

   a. GameLoopManager - rules + meta loop layer. invoke state-driven events.

   b. PlayerController - input layer. invoke input-driven events

   c. SnakeManager - visual layer. manages the RUNTIME snake visual needs. can invoke state-driven events related to the snake visuals. Important: no need for the overwhelming amount of fields to control the snake. keep only the most important nodes for a game designer / art director, or remove them all - baking them into the class/prefab

   d. FeedbackManager - feedback layer. triggers the actual gameplay related feedbacks. observer pattern. no one needs to know about it.

2. fluff and other perepherials can have their own classes, preferabbly consolidated where possible.
3. seperate into assembly defenitions and create the assets for it
4. keep behavior the same.
5. keep performance always in mind - it should run in the browser without any hickups or drops. stable 60 fps is required.
6. new branch. commit as you go
7. verify using playmode and actual builds when you finish the refactor

### [quick steer]:

a small correction for the "data flowing down": the PlayerController obviously is above the GameLoopManager

### [another steer after 60 mins]:

when you finish - there's still a stutter/framedrop every time the snake eats the apple. if that's a timescale feedback - replace it with soft wave and camera shake. if it's not - profile the problem and fix it

### [after Opus finished]:

1. why do we still need the core classes? why didn't you folded them into the gameloop manager?
2. have you improved the garden wind or just noted the bottleneck?

### [steered it after it answered. had to push back so the final architecture would fit my usual style of thinking]:

no need for editor tests. fold it in. I dont like the pattern of "game = new Game()" when we're making a Unity game, not a generic "plug-in" console game with replaceable renderers. it should (and would) simplify things, not the opposite.

### [quick steer after seeing Opus replacing the custom class with Vector2Int]:

i actually prefer a custom "Cell" struct (SIMPLE!) over Vector2Int

## [after Opus finished, started a new session]:

do a full clean code sweep:

1. improve naming
2. improve readability
3. consolidate classes/files where it makes sense
4. remove unnessecarry comments
5. prefer modern C# over old-school

btw - Codex is working on the blender files in parallel - ignore it

## [in parallel, told Astra High to improve the models one last time]:

do a 3d models sweep - go over all the game's models (not the external packages' ones) one by one and look for possible improvements, then implement them using Blender CLI (and Computer-Use skills when needed) and replace the current assets.

important: Claude is working on the repo and committing in parallel. don't interfere, don't touch Unity directly, just the models.

goal: make everything look better, coherent, and fit a super juicy casual game. iterate until achieved.

### [steer Astra]:

the snake head can recieve some love. give it a special focus after the first pass, then resume the loop

### [another steer after Claude ran out of tokens mid-refactor]:

also, of course you should improve the materials as well. btw - Claude is paused, so you can use Unity freely

## [more refactor, Opus 5 High]:

seperate the GardenAnimal monolith into small classes by species inheriting from abstract GardenAnimal

## [more refactor after i started manually going through the codebase, Opus 5 High]:

1. no need for GameCameraRig. the framing thing will be driven by the resolution settings on the player settings. just lerp zoom in and out using coroutines on the GameLoopManager (or another, more fitting manager)
2. no need for live tuning capabilities. if it simplify things - make it work only when changing while not in play mode
3. there are wayy too many inspector fields across the monobehaviors. simplify things, remove overengineered parts, leave only the most impactful and "juice-related" levers as fields with clear tooltips, move the others into SOs or even constants when appropriate
4. clean the editor builders, keep only your harness and blender tools

### [steer after seeing a constants abuse]:

i changed my mind, use SOs instead of constants. each set of related settings gets its own SO

### [another steer after seeing a potential bug in Claude's live thinking]:

if changing the grid size in the rules settings won't create the correct grid layout in scene, it means changing it will break the game - this is exactly the type of fields that should remain constant. find more like this

## [doing final fixes before submission, Opus 5 High]:

1. do a cleanup sweep of the scripts, make sure:

   a. no redundant or obvious comments. add comments where actually needed explanation to avoid logic reading

   b. order of code in a class is: consts -> serialized fields -> fields -> properties -> methods -> subclasses/enums/structs. always public -> protected -> internal -> private

2. random seed should be random for each object. move it from the SO to the initilazation and actually get a random seed each session and not the same one for all animals.
3. use the new input system event driven workflow instead of querying keys every frame. same for pointer, 99% of the time the mouse isn't in use at all, no need to query in update.

### [steer after seeing what Claude is doing]:

instead of building the action map in runtime, just use the existing project wide input action asset or create a new one. preferablly - just create a new map inside the existing asset, and use InputAction as SerializedFields so I can assign and change the inputs easily

### [after finished, extended]:

handle the audit surfaced issues. i want the project to be clean, organized and ready for submission

### [more finishes]:

now change the player settings so it will actually be responsive and fit both PC and mobile (landscape)

### [steer]:

change default resolution to 1080p

### [more polish]:

i want the pickup pitch levels to increase more than 5 or 6 times. change it to 20 and make sure the logic actually supports it

### [last polish]:
speed gauge is rendered too pixelated. im trying to scale it but it shrinks down on Awake.

### [continued]:
still scales down to 1 when entering play. in the scene it's 1.5 on all axes