import unittest
from quality_watch import progress


class ProgressMeaning(unittest.TestCase):
    def test_checkpoint_is_only_an_attempt(self):
        row = progress('QUALITY_CHECKPOINT_BEGIN 108 read the collar\n', 25, {'pid': 25})
        self.assertEqual('108 read the collar', row['checkpointAttempt'])
        self.assertEqual('PENDING', row['validation'])

    def test_latest_attempt_is_not_a_claim_that_earlier_steps_passed(self):
        row = progress('QUALITY_CHECKPOINT_BEGIN 30 first\nQUALITY_CHECKPOINT_BEGIN 170 arrive\n', 25, {'pid': 26})
        self.assertEqual('170 arrive', row['checkpointAttempt'])
        self.assertFalse(row['foregroundMatches'])
        self.assertEqual('PENDING', row['validation'])

    def test_missing_pid_or_foreground_is_unknown(self):
        for pid, owner in [(None, 25), (25, None), (None, None), (25, 0)]:
            with self.subTest(pid=pid, owner=owner):
                self.assertIsNone(progress('', pid, {'pid': owner})['foregroundMatches'])

    def test_rejection_is_not_erased_by_later_attempt(self):
        row = progress('QUALITY_REPLAY_REJECTED\nQUALITY_CHECKPOINT_BEGIN 170 arrive\n', 25, {'pid': 25})
        self.assertEqual('REJECTED', row['validation'])


if __name__ == '__main__':
    unittest.main()
