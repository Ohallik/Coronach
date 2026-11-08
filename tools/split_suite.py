"""Splits the PlayMode test classes into N groups of about equal time, for scripts/full-suite.ps1, from the durations that earlier
result files recorded (6 s a test where none is known). Writes Builds/logs/suite_groups.json.

    python tools/split_suite.py [N]
"""
import collections
import glob
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
N = int(sys.argv[1]) if len(sys.argv) > 1 else 3

known = collections.defaultdict(dict)
results = glob.glob(os.path.join(ROOT, 'Builds', 'quality', 'suite', '*playmode*.xml'))
results += glob.glob(os.path.join(ROOT, 'Builds', 'quality', 'QA', '*playmode*.xml'))
for path in results:
    try:
        for case in ET.parse(path).getroot().iter('test-case'):
            name = case.get('fullname')
            cls = name.replace('Lattice.Tests.PlayMode.', '').split('.')[0]
            known[cls][name] = float(case.get('duration') or 0)
    except (ET.ParseError, OSError):
        pass

classes = {}
for path in glob.glob(os.path.join(ROOT, 'Lattice', 'Assets', 'Tests', 'PlayMode', '*.cs')):
    text = open(path, encoding='utf-8-sig').read()
    tests = len(re.findall(r'\[(UnityTest|Test)\]', text)) + len(re.findall(r'\[(TestCase|ValueSource)\b', text))
    for match in re.finditer(r'public sealed class (\w+)|public class (\w+)', text):
        cls = match.group(1) or match.group(2)
        if tests and (cls.endswith('Tests') or cls in known):
            classes[cls] = max(sum(known[cls].values()), tests * 6)
            break

groups = [{'classes': [], 'seconds': 0} for _ in range(N)]
for cls, seconds in sorted(classes.items(), key=lambda kv: -kv[1]):
    group = min(groups, key=lambda g: g['seconds'])
    group['classes'].append(cls)
    group['seconds'] += seconds
os.makedirs(os.path.join(ROOT, 'Builds', 'logs'), exist_ok=True)
json.dump(groups, open(os.path.join(ROOT, 'Builds', 'logs', 'suite_groups.json'), 'w'), indent=1)
print(len(classes), 'classes', round(sum(classes.values())), 's in all')
for group in groups:
    print(round(group['seconds']), 's,', len(group['classes']), 'classes')
