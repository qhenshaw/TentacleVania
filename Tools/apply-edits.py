# Applies a Markdown edit plan (written by a helper agent) to the working tree. Each section headed "## " or "### "
# names one file in backticks (Assets/....cs, .asmdef, .uxml, .uss or .json); "New file" or "whole file" sections write
# the first code block, other sections pair "old:" and "new:" code blocks as exact replacements (CRLF-aware).
# A section that covers two files must be split into two headings first.
#
# Usage: python -I Tools/apply-edits.py <plan.md> [apply] [multi]
#   Dry-runs by default and reports edits whose old text doesn't match exactly once; "apply" writes the files when
#   nothing failed; "multi" allows an old text that matches several times (replaces all).
import os, re, sys
os.chdir(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
md = open(sys.argv[1], encoding='utf-8').read().replace('\r\n', '\n')
apply = len(sys.argv) > 2 and sys.argv[2] == 'apply'

sections = re.split(r'\n(?=#{2,3} )', md)
plan = []
for sec in sections:
    head = sec.split('\n', 1)[0]
    if not head.startswith('#'):
        continue
    paths = re.findall(r'`(Assets/[^`]+\.(?:cs|asmdef|uxml|uss|json))`', head)
    blocks = [(m.start(), m.group(1)) for m in re.finditer(r'```(?:csharp|json|xml|css)\n(.*?)```', sec, re.S)]
    if not blocks:
        continue
    if not paths:
        print('SKIP (no path):', head)
        continue
    path = paths[0]
    if 'new file' in head.lower() or 'whole file' in head.lower() or 'new `assets' in head.lower():
        plan.append(('new', path, blocks[0][1], None))
        continue
    labels = [(m.start(), m.group(1)) for m in re.finditer(r'\n(old|new):\s*\n', sec)]
    pairs = []
    for pos, code in blocks:
        label = None
        for lpos, l in labels:
            if lpos < pos:
                label = l
        pairs.append((label, code))
    i = 0
    while i < len(pairs):
        if pairs[i][0] == 'old' and i + 1 < len(pairs) and pairs[i + 1][0] == 'new':
            plan.append(('edit', path, pairs[i][1], pairs[i + 1][1]))
            i += 2
        else:
            print('UNPAIRED in', head, 'label', pairs[i][0])
            i += 1

files = {}
def load(p):
    if p not in files:
        files[p] = open(p, encoding='utf-8', newline='').read() if os.path.exists(p) else None
    return files[p]

problems = 0
for kind, path, a, b in plan:
    if kind == 'new':
        if load(path) is not None:
            print('OVERWRITE', path)
        files[path] = a.replace('\n', '\r\n')
        continue
    s = load(path)
    if s is None:
        print('MISSING FILE', path); problems += 1; continue
    old = a.rstrip('\n').replace('\n', '\r\n')
    new = b.rstrip('\n').replace('\n', '\r\n')
    c = s.count(old)
    if c != 1 and not (c > 1 and 'multi' in sys.argv):
        print('COUNT', c, path, repr(old[:120])); problems += 1; continue
    files[path] = s.replace(old, new)
print('edits', len(plan), 'problems', problems)
if apply and problems == 0:
    for p, s in files.items():
        os.makedirs(os.path.dirname(p), exist_ok=True)
        open(p, 'w', encoding='utf-8', newline='').write(s)
    print('written', len(files))
