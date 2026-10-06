"""Original Spike notification: two soft, overlapping glass notes. No external samples."""
import math, struct, wave
from pathlib import Path
rate = 44100
samples = []
for i in range(int(rate * .85)):
    t = i / rate
    value = 0.0
    for start, frequency, gain in [(0, 659.255, .26), (.12, 987.767, .19)]:
        age = t - start
        if age >= 0:
            envelope = (1 - math.exp(-age * 150)) * math.exp(-age * 8) * min(1, max(0, (.85 - t) / .08))
            value += gain * envelope * (math.sin(2 * math.pi * frequency * age) + .12 * math.sin(2 * math.pi * frequency * 2 * age))
    samples.append(struct.pack('<h', round(value * 32767)))
with wave.open(str(Path(__file__).resolve().parents[1] / 'src/Spike.Desktop/Sounds/reminder.wav'), 'wb') as out:
    out.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
    out.writeframes(b''.join(samples))
