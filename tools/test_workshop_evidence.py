"""Intentional workshop menus must never make a frozen-world timing pass."""
import unittest
import test_quality_analyze as baseline
from quality_analyze import analyze, analyze_headroom


class WorkshopEvidenceContract(unittest.TestCase):
    def setUp(self):
        fixture = baseline.TraversalContract()
        fixture.setUp()
        self.run, self.frames, self.route = fixture.run, fixture.frames, fixture.route
        self.route['steps'][0]['pauseUi'] = 'bench'
        self.frames[200].update(paused='True', ui='bench')

    def test_declared_visible_menus_are_retained_as_traversal_evidence(self):
        for menu in ('bench', 'pause', 'defeat'):
            with self.subTest(menu=menu):
                self.route['steps'][0]['pauseUi'] = menu
                self.frames[200]['ui'] = menu
                result = analyze(self.run, self.frames, self.route)
                self.assertTrue(result['valid'], result['failures'])
                self.assertEqual(len(self.frames), result['samples'])

    def test_declared_menu_cannot_excuse_a_frozen_world(self):
        self.frames[200]['ui'] = 'world'
        self.assertIn('paused simulation', analyze(self.run, self.frames, self.route)['failures'])

    def test_wrong_visible_menu_rejects_pause(self):
        self.frames[200]['ui'] = 'defeat'
        self.assertIn('paused simulation', analyze(self.run, self.frames, self.route)['failures'])

    def test_missing_ui_observation_rejects_pause(self):
        del self.frames[200]['ui']
        self.assertIn('paused simulation', analyze(self.run, self.frames, self.route)['failures'])

    def test_undeclared_visible_menu_rejects_pause(self):
        del self.route['steps'][0]['pauseUi']
        self.assertIn('paused simulation', analyze(self.run, self.frames, self.route)['failures'])

    def test_menu_cannot_pass_capped_performance(self):
        self.assertIn('paused simulation', analyze(self.run, self.frames, self.route, True)['failures'])

    def test_menu_cannot_pass_headroom(self):
        self.run.update(headroom=True, frameCap=-1, vSync=0)
        for frame in self.frames:
            frame.update(activeCpuNs='6000000', activeRenderNs='2000000', gpuWorkNs='5000000')
        self.assertIn('paused simulation', analyze_headroom(self.run, self.frames, self.route)['failures'])

    def test_visible_menu_never_excuses_focus_loss(self):
        self.frames[200]['focus'] = 'False'
        self.assertIn('lost focus', analyze(self.run, self.frames, self.route)['failures'])


if __name__ == '__main__':
    unittest.main()
