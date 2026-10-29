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


Generators = {"blip": blip, "impact": impact, "whoosh": whoosh, "loop": loop}


def main():
    kind, path = sys.argv[1], sys.argv[2]
    samples = list(Generators[kind]())
    peak = max(abs(s) for s in samples) or 1.0
    with wave.open(path, "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(Rate)
        out.writeframes(b"".join(struct.pack("<h", int(s / peak * 0.7 * 32767)) for s in samples))


main()
