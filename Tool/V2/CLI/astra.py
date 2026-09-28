#!/usr/bin/env python3
"""Astra Performance V2 automation. Python 3.9+, standard library only. stdout is JSON."""
import argparse
import copy
import json
import math
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import uuid

HERE = Path(__file__).resolve().parent
DEFAULT_PROJECT = HERE.parent / "UnityProject"
ENUMS = {
    "node": ["Unit", "Condition", "GlobalChoice", "End"],
    "dialogue": ["Text", "Choice"],
    "compare": ["Equal", "NotEqual", "Greater", "GreaterOrEqual", "Less", "LessOrEqual"],
    "shot": ["Establishing", "Medium", "Close", "TwoShot", "OverShoulder", "Profile", "LowAngle", "Insert", "Tracking", "Custom"],
    "fx": ["None", "SignalGlitch", "Disturbance", "EmergencyLight"],
}
TEMPLATES = {"SingleCall": "单人来电", "Conversation": "双人对话", "Question": "交互问答", "Broadcast": "纯播报", "Blank": "空白演出"}
PARAMS = {"sceneId", "opening", "reply", "question", "acceptLabel", "declineLabel", "acceptReply", "declineReply", "silenceReply", "replySeconds"}


class CliError(Exception):
    def __init__(self, code, message, issues=None, data=None):
        super().__init__(message)
        self.code, self.issues, self.data = code, issues or [], data


