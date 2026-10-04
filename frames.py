import sys, time, os
from PIL import ImageGrab
out = 'C:/mod/work/b28_0640/gen/frames'
os.makedirs(out, exist_ok=True)
for f in os.listdir(out): os.remove(os.path.join(out, f))
n = int(sys.argv[1]); step = float(sys.argv[2])
t0 = time.time()
for i in range(n):
    im = ImageGrab.grab(all_screens=True)
    w, h = im.size
    # game window is the 960x540 (scaled) box at (160,130)-(1120,670) of a 1280x800 view
    sx, sy = w / 1280.0, h / 800.0
    im = im.crop((int(160 * sx), int(130 * sy), int(1120 * sx), int(670 * sy))).resize((640, 360))
    im.save('%s/f%02d.png' % (out, i))
    time.sleep(max(0, t0 + (i + 1) * step - time.time()))
