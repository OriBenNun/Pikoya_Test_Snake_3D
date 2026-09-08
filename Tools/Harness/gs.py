#!/usr/bin/env python
"""Garden Snake playtest harness.

Drives the live Unity Editor through the Unity CLI pipeline: enters Play mode,
sends real Input System events, reads simulation state, and captures the
composited Game view (including overlay UI) to Artifacts/shots.

Usage examples:
    python Tools/Harness/gs.py play
    python Tools/Harness/gs.py key Space
    python Tools/Harness/gs.py state
    python Tools/Harness/gs.py shot ready
    python Tools/Harness/gs.py stop
"""
import json
import os
import subprocess
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
TMP = os.path.join("Tools", "Harness", "_scratch.cs")


class CommandError(RuntimeError):
    pass


def cli(*args, timeout=120):
    result = subprocess.run(
        ["unity", "command", *args, "--no-banner", "--json"],
        cwd=ROOT, capture_output=True, text=True, timeout=timeout, shell=False)
    text = result.stdout.strip()
    start = text.find("{")
    if start < 0:
        raise SystemExit("no json from unity cli:\n" + text + result.stderr)
    return json.loads(text[start:])


def unwrap(payload):
    data = payload.get("data") or {}
    if not data and payload.get("errors"):
        raise CommandError(json.dumps(payload["errors"])[:400])
    result = data.get("result", data)
    if isinstance(result, dict) and "success" in result and "result" in result:
        if not result["success"]:
            raise SystemExit("eval failed: " + json.dumps(result.get("diagnostics"), indent=1))
        return result["result"]
    return result


def evaluate(code):
    path = os.path.join(ROOT, TMP)
    with open(path, "w", encoding="utf-8") as handle:
        handle.write(code)
    return unwrap(cli("eval_file", "--file", TMP.replace("\\", "/")))


KEYBOARD = """
UnityEngine.InputSystem.Keyboard board = null;
foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
  if (device.name == "HarnessKeyboard") board = (UnityEngine.InputSystem.Keyboard)device;
if (board == null) {
  UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
  board = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("HarnessKeyboard");
}
"""


def state():
    return evaluate("""
var controller = UnityEngine.Object.FindAnyObjectByType<GardenSnake.SnakeController>();
if (controller == null) return "no-controller";
var game = controller.Game;
return string.Format("state={0} score={1} len={2} head={3},{4} food={5},{6} best={7} step={8:0.000}",
  game.State, game.Score, game.Body.Count, game.Body[0].X, game.Body[0].Y,
  game.Food.X, game.Food.Y, controller.Best, controller.StepSeconds);
""")


def press(names, hold=0.09):
    keys = " | ".join("UnityEngine.InputSystem.Key." + name for name in names)
    evaluate(KEYBOARD + f"""
UnityEngine.InputSystem.InputSystem.QueueStateEvent(board,
  new UnityEngine.InputSystem.LowLevel.KeyboardState({keys}));
return "down";
""")
    time.sleep(hold)
    evaluate(KEYBOARD + """
UnityEngine.InputSystem.InputSystem.QueueStateEvent(board,
  new UnityEngine.InputSystem.LowLevel.KeyboardState());
return "up";
""")


def click(name):
    return evaluate(f"""
foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsSortMode.None))
  if (button.name == "{name}") {{ button.onClick.Invoke(); return "clicked {name}"; }}
return "missing {name}";
""")


def shot(name, width=1600, height=1000):
    # capture_game_view writes under the authoring root, so land it there and
    # move the PNG into Artifacts/shots to keep the Unity asset database clean.
    staged = f"_shots/{name}.png"
    cli("capture_game_view", "--source", "screen", "--width", str(width),
        "--height", str(height), "--save_path", staged)
    source = os.path.join(ROOT, "Assets", staged.replace("/", os.sep))
    target = os.path.join(ROOT, "Artifacts", "shots", name + ".png")
    os.makedirs(os.path.dirname(target), exist_ok=True)
    for _ in range(20):
        if os.path.exists(source):
            break
        time.sleep(.2)
    if os.path.exists(source):
        os.replace(source, target)
    for stale in (source + ".meta",):
        if os.path.exists(stale):
            os.remove(stale)
    return target


