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