def read_json(path):
    def pairs(items):
        result = {}
        for key, value in items:
            if key in result:
                raise CliError("JSON_INVALID", "Duplicate JSON key: " + key)
            result[key] = value
        return result
    try:
        return json.loads(Path(path).read_text(encoding="utf-8-sig"), object_pairs_hook=pairs,
                          parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Non-finite number: " + value)))
    except (ValueError, OSError) as exc:
        raise CliError("JSON_INVALID", f"{path}: {exc}") from exc


def write_json(path, data, overwrite=False):
    path = Path(path).resolve()
    path.parent.mkdir(parents=True, exist_ok=True)
    # Exclusive create avoids accidental replacement of authored specs.
    with path.open("w" if overwrite else "x", encoding="utf-8", newline="\n") as stream:
        json.dump(data, stream, ensure_ascii=False, indent=2, allow_nan=False)
        stream.write("\n")


def schema_errors(value, schema, root=None, path=""):
    """Validator for the exact JSON Schema subset used by performance.schema.json."""
    root = root or schema
    if "$ref" in schema:
        schema = root["$defs"][schema["$ref"].split("/")[-1]]
    errors = []
    def fail(message):
        errors.append({"code": "SCHEMA", "path": path or "/", "message": message, "severity": "error"})
    kind = schema.get("type")
    types = {"object": lambda: isinstance(value, dict), "array": lambda: isinstance(value, list),
             "string": lambda: isinstance(value, str), "boolean": lambda: type(value) is bool,
             "integer": lambda: type(value) is int, "number": lambda: type(value) in (int, float) and math.isfinite(value)}
    if kind and not types[kind]():
        fail("Expected " + kind)
        return errors
    if "enum" in schema and value not in schema["enum"]:
        fail("Expected one of: " + str(schema["enum"]))
    if kind == "object":
        for key in schema.get("required", []):
            if key not in value:
                fail("Missing field: " + key)
        for key, item in value.items():
            if key not in schema.get("properties", {}):
                if schema.get("additionalProperties") is False:
                    fail("Unknown field: " + key)
            else:
                errors += schema_errors(item, schema["properties"][key], root, path + "/" + key)
    if kind == "array":
        if len(value) < schema.get("minItems", 0) or len(value) > schema.get("maxItems", 1000000):
            fail("Array size outside allowed range")
        for index, item in enumerate(value):
            errors += schema_errors(item, schema["items"], root, path + "/" + str(index))
    if kind == "string":
        if len(value) < schema.get("minLength", 0):
            fail("Must not be empty")
        if "pattern" in schema and not re.search(schema["pattern"], value):
            fail("Does not match " + schema["pattern"])
    if kind in ("integer", "number"):
        if value < schema.get("minimum", -math.inf) or value > schema.get("maximum", math.inf):
            fail("Number outside allowed range")
        if kind == "number" and abs(value) > 3.4028234e38:
            fail("Number exceeds Unity float range")
        if "exclusiveMinimum" in schema and value <= schema["exclusiveMinimum"]:
            fail("Must be greater than " + str(schema["exclusiveMinimum"]))
    return errors


def check_spec(spec):
    errors = schema_errors(spec, read_json(HERE / "performance.schema.json"))
    if errors:
        raise CliError("SCHEMA_INVALID", "Correct the JSON fields listed in issues.", errors)


def convert_spec(spec, to_wire):
    spec = copy.deepcopy(spec)
    def enum(obj, key, group):
        values = ENUMS[group]
        obj[key] = values.index(obj[key]) if to_wire else values[obj[key]]
    def condition(obj):
        enum(obj, "compare", "compare")
    def choices(items):
        for item in items:
            condition(item["condition"])
    for node in spec["nodes"]:
        enum(node, "kind", "node")
        condition(node["condition"])
        choices(node["choices"])
    for unit in spec["units"]:
        for dialogue in unit["dialogue"]:
            enum(dialogue, "kind", "dialogue")
            choices(dialogue["choices"])
        for camera in unit["cameras"]:
            enum(camera, "shot", "shot")
            enum(camera, "fx", "fx")
    # JsonUtility may emit null for unused string fields; the public contract uses empty strings.
    def clean(value):
        if isinstance(value, dict):
            return {key: clean(item) for key, item in value.items()}
        if isinstance(value, list):
            return [clean(item) for item in value]
        return "" if value is None else value
    return clean(spec)


def find_unity(project, supplied=None):
    if supplied or os.environ.get("UNITY_EDITOR_PATH"):
        path = Path(supplied or os.environ["UNITY_EDITOR_PATH"])
        if not path.is_file():
            raise CliError("UNITY_NOT_FOUND", str(path))
        return path.resolve()
    version_file = project / "ProjectSettings/ProjectVersion.txt"
    match = re.search(r"m_EditorVersion: (\S+)", version_file.read_text(encoding="utf-8"))
    version = match.group(1)
    for base in [Path("F:/Unity/Installs"), Path(os.environ.get("ProgramFiles", "C:/Program Files")) / "Unity/Hub/Editor"]:
        path = base / version / "Editor/Unity.exe"
        if path.is_file():
            return path
    raise CliError("UNITY_NOT_FOUND", "Set --unity or UNITY_EDITOR_PATH to Unity.exe (" + version + ")")


def heartbeat(project):
    path = project / "Library/AstraCli/heartbeat.json"
    try:
        if time.time() - path.stat().st_mtime > 8:
            return None
        data = read_json(path)
        if Path(data["project"]).resolve() == project and data["version"] == 1:
            return data
    except (OSError, CliError, KeyError):
        pass
    return None


def invoke(args, request):
    project = Path(args.project).resolve()
    if not (project / "Assets/AstraToolV2/Editor/ToolV2Cli.cs").is_file():
        raise CliError("BRIDGE_MISSING", "ToolV2Cli.cs not installed in " + str(project))
    queue = project / "Library/AstraCli"
    queue.mkdir(parents=True, exist_ok=True)
    key = uuid.uuid4().hex
    request.update(version=1, id=key)
    beat = heartbeat(project) if args.transport != "batch" else None
    mode = "editor" if beat else "batch"
    if args.transport == "editor" and not beat:
        raise CliError("EDITOR_UNAVAILABLE", "Open the Tool Unity project, exit Play mode, and wait for script compilation.")
    process = None
    if mode == "editor":
        request["token"] = beat["token"]
        path = queue / (key + ".request.json")
        response_path = queue / (key + ".response.json")
    else:
        if (project / "Temp/UnityLockfile").exists():
            raise CliError("PROJECT_BUSY", "Unity owns this project but its CLI bridge is not ready. Exit Play mode and wait for compilation; do not launch a second editor.")
        unity = find_unity(project, args.unity)
        path = queue / (key + ".batch.json")
        response_path = Path(str(path) + ".response.json")
    temp = Path(str(path) + ".tmp")
    write_json(temp, request)
    os.replace(temp, path)
    log = queue / (key + ".log")
    if mode == "batch":
        flags = subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0
        process = subprocess.Popen([str(unity), "-batchmode", "-nographics", "-projectPath", str(project),
                                    "-executeMethod", "ToolV2Cli.Batch", "-astraRequest", str(path), "-logFile", str(log)],
                                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, creationflags=flags)
    deadline = time.monotonic() + args.timeout
    while time.monotonic() < deadline:
        if response_path.exists():
            result = read_json(response_path)
            if process:
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    pass
            result["transport"] = mode
            result["requestId"] = key
            if process:
                result["log"] = str(log)
            data = result.pop("dataJson", None)
            result["data"] = json.loads(data) if data else None
            if result["ok"] and args.command in ("template", "inspect", "validate"):
                result["data"] = convert_spec(result["data"], False)
                if args.command != "inspect":
                    check_spec(result["data"])
            return result
        if process and process.poll() is not None:
            raise CliError("UNITY_FAILED", "Unity exited before returning JSON; inspect log.", data={"log": str(log), "exitCode": process.returncode})
        time.sleep(0.2)
    # Remove only an unclaimed inbox request; never kill the user's editor or retry a mutation.
    if mode == "editor":
        try:
            path.unlink()
            raise CliError("REQUEST_CANCELLED", "Editor did not claim the request before timeout. No operation started.")
        except FileNotFoundError:
            pass
    raise CliError("REQUEST_PENDING", "Operation may still complete. Inspect responseFile before retrying; do not repeat apply/export blindly.",
                   data={"requestId": key, "responseFile": str(response_path), "log": str(log), "pid": process.pid if process else beat["pid"]})


class Parser(argparse.ArgumentParser):
    def error(self, message):
        raise CliError("USAGE", message)


def parser():
    p = Parser(description=__doc__)
    p.add_argument("--project", default=str(DEFAULT_PROJECT))
    p.add_argument("--unity")
    p.add_argument("--transport", choices=["auto", "editor", "batch"], default="auto")
    p.add_argument("--timeout", type=float, default=240)
    sub = p.add_subparsers(dest="command", required=True)
    for name in ["doctor", "schema", "templates", "catalog", "template", "inspect", "validate", "apply", "export", "simulate"]:
        cmd = sub.add_parser(name)
        cmd.add_argument("--out", help="Write returned data to a NEW JSON file (never overwrite)")
        if name in ("catalog", "template"):
            cmd.add_argument("--library", default="Assets/AstraToolV2/Samples/Library.asset")
        if name == "template":
            cmd.add_argument("--kind", choices=list(TEMPLATES), default="Question")
            cmd.add_argument("--name", required=True)
            cmd.add_argument("--speaker")
            cmd.add_argument("--partner")
            cmd.add_argument("--params", help="JSON text/scene/timing overrides; see agent-guide.md")
        if name in ("validate", "simulate"):
            source = cmd.add_mutually_exclusive_group(required=True)
            source.add_argument("--input")
            source.add_argument("--package")
        if name in ("inspect", "export"):
            cmd.add_argument("--package", required=True)
        if name == "apply":
            cmd.add_argument("--input", required=True)
            cmd.add_argument("--dest", required=True, help="New Assets/.../Version folder; existing folders rejected")
        if name == "export":
            cmd.add_argument("--dest", required=True, help="New/empty directory outside UnityProject")
            cmd.add_argument("--id", required=True)
        if name == "simulate":
            cmd.add_argument("--route", default="", help="Comma-separated choice IDs; omitted decisions time out via silence")
            cmd.add_argument("--wait", type=float, default=0, help="Seconds before each explicit choice")
    return p


def main(argv=None):
    try:
        args = parser().parse_args(argv)
        if args.timeout <= 0 or not math.isfinite(args.timeout):
            raise CliError("USAGE", "timeout must be finite and positive")
        if args.out and Path(args.out).exists():
            raise CliError("OUTPUT_EXISTS", "Choose a new --out file: " + args.out)
        if args.command in ("doctor", "schema", "templates"):
            if args.command == "schema":
                data = read_json(HERE / "performance.schema.json")
            elif args.command == "templates":
                data = {"templates": TEMPLATES, "params": sorted(PARAMS)}
            else:
                project = Path(args.project).resolve()
                data = {"python": sys.version.split()[0], "project": str(project),
                        "bridgeInstalled": (project / "Assets/AstraToolV2/Editor/ToolV2Cli.cs").exists(),
                        "editorReady": bool(heartbeat(project)), "projectLocked": (project / "Temp/UnityLockfile").exists()}
                try:
                    data["unity"] = str(find_unity(project, args.unity))
                except (CliError, OSError) as exc:
                    data["unityError"] = str(exc)
            result = {"version": 1, "ok": True, "code": "OK", "issues": [], "data": data}
        else:
            request = {"command": args.command}
            for key in ("package", "library", "speaker", "partner", "name"):
                if getattr(args, key, None):
                    request[key] = getattr(args, key)
            if getattr(args, "input", None):
                spec = read_json(args.input)
                check_spec(spec)
                request["specJson"] = json.dumps(convert_spec(spec, True), ensure_ascii=False, allow_nan=False)
            if args.command == "template":
                request["template"] = args.kind
                if args.params:
                    params = read_json(args.params)
                    if not isinstance(params, dict) or set(params) - PARAMS:
                        raise CliError("PARAMS_INVALID", "Only these fields are supported: " + ", ".join(sorted(PARAMS)))
                    for key, value in params.items():
                        if key == "replySeconds":
                            if type(value) not in (int, float) or not math.isfinite(value) or not 1 <= value <= 120:
                                raise CliError("PARAMS_INVALID", "replySeconds must be 1..120")
                        elif not isinstance(value, str):
                            raise CliError("PARAMS_INVALID", key + " must be text")
                    request["paramsJson"] = json.dumps(params, ensure_ascii=False)
            if args.command == "apply":
                request["destination"] = args.dest
            if args.command == "export":
                if not re.fullmatch(r"[A-Za-z0-9_-]{1,64}", args.id):
                    raise CliError("INVALID_ID", "Use 1..64 ASCII letters, numbers, _ or -")
                request.update(output=str(Path(args.dest).resolve()), performanceId=args.id)
            if args.command == "simulate":
                if not math.isfinite(args.wait) or args.wait < 0:
                    raise CliError("USAGE", "wait must be finite and non-negative")
                request.update(route=[part.strip() for part in args.route.split(",") if part.strip()], waitSeconds=args.wait)
            result = invoke(args, request)
        if result["ok"] and args.out:
            write_json(args.out, result["data"])
            result["outputFile"] = str(Path(args.out).resolve())
        print(json.dumps(result, ensure_ascii=False, allow_nan=False))
        return 0 if result["ok"] else 2
    except (CliError, OSError, ValueError, KeyError, IndexError) as exc:
        print(json.dumps({"version": 1, "ok": False, "code": getattr(exc, "code", "CLI_ERROR"), "message": str(exc),
                          "issues": getattr(exc, "issues", []), "data": getattr(exc, "data", None)}, ensure_ascii=False))
        return 2


if __name__ == "__main__":
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main())
