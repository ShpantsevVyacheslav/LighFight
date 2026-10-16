"""Synthetic parser regression only, not runtime/device evidence."""
import csv,json,subprocess,sys,tempfile,unittest
from pathlib import Path
class SummaryTest(unittest.TestCase):
 def run_case(self,reason='completed120seconds',markers=False,native=False):
  with tempfile.TemporaryDirectory() as d:
   p=Path(d);(p/'configuration.txt').write_text('durationSeconds=120\n'+('mainMarker=True\nposeMarker=True\ncontactMarker=False\n' if markers else ''));(p/'result.txt').write_text(f'reason={reason}\nerrors=0\nelapsed=120\n')
   with (p/'frames.csv').open('w') as out:
    w=csv.writer(out);w.writerow(['elapsed','wallMs','timestamp','gpuRawMs','mainMs','poseMs','contactMs']);w.writerows([[1,16.7,1,0,8,0,0],[2,16.7,1,2,12,4,0],[3,16.7,1,2,10,2,0],[4,16.7,2,0,6,0,0]])
   with (p/'slow.csv').open('w') as out:
    w=csv.writer(out);w.writerow(['elapsed','cpuProcessSeconds','thermal','physicalFootprintBytes','hitA','hitB']);w.writerows([[0,10,0,100,0,0],[1,10.5,0,110,0,0],[61,70.5,2,130,1,0],[62,71,2,120,1,0]] if native else [[0,-1,-1,0,0,0],[1,-1,-1,0,0,0]])
   run=subprocess.run([sys.executable,str(Path(__file__).with_name('summarize_evidence.py')),str(p)],capture_output=True,text=True)
   return run.returncode,json.loads((p/'summary.json').read_text())
 def test_raw_zero_never_counted_as_valid_gpu_and_later_positive_accepted(self):
  rc,r=self.run_case();self.assertEqual(rc,0);self.assertEqual(r['validUniqueGpuMs']['samples'],1);self.assertEqual(r['validUniqueGpuMs']['mean'],2);self.assertEqual(r['gpuCompletedPositiveAfterEarlierZero'],1)
 def test_unavailable_os_metrics_not_zero_busy_or_zero_memory(self):
  _,r=self.run_case();self.assertIsNone(r['processCpuCoreEquivalents']);self.assertIsNone(r['maxPhysicalFootprintBytes']);self.assertEqual(r['observedThermalStates'],[-1])
 def test_critical_stop_not_completed_even_if_duration_field_long(self):
  rc,r=self.run_case('critical_thermal_stop');self.assertEqual(rc,1);self.assertEqual(r['status'],'INCOMPLETE_OR_FAILED')
 def test_valid_recorders_keep_zero_work_and_report_separate_inclusive_markers(self):
  rc,r=self.run_case(markers=True);self.assertEqual(rc,0)
  self.assertEqual(r['cpuMarkersMs']['main']['mean'],9)
  self.assertEqual(r['cpuMarkersMs']['pose']['samples'],4)
  self.assertEqual(r['cpuMarkersMs']['pose']['mean'],1.5)
  self.assertEqual(r['cpuMarkersMs']['pose']['p95'],4)
  self.assertIsNone(r['cpuMarkersMs']['contact'])
  self.assertEqual(r['buckets'][0]['cpuMarkersMs']['main']['samples'],4)
 def test_missing_recorder_validity_is_unavailable_not_zero_cost(self):
  _,r=self.run_case();self.assertIsNone(r['cpuMarkersMs']['main'])
 def test_native_bucket_cpu_does_not_interpolate_across_boundary(self):
  _,r=self.run_case(native=True)
  self.assertEqual(r['buckets'][0]['cpuCoveredSeconds'],1)
  self.assertEqual(r['buckets'][0]['processCpuCoreEquivalents'],.5)
  self.assertEqual(r['buckets'][1]['processCpuCoreEquivalents'],.5)
  self.assertEqual(r['buckets'][1]['observedThermalStates'],[2])
  self.assertEqual(r['buckets'][1]['maxPhysicalFootprintBytes'],130)
if __name__=='__main__':unittest.main()
