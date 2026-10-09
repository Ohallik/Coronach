import copy
import unittest
from quality_navigation import describe_navigation


class NavigationDiagnosisTests(unittest.TestCase):
    def setUp(self):
        self.route = dict(scene='TallowApproach', steps=[dict(name='return', seconds=10, navigate=True,
                         point=dict(x=26, y=1, z=0), leftTrigger=1)])
        self.run = dict(valid=False, samples=3, failures=['return: navigation checkpoint missed', 'focus lost'])
        self.frames = [dict(elapsed=str(t), ms='16.67', step='0', scene='TallowApproach', hero='Taren',
                      x=str(x), z='0', reportedSpeed='2.1', focus='True', paused='False', blocked='False')
                       for t, x in [(1, 0), (6, 10.5), (11, 21)]]

    def report(self):
        return describe_navigation(self.run, self.frames, self.route)

    def test_shortfall_retains_failed_verdict_and_full_brake_request(self):
        report = self.report(); step = report['navigation'][0]
        self.assertIs(report['runtimeValid'], False)
        self.assertEqual(report['runtimeFailures'], self.run['failures'])
        self.assertEqual(step['authoredInputs']['leftTrigger'], 1)
        self.assertEqual(step['runtimeFailures'], ['return: navigation checkpoint missed'])
        self.assertEqual(step['sampledPathMetres'], 21)
        self.assertEqual(step['closestDistanceMetres'], 5)
        self.assertAlmostEqual(step['estimatedExtraSecondsAtObservedSpeed'], 4.35 / 2.1)

    def test_focus_loss_is_timed_and_disables_extrapolation(self):
        self.frames[1]['focus'] = 'False'; report = self.report()
        self.assertEqual(report['firstFocusLossSeconds'], 6)
        self.assertEqual(report['navigation'][0]['focusedSamples'], 2)
        self.assertIsNone(report['navigation'][0]['estimatedExtraSecondsAtObservedSpeed'])

    def test_pause_or_blocking_prevents_a_clean_speed_extrapolation(self):
        for flag in ('paused', 'blocked'):
            with self.subTest(flag=flag):
                self.frames[1][flag] = 'True'
                self.assertIsNone(self.report()['navigation'][0]['estimatedExtraSecondsAtObservedSpeed'])
                self.frames[1][flag] = 'False'

    def test_hero_or_scene_changes_do_not_become_travel(self):
        for field, value in [('hero', 'Sela'), ('scene', 'TallowDrift')]:
            with self.subTest(field=field):
                original = self.frames[1][field]; self.frames[1][field] = value
                step = self.report()['navigation'][0]
                self.assertEqual(step['sampledPathMetres'], 0)
                self.assertEqual(step['discontinuities'], 2)
                self.assertEqual(step['goal'], 'DISCONTINUOUS_COORDINATES')
                self.assertNotIn('estimatedExtraSecondsAtObservedSpeed', step)
                self.frames[1][field] = original

    def test_moving_goal_is_not_compared_to_its_placeholder_point(self):
        self.route['steps'][0]['approachTarget'] = True
        step = self.report()['navigation'][0]
        self.assertEqual(step['goal'], 'MOVING_TARGET_NOT_RECONSTRUCTED')
        self.assertNotIn('closestDistanceMetres', step)

    def test_missing_navigation_remains_unobserved(self):
        step = copy.deepcopy(self.route['steps'][0]); step['name'] = 'later'; self.route['steps'].append(step)
        self.assertEqual(self.report()['navigation'][1]['observation'], 'UNOBSERVED')

    def test_authored_tolerance_has_no_added_acceptance_margin(self):
        self.route['steps'][0]['tolerance'] = 2
        step = self.report()['navigation'][0]
        self.assertEqual(step['authoredRadiusMetres'], 2)
        self.assertAlmostEqual(step['estimatedExtraSecondsAtObservedSpeed'], 3 / 2.1)
        self.assertNotIn('valid', step)

    def test_nonfinite_and_nonmonotonic_samples_reject(self):
        self.frames[1]['x'] = 'NaN'
        with self.assertRaisesRegex(ValueError, 'nonfinite'): self.report()
        self.frames[1]['x'] = '10.5'; self.frames[1]['elapsed'] = '1'
        with self.assertRaisesRegex(ValueError, 'nonmonotonic'): self.report()

    def test_empty_or_unknown_focus_rejects(self):
        with self.assertRaisesRegex(ValueError, 'empty'): describe_navigation(self.run, [], self.route)
        self.frames[1]['focus'] = 'unknown'
        with self.assertRaisesRegex(ValueError, 'unknown focus'): self.report()


if __name__ == '__main__':
    unittest.main()
