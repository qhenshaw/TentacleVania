# Run economy model (v2, roguelike): expected biomass and Essence per run floor and per run, and how many runs it
# takes to buy out Mother's Call. Floor enemy pools, boss rooms and floor counts come from the RunKit asset through the
# open Unity Editor (unity CLI); enemy and boss biomass from their definitions (times RunSession.EnemyBiomassScale),
# unlock prices from MotherCallStock.asset. Wave sizes, door counts, reward weights and
# amounts mirror Core/Runs (RunWaves, RunGenerator, RunSession, MotherCall, RunShop): update the constants below
# when those change. Assumes the player takes doors in proportion to their reward weights.
#
# Usage: python -I Tools/run-economy.py
# Exits 2 when the editor can't be reached.

import glob
import json
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
COMBAT_ROOMS, ELITE_ROOMS = 8, 2
ELITE_BIOMASS = 3.0
BIOMASS_REWARD, ESSENCE_REWARD, FLOOR_ESSENCE = 150, 30, 60
ELITE_ESSENCE, BOSS_ESSENCE, ENEMY_BIOMASS_SCALE = 10, 40, 0.15
FLOOR_WEIGHTS = [(40, 20, 25, 15), (38, 22, 22, 18), (35, 22, 20, 23), (32, 20, 18, 30)]
RANK_BASE, RANK_STEP, MAX_RANK, STATS = 25, 15, 10, 4
HEAL_PRICE, MUTATION_PRICES = 150, (120, 250, 400)

KIT_EVAL = r'''
var kit = UnityEditor.AssetDatabase.LoadAssetAtPath<TentacleVania.World.Run.RunKit>("Assets/Game/World/Run/Data/RunKitPrototype.asset");
var so = new UnityEditor.SerializedObject(kit);
var floors = so.FindProperty("_floors");
var parts = new System.Collections.Generic.List<string>();
for (var f = 0; f < floors.arraySize; f++)
{
    var fp = floors.GetArrayElementAtIndex(f);
    string Names(string field)
    {
        var list = fp.FindPropertyRelative(field);
        var names = new System.Collections.Generic.List<string>();
        for (var i = 0; i < list.arraySize; i++)
        {
            var o = list.GetArrayElementAtIndex(i).objectReferenceValue as TentacleVania.Enemies.Enemy;
            if (o != null) names.Add(o.Definition != null ? o.Definition.name : o.name);
        }
        return string.Join(",", names);
    }
    parts.Add(fp.FindPropertyRelative("Floor").intValue + "|" + fp.FindPropertyRelative("Sector").stringValue + "|" + Names("Enemies") + "|" + Names("Prey"));
}
return kit.FloorCount + ";" + string.Join(";", parts);
'''


def query_kit():
    path = os.path.join(ROOT, 'Temp', 'run-economy-kit.cs')
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(KIT_EVAL)
    try:
        out = subprocess.run(['unity', 'command', '--caller', 'plugin', '--skill', 'unity-cli', '--result-only', 'eval_file', '--file', path],
                             capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=60, shell=os.name == 'nt').stdout
    except Exception:
        return None
    match = re.search(r'"result": "([^"]*)"', out)
    if not match:
        return None
    head, *floors = match.group(1).split(';')
    pools = {}
    for entry in floors:
        floor, sector, enemies, prey = entry.split('|')
        pools[int(floor)] = (sector, [e for e in enemies.split(',') if e], [p for p in prey.split(',') if p])
    return int(head), pools


def read_biomass(pattern):
    out = {}
    for path in glob.glob(os.path.join(ROOT, pattern), recursive=True):
        text = open(path, encoding='utf-8', errors='ignore').read()
        name = re.search(r'm_Name: (\S+)', text)
        bio = re.search(r'_biomass: (\d+)', text)
        if name and bio:
            out[name.group(1)] = int(bio.group(1))
    return out


