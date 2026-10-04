cd /c/mod/work/b28_0640
powershell -NoProfile -File kits/skatebird/check.ps1 -NoGame 2>&1 | tail -3
powershell -NoProfile -File kits/skatebird/play.ps1 -Mod C:/mod/work/b28_0640/release -Demo 2>&1 | tail -1
for i in $(seq 1 80); do grep -q SIGF_READY C:/mod/work/skatebird/game/BepInEx/LogOutput.log 2>/dev/null && break; sleep 3; done
sleep 3
echo -n 1 > C:/mod/work/skatebird-rec.txt
python frames.py 46 1
grep -n "SIGF demo\|SIGF wave\|SIGF_ERR\|Exception" C:/mod/work/skatebird/game/BepInEx/LogOutput.log | head -20
python - <<'P'
from PIL import Image
import glob
fs=sorted(glob.glob('C:/mod/work/b28_0640/gen/frames/f*.png'))
for k in range(0,len(fs),12):
    part=fs[k:k+12]
    W,H=320,180
    sh=Image.new('RGB',(W*3,H*4))
    for i,f in enumerate(part): sh.paste(Image.open(f).resize((W,H)),((i%3)*W,(i//3)*H))
    sh.save('C:/mod/work/b28_0640/gen/frames/sheet%d.png'%(k//12))
P
