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
    data = payload.get("data", {})
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


PRELUDE = """
using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;
static Keyboard Board(){
  foreach (var device in InputSystem.devices) if (device.name == "HarnessKeyboard") return (Keyboard)device;
  InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
  return InputSystem.AddDevice<Keyboard>("HarnessKeyboard");
}
"""


def state():
    return evaluate(PRELUDE + """
var controller = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
if (controller == null) return "no-controller";
var game = controller.Game;
return string.Format("state={0} score={1} len={2} head={3},{4} food={5},{6} best={7} step={8:0.000}",
  game.State, game.Score, game.Body.Count, game.Body[0].X, game.Body[0].Y,
  game.Food.X, game.Food.Y, controller.Best, controller.StepSeconds);
""")


def press(names, hold=0.09):
    keys = " | ".join("Key." + name for name in names)
    evaluate(PRELUDE + f"""
var board = Board();
InputSystem.QueueStateEvent(board, new KeyboardState({keys}));
return "down";
""")
    time.sleep(hold)
    evaluate(PRELUDE + """
InputSystem.QueueStateEvent(Board(), new KeyboardState());
return "up";
""")


def click(name):
    return evaluate(f"""
using UnityEngine;
foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
  if (button.name == "{name}") {{ button.onClick.Invoke(); return "clicked {name}"; }}
return "missing {name}";
""")


def shot(name, width=1600, height=1000):
    path = f"Artifacts/shots/{name}.png"
    cli("capture_game_view", "--source", "screen", "--width", str(width),
        "--height", str(height), "--save_path", path)
    return path


def playmode(target):
    cli("editor_play" if target else "editor_stop")
    for _ in range(60):
        time.sleep(.5)
        status = unwrap(cli("editor_status"))
        if status.get("playMode") == ("playing" if target else "stopped"):
            return status.get("playMode")
    raise SystemExit("play mode did not reach " + str(target))


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
    elif verb == "recompile":
        cli("recompile")
        for _ in range(90):
            time.sleep(1)
            status = unwrap(cli("recompile_status"))
            if status.get("status") in ("idle", "completed", "done"):
                print(json.dumps(status)[:400])
                return
        raise SystemExit("recompile timed out")
    else:
        raise SystemExit("unknown verb " + verb)


if __name__ == "__main__":
    main()
