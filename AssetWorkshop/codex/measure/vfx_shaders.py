"""The game's particle shaders as the game ships them, for codex/vfx/shaders.md: each shader's passes (blend, depth
write, culling, queue, light mode) read from the game's own SoftRef bundles, and optionally its Direct3D 11 programs
disassembled into codex/out/vfx/shaders/ (gitignored) to read what it computes. Needs UnityPy (pip install UnityPy,
anywhere on the path or in --lib) and, for --disassemble, Windows' d3dcompiler_47.dll.

    python codex/measure/vfx_shaders.py [--lib <folder with UnityPy>] [--disassemble]
"""
import argparse
import ctypes
import os
import struct
import sys

BUNDLES = r"C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\StreamingAssets\SoftRef\Bundles"
SHADER_BUNDLES = ("c4210710", "2c2cce25")
WANTED = ("Particle", "Lux", "Decal", "Distortion", "Blob", "Flipbook")
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "out", "vfx", "shaders")
D3D11 = 4


def shaders(unitypy):
    """(name, reader object) of every particle shader in the game's shader bundles."""
    env = unitypy.Environment()
    for name in SHADER_BUNDLES:
        env.load_file(os.path.join(BUNDLES, name))
    for obj in env.objects:
        if obj.type.name == "Shader":
            tree = obj.read_typetree()
            if any(w in tree["m_ParsedForm"]["m_Name"] for w in WANTED):
                yield tree["m_ParsedForm"]["m_Name"], obj, tree


def states(tree):
    """One line per pass: blend factors (a number, or the property that sets it), ZWrite, Cull, tags."""
    def value(v):
        return v["name"] if v.get("name") not in (None, "<noninit>") else int(v["val"])
    lines = []
    for sub in tree["m_ParsedForm"]["m_SubShaders"]:
        for p in sub["m_Passes"]:
            st = p["m_State"]
            blend = st["rtBlend0"] if "rtBlend0" in st else st["rtBlend"][0]
            tags = dict(p["m_State"]["m_Tags"]["tags"]) if "m_Tags" in st else {}
            lines.append(f"  pass {st.get('m_Name') or '-'}: Blend {value(blend['srcBlend'])} {value(blend['destBlend'])}, "
                         f"ZWrite {value(st['zWrite'])}, Cull {value(st['culling'])}, ZTest {value(st['zTest'])}, {tags}")
    return lines


def disassemble(tree, path, compression):
    """Every Direct3D 11 program of the shader, disassembled, into one text file."""
    d3d = ctypes.WinDLL("d3dcompiler_47.dll")
    blob, texts = bytes(tree["compressedBlob"]), []
    for i, platform in enumerate(tree["platforms"]):
        if platform != D3D11:
            continue
        first = lambda v: v[0] if isinstance(v, list) else v
        offset, size, full = first(tree["offsets"][i]), first(tree["compressedLengths"][i]), first(tree["decompressedLengths"][i])
        raw = compression.decompress_lz4(blob[offset:offset + size], full)
        at = raw.find(b"DXBC")
        while at >= 0:
            length = struct.unpack("<I", raw[at + 24:at + 28])[0]
            texts.append(_disassembly(d3d, raw[at:at + length]))
            at = raw.find(b"DXBC", at + length)
    with open(path, "w", encoding="utf-8") as handle:
        handle.write("\n".join(texts).replace("\0", ""))


def _disassembly(d3d, program):
    result = ctypes.c_void_p()
    if d3d.D3DDisassemble(ctypes.c_char_p(program), ctypes.c_size_t(len(program)), 0, None, ctypes.byref(result)):
        return "(disassembly failed)"
    table = ctypes.cast(ctypes.cast(result, ctypes.POINTER(ctypes.c_void_p))[0], ctypes.POINTER(ctypes.c_void_p))
    pointer = ctypes.WINFUNCTYPE(ctypes.c_void_p, ctypes.c_void_p)(table[3])(result)
    size = ctypes.WINFUNCTYPE(ctypes.c_size_t, ctypes.c_void_p)(table[4])(result)
    return ctypes.string_at(pointer, size).decode("latin1")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--lib", help="a folder holding UnityPy (pip install --target)")
    parser.add_argument("--disassemble", action="store_true")
    args = parser.parse_args()
    if args.lib:
        sys.path.append(args.lib)
    import UnityPy
    from UnityPy.helpers import CompressionHelper
    os.makedirs(OUT, exist_ok=True)
    for name, _, tree in shaders(UnityPy):
        print(name)
        print("\n".join(states(tree)))
        if args.disassemble:
            disassemble(tree, os.path.join(OUT, name.replace("/", "_").replace(" ", "") + ".txt"), CompressionHelper)


if __name__ == "__main__":
    main()
