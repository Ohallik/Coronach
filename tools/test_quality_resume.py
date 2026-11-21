import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from quality_resume import stage_resume


class ResumeContract(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / 'quality'; self.root.mkdir()
        self.source = self.root / 'passed'; self.source.mkdir()
        self.target = self.root / 'next'; self.target.mkdir()
        self.save = self.source / 'saves'; self.save.mkdir()
        for name, data in {'run.json': {'valid': True, 'failures': []}, 'analysis.json': {'valid': True, 'failures': []},
                           'route.json': {'scene': 'Title', 'starterParty': False, 'steps': [{'name': 'title', 'expectedScene': 'Title'}]}, 'build.json': {'source': 'fixture'}}.items():
            (self.source / name).write_text(json.dumps(data))
        (self.save / 'slot1.json').write_text(json.dumps(dict(version=1, party=[{'id': 'Taren'}], scrip=60)))

    def test_copy_preserves_source_and_records_exact_hash(self):
        before = (self.save / 'slot1.json').read_bytes()
        manifest = stage_resume(self.source, self.target, self.root)
        self.assertEqual(before, (self.target / 'saves/slot1.json').read_bytes())
        self.assertEqual(before, (self.save / 'slot1.json').read_bytes())
        self.assertEqual(hashlib.sha256(before).hexdigest(), manifest['saves']['slot1.json'])

    def test_chapter_and_legacy_slots_copy_without_migrating_or_reformatting(self):
        chapter = dict(version=2, party=[{'id': 'Taren'}, {'id': 'Sela'}], flags={'hushwell.nursery': True})
        (self.save / 'autosave.json').write_text(json.dumps(chapter, indent=2) + '\n')
        before = {path.name: path.read_bytes() for path in self.save.iterdir()}
        manifest = stage_resume(self.source, self.target, self.root)
        self.assertEqual(set(before), set(manifest['saves']))
        for name, data in before.items():
            self.assertEqual(data, (self.save / name).read_bytes())
            self.assertEqual(data, (self.target / 'saves' / name).read_bytes())
            self.assertEqual(hashlib.sha256(data).hexdigest(), manifest['saves'][name])

    def test_unsupported_or_noninteger_save_versions_reject_before_copy(self):
        for index, version in enumerate((True, 1.0, 2.0, '1', '2', None, 0, 3, 999)):
            with self.subTest(version=version):
                target = self.root / ('invalid-' + str(index)); target.mkdir()
                slot = self.save / 'slot1.json'
                slot.write_text(json.dumps(dict(version=version, party=[{'id': 'Taren'}])))
                before = slot.read_bytes()
                with self.assertRaises(ValueError): stage_resume(self.source, target, self.root)
                self.assertFalse((target / 'saves').exists())
                self.assertEqual(before, slot.read_bytes())

    def test_rejected_or_incomplete_evidence_cannot_seed_a_pass(self):
        for name, data in [('run.json', {'valid': False}), ('analysis.json', {'valid': True, 'failures': ['lost focus']})]:
            with self.subTest(name=name):
                path = self.source / name; original = path.read_bytes(); path.write_text(json.dumps(data))
                with self.assertRaises(ValueError): stage_resume(self.source, self.target, self.root)
                path.write_bytes(original)
        self.assertFalse((self.target / 'saves').exists())

    def scout(self, run_found_no_fault=True, analysis_found_no_fault=True):
        """The source as a reconnaissance run (D290) leaves it: never valid, with what it found beside."""
        (self.source / 'run.json').write_text(json.dumps({'valid': False, 'reconnaissance': True, 'reconnaissancePassed': run_found_no_fault,
                                                          'failures': [] if run_found_no_fault else ['step: condition timeout']}))
        (self.source / 'analysis.json').write_text(json.dumps({'valid': False, 'reconnaissance': True, 'reconnaissancePassed': analysis_found_no_fault,
                                                               'failures': [] if analysis_found_no_fault else ['route truncated']}))

    def test_reconnaissance_can_never_seed_a_run_that_is_to_be_evidence(self):
        self.scout()
        with self.assertRaises(ValueError): stage_resume(self.source, self.target, self.root)
        self.assertFalse((self.target / 'saves').exists())

    def test_reconnaissance_goes_on_from_reconnaissance_that_found_no_fault_and_from_a_pass(self):
        self.scout()
        manifest = stage_resume(self.source, self.target, self.root, reconnaissance=True)
        self.assertTrue(manifest['reconnaissance'])
        self.assertTrue((self.target / 'saves/slot1.json').exists())
        # From a passed run too: scouting ahead of where the evidence has reached is the point.
        again = self.root / 'ahead'; again.mkdir()
        for name in ('run.json', 'analysis.json'): (self.source / name).write_text(json.dumps({'valid': True, 'failures': []}))
        self.assertTrue(stage_resume(self.source, again, self.root, reconnaissance=True)['reconnaissance'])

    def test_reconnaissance_that_found_a_fault_seeds_nothing(self):
        for index, found in enumerate(((False, True), (True, False))):
            with self.subTest(found=found):
                self.scout(*found)
                target = self.root / ('fault-' + str(index)); target.mkdir()
                with self.assertRaises(ValueError): stage_resume(self.source, target, self.root, reconnaissance=True)
                self.assertFalse((target / 'saves').exists())

    def test_reconnaissance_steps_past_faults_only_when_told_to_and_writes_them_down(self):
        self.scout(False, False)
        manifest = stage_resume(self.source, self.target, self.root, reconnaissance=True, past_faults=True)
        self.assertEqual(['route truncated', 'step: condition timeout'], manifest['pastFaults'])
        # Never for a run that is to be evidence, whatever else is asked.
        strict = self.root / 'strict'; strict.mkdir()
        with self.assertRaises(ValueError): stage_resume(self.source, strict, self.root, reconnaissance=False, past_faults=True)
        # And never from a rejected run that was not reconnaissance: a focus-checked run that failed is a failure.
        for name in ('run.json', 'analysis.json'): (self.source / name).write_text(json.dumps({'valid': False, 'failures': ['lost focus']}))
        with self.assertRaises(ValueError): stage_resume(self.source, strict, self.root, reconnaissance=True, past_faults=True)
        self.assertFalse((strict / 'saves').exists())

    def test_a_reconnaissance_mark_without_its_verdict_seeds_nothing(self):
        # A hand-made or half-written record: "valid" alone, or the mark alone, is not a reconnaissance that found no fault.
        (self.source / 'run.json').write_text(json.dumps({'valid': False, 'reconnaissance': True, 'failures': []}))
        (self.source / 'analysis.json').write_text(json.dumps({'valid': False, 'reconnaissance': True, 'failures': []}))
        with self.assertRaises(ValueError): stage_resume(self.source, self.target, self.root, reconnaissance=True)

    def test_development_fixtures_cannot_seed_a_workshop(self):
        for route in ({'scene': 'Gullet_Tunnel'}, {'scene': 'Title'}, {'scene': 'Title', 'starterParty': False, 'loadout': 'gullet'}):
            with self.subTest(route=route):
                (self.source / 'route.json').write_text(json.dumps(route))
                with self.assertRaises(ValueError): stage_resume(self.source, self.target, self.root)

    def test_outside_or_same_run_paths_reject(self):
        outside = Path(self.temp.name) / 'ordinary-saves'; outside.mkdir()
        for source, target in ((outside, self.target), (self.source, outside), (self.source, self.source)):
            with self.subTest(source=source, target=target):
                with self.assertRaises(ValueError): stage_resume(source, target, self.root)

    def test_existing_destination_is_never_overwritten(self):
        (self.target / 'saves').mkdir()
        with self.assertRaises(FileExistsError): stage_resume(self.source, self.target, self.root)

    def test_invalid_slot_rejects_before_copy(self):
        (self.save / 'slot1.json').write_text('{"version":1,"party":[]}')
        with self.assertRaises(ValueError): stage_resume(self.source, self.target, self.root)
        self.assertFalse((self.target / 'saves').exists())

    def test_title_route_must_name_every_checkpoint_scene(self):
        route = {'scene': 'Title', 'starterParty': False, 'steps': [
            {'name': 'title', 'expectedScene': 'Title'},
            {'name': 'fly into the Gullet', 'navigate': True}]}
        (self.source / 'route.json').write_text(json.dumps(route))
        with self.assertRaisesRegex(ValueError, 'fly into the Gullet'):
            stage_resume(self.source, self.target, self.root)
        self.assertFalse((self.target / 'saves').exists())

    def test_empty_title_route_cannot_seed_a_workshop(self):
        (self.source / 'route.json').write_text(json.dumps({'scene': 'Title', 'starterParty': False, 'steps': []}))
        with self.assertRaises(ValueError): stage_resume(self.source, self.target, self.root)
        self.assertFalse((self.target / 'saves').exists())


if __name__ == '__main__': unittest.main()
