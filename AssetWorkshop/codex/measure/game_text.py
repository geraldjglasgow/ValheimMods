"""Text helpers for the game's Unity YAML files, shared by game.py and game_mesh.py: regex lookups at any depth,
indented blocks, hex fields."""
import re


def _all(text, pattern, multiline=True):
    return re.findall(pattern, text, re.MULTILINE if multiline else 0)


def _nested(text, *names):
    """The value of the last name, looked up inside each earlier name's block in turn."""
    at = 0
    for name in names:
        match = re.compile(rf"^\s*{name}: ?(.*)$", re.MULTILINE).search(text, at)
        if not match:
            return ""
        at = match.end()
    return match.group(1).strip()


def _section(text, name):
    """The block under 'name:' (m_Floats, m_Colors, m_ValidKeywords): its line and every deeper-indented line after."""
    match = re.search(rf"^( *){name}:.*$", text, re.MULTILINE)
    if not match:
        return ""
    indent, lines = len(match.group(1)), []
    for line in text[match.end() + 1:].splitlines():
        if line.strip() and len(line) - len(line.lstrip()) <= indent and not line.lstrip().startswith("- "):
            break
        lines.append(line)
    return "\n".join(lines)


def _hex(text, name):
    return bytes.fromhex(_nested(text, name))


def _vertex_count(text):
    found = _all(text, r"m_VertexCount: (\d+)")
    return found[0] if found else 0
