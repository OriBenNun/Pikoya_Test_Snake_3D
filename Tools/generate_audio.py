"""Original synthesized audio for Garden Snake; standard-library Python only.

Every clip is written from scratch here so the project ships no licensed audio.
The short effects share one voice: a few sine partials through an ADSR envelope
and a one-pole low pass, which keeps the set sounding like one instrument.
The ambient bed is built from frequencies snapped to the loop length, so it
repeats without a click.
"""
import math
import os
import struct
import wave

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
OUT = os.path.join(ROOT, 'Assets/GardenSnake/Audio')
os.makedirs(OUT, exist_ok=True)
RATE = 44100


def save(name, samples):
    frames = b''.join(
        struct.pack('<h', int(max(-1.0, min(1.0, value)) * 32767)) for value in samples)
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as audio:
        audio.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        audio.writeframes(frames)


def envelope(position, attack, decay, curve=1.8):
    """Fast attack, shaped release. Position runs 0..1 over the clip."""
    if position < attack:
        return position / attack
    tail = (position - attack) / max(1e-5, decay)
    return max(0.0, (1.0 - min(1.0, tail)) ** curve)


def low_pass(samples, cutoff):
    coefficient = math.exp(-2 * math.pi * cutoff / RATE)
    output = []
    previous = 0.0
    for value in samples:
        previous = value * (1 - coefficient) + previous * coefficient
        output.append(previous)
    return output


def pluck(notes, duration, volume=.45, partials=(1, .28, .12), attack=.008,
          decay=1.0, cutoff=5200, detune=.0):
    """A short melodic voice: notes are stepped through across the clip."""
    samples = []
    phases = [0.0] * len(partials)
    count = int(duration * RATE)
    for index in range(count):
        position = index / count
        frequency = notes[min(int(position * len(notes)), len(notes) - 1)]
        value = 0.0
        for partial, (harmonic, gain) in enumerate(zip(range(1, len(partials) + 1), partials)):
            phases[partial] += math.tau * frequency * harmonic * (1 + detune * partial) / RATE
            value += math.sin(phases[partial]) * gain
        samples.append(value * envelope(position, attack, decay) * volume)
    return low_pass(samples, cutoff)


def noise_tail(duration, volume, cutoff=900, seed=7):
    """Deterministic pseudo-noise so the build stays reproducible."""
    samples = []
    count = int(duration * RATE)
    state = seed
    for index in range(count):
        state = (state * 1103515245 + 12345) & 0x7fffffff
        value = (state / 0x3fffffff) - 1.0
        samples.append(value * envelope(index / count, .01, 1.0, 2.6) * volume)
    return low_pass(samples, cutoff)


def mix(*layers):
    length = max(len(layer) for layer in layers)
    output = [0.0] * length
    for layer in layers:
        for index, value in enumerate(layer):
            output[index] += value
    return output


def ambience(duration=12.0, volume=.16):
    """A slow chord that loops seamlessly: every partial fits the loop exactly."""
    count = int(duration * RATE)
    # A minor ninth spread over three octaves, snapped to whole cycles per loop.
    voices = []
    for target, gain, sway in ((110, .5, .9), (164.81, .34, .7), (220, .26, 1.3),
                               (329.63, .16, 1.7), (493.88, .09, 2.1)):
        cycles = max(1, round(target * duration))
        voices.append((cycles / duration, gain, max(1, round(sway * duration)) / duration))
    samples = []
    for index in range(count):
        time = index / RATE
        value = 0.0
        for frequency, gain, sway in voices:
            breath = .72 + .28 * math.sin(math.tau * sway * time)
            value += math.sin(math.tau * frequency * time) * gain * breath
        samples.append(value * volume)
    return low_pass(samples, 1400)


save('Pickup', pluck([784, 988, 1319], .2, .42, (1, .3, .16), decay=.95, cutoff=6200))
save('Turn', pluck([196, 262], .05, .16, (1, .1), attack=.003, decay=1.0, cutoff=1500))
save('Start', pluck([392, 523, 659, 784], .38, .34, (1, .26, .1), decay=1.0, cutoff=5000))
save('Best', pluck([659, 880, 1047, 1319, 1568], .55, .34, (1, .22, .12), decay=1.0, cutoff=7000))
save('Click', pluck([523, 659], .06, .2, (1, .12), attack=.002, decay=1.0, cutoff=3200))
save('Lose', mix(
    pluck([294, 247, 185, 139, 98], .62, .34, (1, .32, .18), decay=1.0, cutoff=2400, detune=.004),
    noise_tail(.62, .1)))
save('Ambience', ambience())
print('GARDEN_AUDIO_OK')
