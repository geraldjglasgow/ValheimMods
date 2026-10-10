#!/bin/bash
# probe.sh N : load a DevBridge build with today's Director code into the running game without a restart.
# Copies DevBridge to a scratch folder, renames it DevBridgeProbeN on port 7790+N (only the Director's patches applied),
# builds it, resets probe N-1's cast and loads probe N through the main bridge's /eval. Use port 7790+N for /shot,
# /studio/scene/* and /cast; screenshots and everything else stay on 7780. A game restart loads the real build.
N=${1:?usage: probe.sh N}
SCRATCH="${PROBE_SCRATCH:-$(cygpath -m "${TEMP:-/tmp}")/devbridge-probe}"
SRC="$(cd "$(dirname "$0")/../../DevBridge" && pwd)"
P="$SCRATCH/probe$N"
rm -rf "$P"; mkdir -p "$P/src"
(cd "$SRC" && tar --exclude=bin --exclude=obj -cf - .) | (cd "$P/src" && tar -xf -)
F="$P/src/DevBridge.cs"
sed -i "s/PluginGuid = \"com.DevBridge\"/PluginGuid = \"com.DevBridge.Probe$N\"/; s/PluginName = \"DevBridge\"/PluginName = \"DevBridgeProbe$N\"/" "$F"
sed -i "s/Config.Bind(\"Server\", \"Port\", 7780,/Config.Bind(\"Server\", \"Port\", $((7790+N)),/" "$F"
sed -i 's/            StudioKey.Bind(Config);//; s/            LogCapture.Install();//; s/            StudioKey.Check(Port);//' "$F"
sed -i 's/new Harmony(PluginGuid).PatchAll(typeof(DevBridgePlugin).Assembly);/var h = new Harmony(PluginGuid); foreach (var t in typeof(DevBridgePlugin).Assembly.GetTypes()) if (t.Namespace == "DevBridge.Director" \&\& t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0) h.CreateClassProcessor(t).Patch();/' "$F"
"/c/Program Files/dotnet/dotnet" build "$P/src/DevBridge.csproj" -c Release -nologo -v q -p:AssemblyName=DevBridgeProbe$N \
    -o "$P/out" -p:TestPluginsPath="$P/none" 2>&1 | grep -E "error|Build succeeded" | head -20
[ -f "$P/out/DevBridgeProbe$N.dll" ] || exit 1
curl -s -m 5 "http://127.0.0.1:$((7790+N-1))/cast?reset=1" > /dev/null
curl -s -G http://127.0.0.1:7780/eval --data-urlencode \
    "expr=\$go(\"BepInEx_Manager\").AddComponent(Assembly.LoadFile(\"$P/out/DevBridgeProbe$N.dll\").GetType(\"DevBridge.DevBridgePlugin\")).name" > /dev/null
sleep 1; curl -s -m 5 "http://127.0.0.1:$((7790+N))/help" | head -c 40; echo " <- probe $N on port $((7790+N))"
