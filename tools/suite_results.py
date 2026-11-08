"""Adds up a split suite's parts (scripts/full-suite.ps1) and lists every test that didn't pass.

    python tools/suite_results.py <label>
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'Builds', 'quality', 'suite')
label = sys.argv[1]

total = passed = failed = skipped = 0
seconds = 0.0
bad = []
for path in sorted(glob.glob(os.path.join(OUT, label + '-part*-playmode-results.xml'))):
    root = ET.parse(path).getroot()
    t, p, f, s = [int(root.get(k) or 0) for k in ('total', 'passed', 'failed', 'skipped')]
    total += t; passed += p; failed += f; skipped += s
    seconds += float(root.get('duration') or 0)
    print(os.path.basename(path), t, p, f, s, round(float(root.get('duration') or 0)))
    for case in root.iter('test-case'):
        if case.get('result') != 'Passed':
            message = case.find('./failure/message')
            text = (message.text if message is not None else '') or ''
            bad.append((case.get('fullname').replace('Lattice.Tests.PlayMode.', ''), case.get('result'), text.strip().replace('\n', ' ')[:420]))
print(f'PLAYMODE total {total} passed {passed} failed {failed} skipped {skipped} duration {round(seconds)} s')
for name, result, text in bad:
    print(' -', name, result, text)
try:
    root = ET.parse(os.path.join(OUT, label + '-editmode-results.xml')).getroot()
    print('EDITMODE', {k: root.get(k) for k in ('result', 'total', 'passed', 'failed')})
    for case in root.iter('test-case'):
        if case.get('result') != 'Passed':
            message = case.find('./failure/message')
            print(' -', case.get('fullname'), ((message.text if message is not None else '') or '')[:300])
except FileNotFoundError:
    print('no EditMode results')
