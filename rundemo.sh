cd /c/mod/work/b28_0640
powershell -NoProfile -File kits/skatebird/check.ps1 -NoGame 2>&1 | tail -5
powershell -NoProfile -File kits/skatebird/play.ps1 -Mod C:/mod/work/b28_0640/release -Demo 2>&1 | tail -1
for i in $(seq 1 80); do grep -q SIGF_READY C:/mod/work/skatebird/game/BepInEx/LogOutput.log 2>/dev/null && break; sleep 3; done
sleep 3
echo -n 1 > C:/mod/work/skatebird-rec.txt
# screenshots at given offsets (seconds after demo start)
prev=0
for t in "$@"; do
  sleep $((t-prev)); prev=$t
  powershell -NoProfile -ExecutionPolicy Bypass -File C:/mod/repo/machine/showcase/showcase.ps1 -Game -Seconds 3 | tail -1
done
grep -n "SIGF demo\|SIGF wave\|SIGF_ERR\|Exception" C:/mod/work/skatebird/game/BepInEx/LogOutput.log | head -20
