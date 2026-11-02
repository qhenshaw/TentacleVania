# Writes the room kit sector spec for one run floor (Assets/Game/World/Data/Sectors/RunKitL<n>.sector.json) from the
# room tables below: each room is a rectangle with the entry on its south edge, RunExits on the listed sides (centred,
# like the KT greybox kit) and theme props placed at fractions of the room. Build the scenes afterwards with
# SectorPlans.Build("K<n>") from an editor eval, then list the rooms in the RunKit asset.
#
# Usage: python -I Tools/runkit-spec.py <floor 0-3>

import json
import os
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
ENTRY = 'Assets/Game/World/Run/RunEntry.prefab'
EXIT = 'Assets/Game/World/Run/RunExit.prefab'
PROPS = {
    'pod': 'Assets/Game/World/Opening/SpecimenPod.prefab',
    'console': 'Assets/Game/World/Opening/LabConsole.prefab',
    'cover': 'Assets/Game/World/Cover/Barricade.prefab',
    'pylon': 'Assets/Game/World/Hazards/ArenaPylon.prefab',
    'acid': 'Assets/Game/World/Hazards/Hazard_Acid.prefab',
    'cryo': 'Assets/Game/World/Hazards/Hazard_Cryo.prefab',
    'electric': 'Assets/Game/World/Hazards/Hazard_Electric.prefab',
    'fire': 'Assets/Game/World/Hazards/Hazard_Fire.prefab',
    'radiation': 'Assets/Game/World/Hazards/Hazard_Radiation.prefab',
    'tank': 'Assets/Game/World/Props/MotherTank.prefab',
}
ALL = ('North', 'East', 'West')

# (scene suffix, role, width, height, exit sides, [(prop, fx, fy, yaw)])
FLOORS = {
    0: ('Containment', [
        ('Entry', 'Plain', 6, 5, ALL, [('pod', 0.2, 0.7, 180), ('pod', 0.8, 0.7, 180)]),
        ('Cells', 'Arena', 9, 6, ALL, [('pod', 0.15, 0.8, 180), ('pod', 0.3, 0.8, 180), ('pod', 0.7, 0.8, 180), ('pod', 0.85, 0.8, 180)]),
        ('Admin', 'Arena', 7, 7, ALL, [('console', 0.25, 0.3, 0), ('console', 0.75, 0.3, 0), ('cover', 0.25, 0.65, 0), ('cover', 0.75, 0.65, 0)]),
        ('Decon', 'Arena', 7, 8, ALL, [('acid', 0.3, 0.5, 0), ('acid', 0.7, 0.5, 0)]),
        ('Lobby', 'Arena', 10, 6, ALL, [('cover', 0.2, 0.4, 0), ('cover', 0.8, 0.4, 0), ('console', 0.35, 0.75, 180), ('console', 0.65, 0.75, 180)]),
        ('Checkpoint', 'Arena', 6, 6, ALL, [('cover', 0.25, 0.55, 90), ('cover', 0.75, 0.55, 90)]),
        ('Isolation', 'Arena', 8, 8, ALL, [('pod', 0.2, 0.2, 0), ('pod', 0.8, 0.2, 0), ('pod', 0.2, 0.8, 180), ('pod', 0.8, 0.8, 180), ('acid', 0.35, 0.5, 0)]),
        ('Records', 'Arena', 8, 6, ALL, [('console', 0.2, 0.75, 180), ('console', 0.8, 0.75, 180), ('cover', 0.3, 0.35, 0), ('cover', 0.7, 0.35, 0)]),
        ('Holding', 'Arena', 11, 7, ALL, [('pod', 0.12, 0.8, 180), ('pod', 0.25, 0.8, 180), ('pod', 0.75, 0.8, 180), ('pod', 0.88, 0.8, 180), ('cover', 0.35, 0.35, 0), ('cover', 0.65, 0.35, 0)]),
    ]),
}

GAP = 3


def build(floor):
    theme, rooms = FLOORS[floor]
    sector = f'K{floor}'
    spec = {
        'sector': sector,
        'rootScene': 'Run',
        'folder': f'Assets/Game/Scenes/RunKit/L{floor}',
        'addressablesGroup': f'RunKitL{floor}',
        'color': [0.7, 0.7, 0.75, 1],
        'spawn': [2, 2],
        'spawnYaw': 0,
        'runKit': True,
        'rooms': [],
    }
    x = 0
    for suffix, role, w, h, sides, props in rooms:
        cx = x + w // 2
        cy = h // 2
        doors, place = [], [{'type': 'prefab', 'prefab': ENTRY, 'at': [cx + 0.5, 0.8], 'yaw': 0, 'name': 'Entry'}]
        for side in sides:
            if side == 'North':
                doors.append({'x': cx, 'y': h - 1, 'side': side})
                place.append({'type': 'prefab', 'prefab': EXIT, 'at': [cx + 0.5, h - 0.15], 'yaw': 0, 'name': 'ExitN'})
            elif side == 'East':
                doors.append({'x': x + w - 1, 'y': cy, 'side': side})
                place.append({'type': 'prefab', 'prefab': EXIT, 'at': [x + w - 0.15, cy + 0.5], 'yaw': 90, 'name': 'ExitE'})
            else:
                doors.append({'x': x, 'y': cy, 'side': side})
                place.append({'type': 'prefab', 'prefab': EXIT, 'at': [x + 0.15, cy + 0.5], 'yaw': 270, 'name': 'ExitW'})
        for i, (prop, fx, fy, yaw) in enumerate(props):
            place.append({'type': 'prefab', 'prefab': PROPS[prop], 'at': [round(x + fx * w, 2), round(fy * h, 2)], 'yaw': yaw, 'name': f'{prop.capitalize()}{i}'})
        room = {'scene': f'{sector}_{suffix}', 'role': role, 'rect': [x, 0, w, h], 'doors': doors, 'place': place}
        if role == 'Arena':
            room['waves'] = [[{'enemy': 'Scientist', 'count': 1}]]
        spec['rooms'].append(room)
        x += w + GAP
    path = os.path.join(ROOT, 'Assets', 'Game', 'World', 'Data', 'Sectors', f'RunKitL{floor}.sector.json')
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(spec, f, indent=2)
        f.write('\n')
    print(f'{path}: {len(rooms)} rooms ({theme})')


if __name__ == '__main__':
    build(int(sys.argv[1]))
