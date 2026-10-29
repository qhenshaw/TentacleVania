# Synthesizes four seamlessly looping placeholder music stems (drone, pulse, percussion, lead) for one track, so the
# layered music system can be heard before licensed stems exist. Every stem has exactly the same length (a whole
# number of bars); notes end before the loop point and the drone's frequencies complete whole cycles over the loop.
#
# Usage: python -I Tools/FMOD/placeholder_music.py <out dir> <track name> <bpm> <root hz> [bars] [style]
#   style: stage (default), boss (busier, darker) or endless (driving, brighter)
# Writes <out dir>/<track name>_1_Drone.wav, _2_Pulse.wav, _3_Percussion.wav and _4_Lead.wav (mono, 44.1 kHz).

import math
import os
import random
import struct
import sys
import wave

Rate = 44100
MinorScale = [0, 2, 3, 5, 7, 8, 10, 12]


def note(root, step):
    return root * 2 ** (step / 12)


def quantize(frequency, loop_seconds):
    return max(1, round(frequency * loop_seconds)) / loop_seconds


def drone(n, beat, root, style, rng):
    length = n / Rate
    tones = [(quantize(root / 2, length), 0.6), (quantize(note(root / 2, 7), length), 0.35), (quantize(root, length), 0.25)]
    if style == "boss":
        tones.append((quantize(note(root / 2, 1), length), 0.2))
    lfo = 2 / length
    out = [0.0] * n
    for i in range(n):
        t = i / Rate
        swell = 0.75 + 0.25 * math.sin(2 * math.pi * lfo * t)
        value = 0.0
        for frequency, amp in tones:
            phase = 2 * math.pi * frequency * t
            value += amp * (math.sin(phase) + 0.3 * math.sin(2 * phase) + 0.15 * math.sin(3 * phase))
        out[i] = value * swell
    return out


def add_tone(out, start, seconds, frequency, amp, decay, shape="sine"):
    count = min(int(seconds * Rate), len(out) - start)
    for j in range(count):
        t = j / Rate
        env = math.exp(-t * decay) * min(1.0, t * 400) * min(1.0, (count - j) / 200)
        phase = 2 * math.pi * frequency * t
        if shape == "square":
            wave_value = 1.0 if math.sin(phase) >= 0 else -1.0
            wave_value = 0.6 * wave_value + 0.4 * math.sin(phase)
        elif shape == "saw":
            wave_value = 2 * ((frequency * t) % 1.0) - 1
        else:
            wave_value = math.sin(phase)
        out[start + j] += wave_value * amp * env


def add_noise(out, start, seconds, amp, decay, rng, smooth=0.0):
    count = min(int(seconds * Rate), len(out) - start)
    filtered = 0.0
    for j in range(count):
        t = j / Rate
        env = math.exp(-t * decay) * min(1.0, (count - j) / 100)
        sample = rng.random() * 2 - 1
        filtered = smooth * filtered + (1 - smooth) * sample
        out[start + j] += filtered * amp * env


def pulse(n, beat, root, style, rng):
    out = [0.0] * n
    eighth = beat / 2
    steps = int(round(n / (eighth * Rate)))
    pattern = [0, 0, 12, 0, 0, 7, 0, 10] if style != "boss" else [0, 0, 1, 0, 0, 12, 0, 1]
    for k in range(steps):
        step = pattern[k % len(pattern)]
        if style == "endless" and k % 16 >= 8:
            step = [3, 3, 15, 3, 3, 10, 3, 12][k % 8]
        add_tone(out, int(k * eighth * Rate), eighth * 0.95, note(root / 2, step), 0.8, 9, "square")
    return out


def percussion(n, beat, root, style, rng):
    out = [0.0] * n
    sixteenth = beat / 4
    steps = int(round(n / (sixteenth * Rate)))
    for k in range(steps):
        start = int(k * sixteenth * Rate)
        in_bar = k % 16
        kick = in_bar in (0, 8) or (style == "boss" and in_bar in (3, 6, 11, 14)) or (style == "endless" and in_bar in (4, 12))
        if kick:
            count = min(int(0.25 * Rate), n - start)
            for j in range(count):
                t = j / Rate
                frequency = 45 + 90 * math.exp(-t * 30)
                out[start + j] += math.sin(2 * math.pi * frequency * t) * math.exp(-t * 14) * 1.0 * min(1.0, (count - j) / 100)
        if in_bar in (4, 12):
            add_noise(out, start, 0.18, 0.55, 22, rng, 0.35)
            add_tone(out, start, 0.12, 190, 0.3, 30)
        if in_bar % 2 == 1 or (style != "stage" and in_bar % 2 == 0 and in_bar not in (0, 8)):
            add_noise(out, start, 0.05, 0.22 if in_bar % 4 == 2 else 0.14, 70, rng, 0.0)
    return out


def lead(n, beat, root, style, rng):
    out = [0.0] * n
    sixteenth = beat / 4
    steps = int(round(n / (sixteenth * Rate)))
    phrase = [0, 3, 7, 10, 7, 3, 12, 10, 7, 5, 3, 7, 5, 3, 2, 0] if style != "boss" else [0, 1, 7, 8, 7, 1, 12, 13, 12, 8, 7, 8, 7, 1, 0, 1]
    if style == "endless":
        phrase = [0, 7, 12, 15, 12, 7, 10, 14, 12, 10, 7, 3, 5, 7, 10, 12]
    for k in range(steps):
        bar = k // 16
        if style == "stage" and k % 2 == 1:
            continue
        step = phrase[k % 16] + (5 if bar % 4 == 2 else 0) + (-2 if bar % 4 == 3 else 0)
        add_tone(out, int(k * sixteenth * Rate), sixteenth * 1.8, note(root * 2, step), 0.5, 7, "saw")
    return out


Stems = [("1_Drone", drone, 0.6), ("2_Pulse", pulse, 0.55), ("3_Percussion", percussion, 0.7), ("4_Lead", lead, 0.4)]


def write(path, samples, level):
    peak = max(abs(s) for s in samples) or 1.0
    with wave.open(path, "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(Rate)
        out.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s / peak * level)) * 32767)) for s in samples))


def main():
    out_dir, name, bpm, root = sys.argv[1], sys.argv[2], float(sys.argv[3]), float(sys.argv[4])
    bars = int(sys.argv[5]) if len(sys.argv) > 5 else 4
    style = sys.argv[6] if len(sys.argv) > 6 else "stage"
    beat = 60.0 / bpm
    n = int(round(bars * 4 * beat * Rate))
    os.makedirs(out_dir, exist_ok=True)
    for suffix, generator, level in Stems:
        samples = generator(n, beat, root, style, random.Random(sum(map(ord, suffix))))
        path = os.path.join(out_dir, f"{name}_{suffix}.wav")
        write(path, samples, level)
        print(path)


main()
