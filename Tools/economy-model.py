# Biomass economy model: reads every sector spec (waves, elites, caches, Mutagen, bosses) and the enemy/boss
# definitions' biomass and HP, then prints income per sector along the planned route, cumulative biomass, the stat
# level and size stage it buys, and the average enemy HP per sector. Per-sector scales (enemy health, damage and
# biomass, cache value; bosses use their own definition's biomass) come from Core/Progression/Resources/DifficultyCurve.asset, so the table shows
# the tuned game; --raw ignores them, and --solve prints the enemy biomass scale that would hit each sector's Target.
#
# Usage: python -I Tools/economy-model.py [--raw] [--solve]
import glob, json, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ROUTE = ['SH', 'AD', 'RD', 'BL', 'QU', 'CS', 'BA', 'AR', 'CC', 'DC', 'BS', 'MV']
ELITE_HEALTH = 1.8
ELITE_BIOMASS = 3.0
SIZE = [(30000, 'Apex'), (10000, 'Brute'), (2500, 'Stalker'), (0, 'Hatchling')]


def read_assets(pattern):
    out = {}
    for path in glob.glob(os.path.join(ROOT, pattern), recursive=True):
        text = open(path, encoding='utf-8', errors='ignore').read()
        name = re.search(r'm_Name: (\S+)', text)
        hp = re.search(r'_maxHealth: ([\d.]+)', text)
        bio = re.search(r'_biomass: (\d+)', text)
        if name and hp and bio:
            out[name.group(1)] = (float(hp.group(1)), int(bio.group(1)))
    return out


def read_curve():
    path = os.path.join(ROOT, 'Assets/Game/Core/Progression/Resources/DifficultyCurve.asset')
    if not os.path.exists(path):
        return {}
    text = open(path, encoding='utf-8').read()
    out = {}
    for block in re.split(r'\n  - ', text)[1:]:
        fields = dict(re.findall(r'(\w+): ([\w.]+)', block))
        out[fields['Sector']] = {'health': float(fields['Health']), 'damage': float(fields['Damage']), 'biomass': float(fields['Biomass']),
                                 'cache': int(fields['Cache']), 'target': int(fields.get('Target', 0))}
    return out


def level_cost(level):
    return round(60 + 12 * max(0, level) ** 1.4)


def levels_for(spent):
    level, total = 4, 0
    while total + level_cost(level) <= spent and level < 240:
        total += level_cost(level)
        level += 1
    return level


def main():
    raw = '--raw' in sys.argv
    defs = read_assets('Assets/Game/Enemies/Data/Enemies/*.asset')
    defs.update(read_assets('Assets/Game/Bosses/**/*.asset'))
    curve = {} if raw else read_curve()
    bosses = set(read_assets('Assets/Game/Bosses/**/*.asset'))
    solve = '--solve' in sys.argv
    sectors = {}
    for path in glob.glob(os.path.join(ROOT, 'Assets/Game/World/Data/Sectors/*.sector.json')):
        spec = json.load(open(path, encoding='utf-8'))
        sectors[spec['sector']] = spec
    print(f"{'Sector':6} {'Enemies':>7} {'Elites':>6} {'Biomass':>8} {'Caches':>6} {'Cache bm':>8} {'Mutagen':>7} {'Total':>7} {'Cumul.':>8} {'Level':>5} {'Size':>9} {'Avg HP':>7} {'HPx':>5} {'Dmgx':>5}")
    cumulative = 0
    missing = set()
    for code in ROUTE:
        spec = sectors.get(code)
        if spec is None:
            continue
        scale = curve.get(code, {})
        bio_scale = scale.get('biomass', 1.0)
        cache_value = scale.get('cache', 250)
        hp_scale = scale.get('health', 1.0)
        enemies = elites = biomass = caches = mutagen = 0
        boss_biomass = enemy_raw = 0
        hp_total = 0.0
        for room in spec['rooms']:
            for wave in room.get('waves', []):
                for entry in wave:
                    name = entry['enemy']
                    count = entry.get('count', 1)
                    hp, bio = defs.get(name, (0, 0))
                    if name not in defs:
                        missing.add(name)
                    elite = entry.get('elite', False)
                    if name in bosses:
                        boss_biomass += count * bio
                        continue
                    enemy_raw += count * bio * (ELITE_BIOMASS if elite else 1)
                    enemies += count
                    elites += count if elite else 0
                    biomass += count * bio * (ELITE_BIOMASS if elite else 1) * bio_scale
                    hp_total += count * hp * (ELITE_HEALTH if elite else 1) * hp_scale
            for place in room.get('place', []):
                prefab = place.get('prefab', '')
                if prefab == 'BiomassCache':
                    caches += 1
                elif prefab == 'MutagenPickup':
                    mutagen += 1
        cache_biomass = caches * cache_value
        biomass += boss_biomass
        total = int(biomass + cache_biomass)
        if solve and scale.get('target'):
            print(f"  {code}: enemy biomass scale {(scale['target'] - boss_biomass - cache_biomass) / max(1, enemy_raw):.2f} for target {scale['target']}")
        cumulative += total
        level = levels_for(cumulative)
        size = next(name for threshold, name in SIZE if cumulative >= threshold)
        avg = hp_total / enemies if enemies else 0
        print(f"{code:6} {enemies:7d} {elites:6d} {int(biomass):8d} {caches:6d} {cache_biomass:8d} {mutagen:7d} {total:7d} {cumulative:8d} {level:5d} {size:>9} {avg:7.0f} {hp_scale:5.2f} {scale.get('damage', 1.0):5.2f}")
    if missing:
        print('no definition for:', ', '.join(sorted(missing)))
    print('Level = total stat level if every biomass went into stats (start 4); the design expects ~60 by the end.')


if __name__ == '__main__':
    main()
