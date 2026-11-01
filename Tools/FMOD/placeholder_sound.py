import math
import random
import struct
import sys
import wave

Rate = 44100


def blip():
    duration = 0.15
    for i in range(int(Rate * duration)):
        t = i / Rate
        yield math.sin(2 * math.pi * (880 - 2000 * t) * t) * math.exp(-t * 30)


def impact():
    noise = random.Random(1)
    duration = 0.35
    for i in range(int(Rate * duration)):
        t = i / Rate
        thump = math.sin(2 * math.pi * 60 * t) * math.exp(-t * 10)
        crack = (noise.random() * 2 - 1) * math.exp(-t * 25)
        yield thump * 0.8 + crack * 0.5


def whoosh():
    noise = random.Random(2)
    duration = 0.6
    filtered = 0.0
    for i in range(int(Rate * duration)):
        t = i / Rate
        swell = math.sin(math.pi * t / duration) ** 2
        cutoff = 0.02 + 0.2 * swell
        filtered += cutoff * ((noise.random() * 2 - 1) - filtered)
        yield filtered * swell * 4


def loop():
    duration = 1.0
    for i in range(int(Rate * duration)):
        t = i / Rate
        yield math.sin(2 * math.pi * 110 * t) * 0.7 + math.sin(2 * math.pi * 220 * t) * 0.2 + math.sin(2 * math.pi * 333 * t) * 0.1


Contours = {
    "alert": (0.45, lambda t: 1.0 + 0.5 * t, 0.0),
    "pain": (0.25, lambda t: 1.4 - 0.4 * t, 0.0),
    "death": (0.9, lambda t: 1.2 - 0.7 * t, 6.0),
    "grabbed": (0.6, lambda t: 1.3, 12.0),
    "attack": (0.2, lambda t: 1.0 + 0.2 * t, 0.0),
    "stagger": (0.7, lambda t: 1.5 - 0.8 * t, 9.0),
    "idle": (1.2, lambda t: 0.5 + 0.05 * math.sin(t * 9), 3.0),
    "feed": (0.8, lambda t: 0.7 - 0.2 * t, 20.0),
    "hiss": (0.7, lambda t: 1.0, 0.0),
}


def voice(shape, pitch, machine):
    duration, contour, vibrato = Contours[shape]
    noise = random.Random(3)
    phase = 0.0
    filtered = 0.0
    count = int(Rate * duration)
    for i in range(count):
        t = i / Rate
        u = i / count
        envelope = min(1.0, t / 0.02) * (1.0 - u) ** (0.6 if shape != "attack" else 2.0)
        frequency = pitch * contour(u) * (1.0 + 0.04 * math.sin(2 * math.pi * vibrato * t))
        phase += 2 * math.pi * frequency / Rate
        if shape == "hiss":
            filtered += 0.6 * ((noise.random() * 2 - 1) - filtered)
            yield ((noise.random() * 2 - 1) - filtered) * envelope
            continue
        if machine:
            tone = 1.0 if math.sin(phase) >= 0 else -1.0
            tone = round(tone * 0.5 + math.sin(phase * 3) * 0.3, 1)
        else:
            tone = sum(math.sin(phase * k) / k * (1.4 if k in (2, 3) else 1.0) for k in range(1, 7))
        if shape in ("idle", "feed"):
            filtered += 0.05 * ((noise.random() * 2 - 1) - filtered)
            tone = tone * 0.7 + filtered * 6
        yield tone * envelope


Generators = {"blip": blip, "impact": impact, "whoosh": whoosh, "loop": loop}


def main():
    kind, path = sys.argv[1], sys.argv[2]
    if kind == "voice":
        shape, pitch, machine = sys.argv[3], float(sys.argv[4]), len(sys.argv) > 5 and sys.argv[5] == "machine"
        samples = list(voice(shape, pitch, machine))
    else:
        samples = list(Generators[kind]())
    peak = max(abs(s) for s in samples) or 1.0
    with wave.open(path, "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(Rate)
        out.writeframes(b"".join(struct.pack("<h", int(s / peak * 0.7 * 32767)) for s in samples))


main()
