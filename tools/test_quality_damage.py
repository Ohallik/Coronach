import unittest
import json
import tempfile
import csv
import subprocess
import sys
from pathlib import Path

from quality_damage import analyze, include


def hit(shot=1, volley=1, source=20):
    return dict(seconds=4.2, step=3, sourceAvailable=True, sourceAlive=True,
                scene='Gullet_Tunnel', victim='Taren', source='ChoristerDrifter', type='Beam',
                impactSourceInstanceId=source,
                deathAttack=False, requested=12, received=9, remaining=91,
                position=dict(x=1, y=1, z=3), projectile=True,
                provenanceAvailable=True, releaseCapture='capture-one',
                volleyId=volley, shotId=shot, sourceInstanceId=source,
                projectileInstanceId=-100 + shot, releaseSource='ChoristerDrifter',
                releasedAt=3.5, releasePosition=dict(x=0, y=1, z=8),
                releaseVelocity=dict(x=0, y=0, z=-11))


def report(*hits):
    return dict(version=2, complete=True, dropped=0, captureId='capture-one', hits=list(hits),
                projectileLaunches=3 if hits else 0, projectileVolleys=3 if hits else 0)


class DamageProvenanceTests(unittest.TestCase):
    def reject(self, value, message):
        result = analyze(value)
        self.assertFalse(result['valid'], result)
        self.assertTrue(any(message in failure for failure in result['failures']), result)

    def test_a_three_shot_fan_has_one_volley_and_three_launches(self):
        result = analyze(report(hit(1), hit(2), hit(3)))
        self.assertTrue(result['valid'], result)
        self.assertEqual((1, 3), (result['distinctVolleys'], result['distinctLaunches']))

    def test_same_definition_actors_and_reused_pool_objects_remain_distinct(self):
        a, b, c = hit(), hit(2, 2, 21), hit(3, 3)
        c['projectileInstanceId'] = a['projectileInstanceId']
        result = analyze(report(a, b, c))
        self.assertTrue(result['valid'], result)
        self.assertEqual(3, result['distinctLaunches'])

    def test_destroyed_source_keeps_its_release_identity(self):
        value = hit(); value.update(sourceAvailable=False, sourceAlive=False, impactSourceInstanceId=0, source='UNAVAILABLE')
        self.assertTrue(analyze(report(value))['valid'])

    def test_dead_source_is_not_relabelled_as_an_intended_death_attack(self):
        value = hit(); value['sourceAlive'] = False
        self.assertTrue(analyze(report(value))['valid'])
        self.assertFalse(value['deathAttack'])

    def test_legacy_events_cannot_acquire_invented_shot_identity(self):
        value = report(hit()); value.pop('version'); value.pop('captureId')
        result = analyze(value)
        self.assertTrue(result['valid'], result)
        self.assertIn('UNAVAILABLE', result['projectileIdentity'])
        self.assertIsNone(result['distinctLaunches'])

    def test_non_projectile_wall_damage_does_not_require_release_fields(self):
        value = hit(); value['projectile'] = False
        for key in ['shotId', 'volleyId', 'releaseSource', 'releasedAt']:
            value.pop(key)
        self.assertTrue(analyze(report(value))['valid'])

    def test_reused_launch_id_rejects_even_for_another_source(self):
        self.reject(report(hit(), hit(1, 2, 21)), 'identity was reused')

    def test_volley_cannot_change_actors_with_the_same_definition(self):
        self.reject(report(hit(), hit(2, 1, 21)), 'different source instances')

    def test_missing_release_and_foreign_capture_reject(self):
        for change in [dict(provenanceAvailable=False), dict(releaseCapture='old-capture')]:
            with self.subTest(change=change):
                value = hit(); value.update(change)
                self.reject(report(value), 'release provenance')

    def test_zero_boolean_and_missing_identifiers_reject(self):
        for key in ['shotId', 'volleyId', 'sourceInstanceId', 'projectileInstanceId']:
            for bad in [0, True, None]:
                with self.subTest(key=key, bad=bad):
                    value = hit(); value[key] = bad
                    self.reject(report(value), 'invalid')

    def test_invalid_release_clocks_and_geometry_reject(self):
        for key, bad in [('releasedAt', 4.3), ('releasedAt', -1), ('releasedAt', float('nan')),
                         ('releasePosition', dict(x=0, y=float('inf'), z=0)),
                         ('releaseVelocity', dict(x=0, y=0, z=0))]:
            with self.subTest(key=key, bad=bad):
                value = hit(); value[key] = bad
                self.assertFalse(analyze(report(value))['valid'])

    def test_incomplete_and_dropped_events_reject(self):
        for change in [dict(complete=False), dict(dropped=1), dict(dropped=False)]:
            value = report(hit()); value.update(change)
            self.assertFalse(analyze(value)['valid'])

    def test_missing_classification_and_source_identity_reject(self):
        for key in ['projectile', 'releaseSource', 'sourceAvailable', 'impactSourceInstanceId', 'victim']:
            value = hit(); value.pop(key)
            self.assertFalse(analyze(report(value))['valid'])

    def test_available_source_must_match_its_release_instance(self):
        value = hit(); value['impactSourceInstanceId'] = 21
        self.reject(report(value), 'differs from release source')
        value = hit(); value['releaseSource'] = 'Cantor'
        self.reject(report(value), 'differs from release source')

    def test_impact_order_and_liveness_are_consistent(self):
        a, b = hit(), hit(2); b['seconds'] = 4
        self.reject(report(a, b), 'reversed impact clock')
        a['sourceAvailable'] = False
        self.reject(report(a), 'cannot be observed alive')

    def test_empty_complete_capture_is_valid_but_unknown_versions_are_not(self):
        self.assertTrue(analyze(report())['valid'])
        value = report(); value['version'] = 3
        self.reject(value, 'unsupported')

    def test_release_counts_bound_identities_without_requiring_every_shot_to_hit(self):
        value = report(hit()); value['projectileLaunches'] = 20
        self.assertTrue(analyze(value)['valid'])
        value['projectileLaunches'] = 0
        self.reject(value, 'exceeds recorded releases')
        value = report(hit()); value.pop('projectileVolleys')
        self.reject(value, 'invalid projectileVolleys')

    def test_inclusion_preserves_prior_failures_and_requires_declared_sidecar(self):
        with tempfile.TemporaryDirectory() as folder:
            result = dict(valid=True, failures=[]); include(result, folder)
            self.assertNotIn('damageEvidence', result)
            result = dict(valid=True, failures=[]); include(result, folder, 2)
            self.assertFalse(result['valid'])
            Path(folder, 'damage.json').write_text(json.dumps(report(hit())), encoding='utf-8')
            result = dict(valid=False, failures=['earlier route failure']); include(result, folder, 2)
            self.assertFalse(result['valid']); self.assertTrue(result['damageEvidence']['valid'])
            self.assertEqual(['earlier route failure'], result['failures'])

    def test_declared_version_and_malformed_sidecar_reject(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder, 'damage.json')
            for data in [[], 'bad json', dict(complete=True, dropped=0, hits=[])]:
                with self.subTest(data=data):
                    path.write_text(json.dumps(data), encoding='utf-8')
                    result = dict(valid=True, failures=[]); include(result, folder, 2)
                    self.assertFalse(result['valid'])

    def test_declared_version_must_be_a_supported_integer_even_without_a_sidecar(self):
        with tempfile.TemporaryDirectory() as folder:
            for present in (False, True):
                if present:
                    Path(folder, 'damage.json').write_text(json.dumps(report()), encoding='utf-8')
                for version in (False, True, 0.0, 2.0, None, '', 3, -1):
                    with self.subTest(present=present, version=version):
                        result = dict(valid=True, failures=[]); include(result, folder, version)
                        self.assertFalse(result['valid'])

    def test_replay_cli_enforces_the_damage_sidecar_contract(self):
        import test_quality_analyze
        fixture = test_quality_analyze.TraversalContract()
        fixture.setUp()
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'route.json').write_text(json.dumps(fixture.route), encoding='utf-8')
            with (root / 'frames.csv').open('w', encoding='utf-8', newline='') as output:
                writer = csv.DictWriter(output, fieldnames=fixture.frames[0].keys())
                writer.writeheader(); writer.writerows(fixture.frames)
            invalid = report(hit()); invalid['hits'][0]['shotId'] = 0
            cases = [(2, report(hit()), True), (2, invalid, False), (2, None, False),
                     (0, dict(complete=True, dropped=0, hits=[]), True)]
            for version, damage, valid in cases:
                with self.subTest(version=version, damage=damage):
                    fixture.run['damageTraceVersion'] = version
                    (root / 'run.json').write_text(json.dumps(fixture.run), encoding='utf-8')
                    sidecar = root / 'damage.json'
                    if damage is None:
                        sidecar.unlink(missing_ok=True)
                    else:
                        sidecar.write_text(json.dumps(damage), encoding='utf-8')
                    run = subprocess.run([sys.executable, str(Path(__file__).with_name('quality_analyze.py')), folder], capture_output=True, text=True)
                    result = json.loads((root / 'analysis.json').read_text(encoding='utf-8'))
                    self.assertEqual(valid, run.returncode == 0, run.stdout + run.stderr)
                    self.assertEqual(valid, result['valid'], result)
                    self.assertIn('damageEvidence', result)
                    if version == 0:
                        self.assertIn('UNAVAILABLE', result['damageEvidence']['projectileIdentity'])


if __name__ == '__main__':
    unittest.main()
