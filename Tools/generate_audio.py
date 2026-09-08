"""Original short synthesized effects; standard-library Python only."""
import math
import os
import struct
import wave

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
OUT = os.path.join(ROOT, 'Assets/GardenSnake/Audio')
os.makedirs(OUT, exist_ok=True)
RATE = 44100

def write(name, notes, duration, volume=.45):
    samples = []
    phase = 0
    for i in range(int(duration * RATE)):
        t = i / RATE
        p = t / duration
        frequency = notes[min(int(p * len(notes)), len(notes) - 1)]
        phase += math.tau * frequency / RATE
        envelope = min(1, t / .006) * (1 - p) ** 1.7
        sound = (math.sin(phase) + .18 * math.sin(phase * 2)) * envelope * volume
        samples.append(struct.pack('<h', int(max(-1, min(1, sound)) * 32767)))
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as audio:
        audio.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        audio.writeframes(b''.join(samples))

write('Pickup', [660, 880, 1108, 1320], .22)
write('Turn', [240, 310], .045, .23)
write('Start', [392, 494, 587, 784], .32)
write('Lose', [294, 247, 196, 147, 98], .47)
print('GARDEN_AUDIO_OK')
