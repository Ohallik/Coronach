#!/usr/bin/env python3
"""Payload compatibility tests for Lattice's Meshy bridge."""

from __future__ import annotations

import unittest
from types import SimpleNamespace

from tools.meshy import meshy


class MeshyContractTests(unittest.TestCase):
    def test_smart_topology_img3d_omits_standard_only_options(self) -> None:
        args = SimpleNamespace(
            image="fixture.png",
            ai_model="meshy-t2",
            band="prop",
            target_polycount=6000,
            riggable=False,
        )

        payload, polycount_band = meshy.build_img3d_payload(
            args, "data:image/png;base64,fixture"
        )

        self.assertEqual("smart-topology", payload["model_type"])
        self.assertEqual("meshy-t2", payload["ai_model"])
        self.assertEqual(6000, payload["target_polycount"])
        self.assertTrue(payload["should_texture"])
        self.assertNotIn("image_enhancement", payload)
        self.assertNotIn("remove_lighting", payload)
        self.assertEqual({"min": 1000, "max": 6000, "target": 6000}, polycount_band)


if __name__ == "__main__":
    unittest.main()
