import unittest
import numpy as np
from raster_metrics import silhouette_metrics

class SilhouetteRegressionTests(unittest.TestCase):
    def test_identical_perforated_component_passes(self):
        a = np.zeros((128,128), bool); a[10:118,10:118] = True; a[40:60,40:60] = False
        r = silhouette_metrics(a,a)
        self.assertEqual(r['silhouetteIoU'],1.)
        self.assertEqual(r['sourceHolePixelsCoveredFraction'],0.)
        self.assertEqual(r['sourceHolePixels'],400)

    def test_small_filled_opening_cannot_hide_in_high_iou(self):
        a = np.zeros((128,128), bool); a[10:118,10:118] = True; a[40:42,40:42] = False
        b = a.copy(); b[40:42,40:42] = True
        r = silhouette_metrics(a,b)
        self.assertGreater(r['silhouetteIoU'],.995)
        self.assertGreater(r['sourceHolePixelsCoveredFraction'],.02)

    def test_missing_leg_is_rejected_by_silhouette(self):
        a = np.zeros((128,128), bool); a[30:50,20:108] = True; a[50:115,25:31] = True; a[50:115,97:103] = True
        b = a.copy(); b[50:115,25:31] = False
        r = silhouette_metrics(a,b)
        self.assertLess(r['silhouetteIoU'],.995)
        self.assertEqual(r['lostSourcePixels'],390)

    def test_empty_or_mismatched_evidence_is_invalid(self):
        with self.assertRaises(ValueError): silhouette_metrics(np.zeros((2,2)), np.zeros((2,2)))
        with self.assertRaises(ValueError): silhouette_metrics(np.ones((2,2)), np.ones((3,2)))

if __name__ == '__main__': unittest.main()
