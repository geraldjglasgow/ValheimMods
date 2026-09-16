"""Clean-room guard: a PreToolUse hook that refuses tool calls reaching outside this repository.

Enforces CLEANROOM.md. Reads the hook payload on stdin and prints a PreToolUse deny decision when a
call would read another Valheim mod's code, decompile something that is not the game, or fetch code
from the network. Anything it does not recognise is allowed - it is a gate on known routes out, not a
whitelist of every safe call.
"""

import json
import re
import sys

# Places other people's mods live on this machine: every downloaded mod, every installed profile.
BLOCKED_PATHS = [
    r"r2modmanplus-local",
    r"bepinex[\\/]+plugins",
    r"thunderstore[\\/]+cache",
    r"\.quarantine",
    r"steamapps[\\/]+workshop",
]

# Decompilers and binary readers. Allowed only when aimed at the game's own assemblies.
DECOMPILERS = [r"ilspycmd", r"ilspy", r"dnspy", r"dotpeek", r"monodis", r"ikdasm", r"\bstrings\b"]
GAME_MANAGED = r"valheim_data[\\/]+managed"

# Network fetchers. Two hosts stay open because the release process in CLAUDE.md needs them.
FETCHERS = [
    r"\bcurl\b", r"\bwget\b", r"invoke-webrequest", r"\biwr\b", r"invoke-restmethod", r"\birm\b",
    r"git\s+clone", r"\bgh\s+(repo|api|release)",
]
ALLOWED_HOSTS = ["thunderstore.io", "nexusmods.com"]

PATH_KEYS = ["file_path", "path", "notebook_path"]


def deny(reason):
    """Refuse the call. The reason reaches the model, so it says what to do instead."""
    print(json.dumps({
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": reason,
        }
    }))
    sys.exit(0)


def blocked_path(text):
    """The first blocked location this text mentions, or None."""
    low = text.replace("/", "\\").lower()
    for pattern in BLOCKED_PATHS:
        if re.search(pattern, low):
            return pattern
    return None


def check_paths(tool_input):
    """File tools: refuse a path inside another mod's install."""
    candidates = [str(tool_input.get(k, "")) for k in PATH_KEYS]
    candidates.append(str(tool_input.get("pattern", "")))
    for value in candidates:
        if value and blocked_path(value):
            deny(
                "CLEANROOM.md: that path holds other people's Valheim mods. Their source, DLLs, "
                "configs and save data may never be read. Work from the spec in features/ and the "
                "game's own assemblies in valheim_Data/Managed."
            )


def check_decompiler(command):
    """A decompiler is fine on the game's assemblies and nowhere else."""
    low = command.lower()
    if not any(re.search(p, low) for p in DECOMPILERS):
        return
    if re.search(GAME_MANAGED, low.replace("/", "\\")):
        return
    if ".dll" in low or ".exe" in low:
        deny(
            "CLEANROOM.md: decompiling anything but the game's own assemblies is not allowed. "
            "Only assembly_valheim.dll, assembly_utils.dll and the Unity DLLs under "
            "valheim_Data/Managed may be decompiled, and only into the scratch directory."
        )


def check_fetch(command):
    """Network fetches: only the two stores the release process talks to."""
    low = command.lower()
    if not any(re.search(p, low) for p in FETCHERS):
        return
    if re.search(r"git\s+clone", low):
        deny("CLEANROOM.md: cloning another repository into this workspace is not allowed.")
    urls = re.findall(r"https?://([^/\s'\"]+)", low)
    if urls and all(any(h in u for h in ALLOWED_HOSTS) for u in urls):
        return  # the store APIs the release process needs
    deny(
        "CLEANROOM.md: no looking up code online. Not Stack Overflow, not GitHub, not a mod's page, "
        "wiki or docs. The specification and the game's own assemblies are the only sources. "
        "Only thunderstore.io and nexusmods.com are reachable, for publishing."
    )


def main():
    try:
        payload = json.load(sys.stdin)
    except Exception:
        return  # a payload we cannot read is not a reason to block work
    tool = payload.get("tool_name", "")
    tool_input = payload.get("tool_input", {}) or {}
    if tool in ("Bash", "PowerShell"):
        command = str(tool_input.get("command", ""))
        if blocked_path(command):
            deny(
                "CLEANROOM.md: that command reaches into another mod's install. Other people's mods "
                "may not be read, copied or decompiled - see 'What you may not read'."
            )
        check_decompiler(command)
        check_fetch(command)
        return
    check_paths(tool_input)


if __name__ == "__main__":
    main()