def playmode(target):
    cli("editor_play" if target else "editor_stop")
    if target:
        time.sleep(1)
    for _ in range(150):
        time.sleep(.6)
        try:
            status = unwrap(cli("editor_status"))
        except CommandError:
            continue  # the Editor drops requests while it swaps play mode
        if status.get("playMode") == ("playing" if target else "stopped"):
            if target:
                # the Editor is not focused while the harness drives it
                evaluate("UnityEngine.Application.runInBackground = true; return \"ticking\";")
            return status.get("playMode")
    raise SystemExit("play mode did not reach " + str(target))


def compile_project():
    """Force a script compile and surface any C# errors instead of hanging the loop."""
    if unwrap(cli("editor_status")).get("playMode") != "stopped":
        playmode(False)  # the Editor will not reload the domain during play mode
    cli("recompile")
    for _ in range(180):
        time.sleep(1)
        try:
            report = unwrap(cli("recompile_status"))
        except CommandError:
            continue
        if isinstance(report, str):
            report = json.loads(report)
        status = report.get("status")
        if status not in ("up_to_date", "completed", "idle", "failed", "success"):
            continue
        if report.get("failed") or report.get("errors"):
            raise SystemExit(json.dumps(report.get("errors"))[:4000])
        return status
    raise SystemExit("compile timed out")


def logs(level="error", tail=12):
    payload = unwrap(cli("console", "--tail", str(tail), "--level", level))
    lines = []
    for entry in payload.get("entries", []):
        lines.append(entry["level"][0].upper() + ": " + entry["message"].split("\n")[0][:220])
    return "\n".join(lines) or "(none)"


def main():
    if len(sys.argv) < 2:
        raise SystemExit(__doc__)
    verb, args = sys.argv[1], sys.argv[2:]
    if verb == "play":
        print(playmode(True))
    elif verb == "stop":
        print(playmode(False))
    elif verb == "state":
        print(state())
    elif verb == "key":
        press(args)
        print(state())
    elif verb == "steer":
        # steer W A S D... with a wait between each, printing state as it goes
        for token in args:
            if token.replace(".", "").isdigit():
                time.sleep(float(token))
            else:
                press([token])
        print(state())
    elif verb == "click":
        print(click(args[0]))
    elif verb == "shot":
        print(shot(*args))
    elif verb == "wait":
        time.sleep(float(args[0]))
        print(state())
    elif verb == "script":
        print(unwrap(cli("eval_file", "--file", args[0])))
    elif verb == "logs":
        print(logs(*args))
    elif verb == "status":
        print(json.dumps(unwrap(cli("editor_status")), indent=1))
    elif verb == "rebuild":
        playmode(False)
        compile_project()
        for attempt in range(4):
            try:
                print(unwrap(cli("eval_file", "--file", "Tools/Harness/rebuild.cs",
                                 "--timeout", "300", timeout=360)))
                return
            except CommandError as error:
                if attempt == 3:
                    raise SystemExit(str(error))
                time.sleep(4)  # the Editor drops requests while it is busy importing
    elif verb == "compile":
        print(compile_project())
    elif verb == "playtest":
        label = args[0] if args else "run"
        seconds = float(args[1]) if len(args) > 1 else 30
        every = float(args[2]) if len(args) > 2 else 2.5
        playmode(False)
        compile_project()
        playmode(True)
        time.sleep(1.5)
        print(evaluate(f'return GardenSnake.Editor.GardenPlaytest.Run("{label}", {seconds}f, {every}f);'))
        deadline = time.time() + seconds + 40
        while time.time() < deadline:
            time.sleep(4)
            report = evaluate("return GardenSnake.Editor.GardenPlaytest.Status();")
            if report and not report.startswith("running"):
                print(report)
                return
        raise SystemExit("playtest did not finish")
    elif verb == "recompile":
        print(compile_project())
    else:
        raise SystemExit("unknown verb " + verb)


if __name__ == "__main__":
    main()