def read_stock():
    text = open(os.path.join(ROOT, 'Assets/Game/Core/Runs/Resources/MotherCallStock.asset'), encoding='utf-8').read()
    return [int(c) for c in re.findall(r'Cost: (\d+)', text)]


def enemies_in_room(step, elite):
    waves = 2 if elite or step < 4 else 3
    return sum(min(8, 2 + step // 3 + w) for w in range(waves)), waves


def main():
    kit = query_kit()
    if kit is None:
        print('The Unity Editor could not be reached (the kit pools come from the open project).')
        return 2
    floor_count, pools = kit
    defs = read_biomass('Assets/Game/Enemies/Data/Enemies/*.asset')
    bosses = read_biomass('Assets/Game/Bosses/**/*.asset')
    boss_avg = sum(bosses.values()) / max(1, len(bosses))
    print(f"{'Floor':5} {'Sector':6} {'Enemies':>7} {'Enemy bm':>8} {'Door bm':>7} {'Boss bm':>7} {'Biomass':>7} {'Essence':>7} {'Cumul. E':>8}")
    total_essence = 0
    per_floor = []
    for floor in range(floor_count):
        sector, enemies, prey = pools.get(floor, pools[min(pools)])
        scale = ENEMY_BIOMASS_SCALE
        avg = sum(defs.get(e, 0) for e in enemies) / max(1, len(enemies))
        prey_avg = sum(defs.get(p, 0) for p in prey) / max(1, len(prey))
        count, enemy_bm = 0, 0.0
        steps = list(range(1, 12))
        elite_steps = {5, 8}
        for step in steps[:COMBAT_ROOMS + ELITE_ROOMS]:
            elite = step in elite_steps
            n, waves = enemies_in_room(step, elite)
            count += n + waves
            enemy_bm += (n * avg + waves * prey_avg + (waves * avg * (ELITE_BIOMASS - 1) if elite else 0)) * scale
        m, e, b, s = FLOOR_WEIGHTS[min(floor, 3)]
        total = m + e + b + s
        door_bm = COMBAT_ROOMS * BIOMASS_REWARD * b / total
        door_es = COMBAT_ROOMS * ESSENCE_REWARD * s / total
        elites = ELITE_ROOMS * 2
        essence = door_es + FLOOR_ESSENCE + elites * ELITE_ESSENCE + BOSS_ESSENCE
        biomass = enemy_bm + door_bm + boss_avg * ENEMY_BIOMASS_SCALE
        total_essence += essence
        per_floor.append(essence)
        print(f"L{floor:<4} {sector:6} {count:>7} {enemy_bm:>8.0f} {door_bm:>7.0f} {boss_avg * ENEMY_BIOMASS_SCALE:>7.0f} {biomass:>7.0f} {essence:>7.0f} {total_essence:>8.0f}")
    ranks = STATS * sum(RANK_BASE + RANK_STEP * r for r in range(MAX_RANK))
    unlocks = sum(read_stock())
    meta = ranks + unlocks
    print(f"\nShop prices: heal {HEAL_PRICE}, mutations {MUTATION_PRICES[0]}/{MUTATION_PRICES[1]}/{MUTATION_PRICES[2]} biomass by notches.")
    print(f"Mother's Call buy-out: stat ranks {ranks:,} + unlocks {unlocks:,} = {meta:,} Essence.")
    for reached in range(1, floor_count + 1):
        run = sum(per_floor[:reached]) - (FLOOR_ESSENCE if reached < floor_count else 0) * 0
        lost = FLOOR_ESSENCE + BOSS_ESSENCE
        print(f"Runs dying on L{reached - 1} (after its doors, before its boss): {run - lost:>5.0f} Essence each, {meta / max(1, run - lost):>5.0f} runs to buy everything.")
    print(f"Full clears: {total_essence:.0f} Essence each, {meta / total_essence:.0f} runs to buy everything.")
    return 0


if __name__ == '__main__':
    sys.exit(main())
