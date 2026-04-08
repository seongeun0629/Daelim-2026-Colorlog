import unittest

from analysis.lighting import rgb_to_lab
from analysis.tone import get_personal_color_season


class SmokeTest(unittest.TestCase):
    def test_rgb_to_lab_returns_three_values(self):
        values = rgb_to_lab(120, 100, 90)
        self.assertEqual(len(values), 3)

    def test_personal_color_returns_string(self):
        season = get_personal_color_season(70, 5, 10)
        self.assertIsInstance(season, str)
        self.assertTrue(len(season) > 0)


if __name__ == "__main__":
    unittest.main()

