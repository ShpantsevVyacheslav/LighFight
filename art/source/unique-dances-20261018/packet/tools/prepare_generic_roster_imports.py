"""Stage exact acquired FBXs only when the dedicated Unity batch window is authorized."""
import pathlib,json,hashlib,shutil,argparse
if __name__=='__main__':
 a=argparse.ArgumentParser();a.add_argument('--stage',action='store_true',help='copy into Unity Assets immediately before the authorized batch');args=a.parse_args();rows=json.loads(pathlib.Path('art/source/unique-dances-20261018/packet/generic-roster-import-inventory.json').read_text())
 for row in rows:
  if row['actor']==9:continue
  src=pathlib.Path(row['source']);assert hashlib.sha256(src.read_bytes()).hexdigest()==row['sha256']
  if args.stage:
   dest=pathlib.Path(row['genericCopy']);meta=pathlib.Path(str(dest)+'.meta');prepared=pathlib.Path(row['preparedMeta']);dest.parent.mkdir(parents=True,exist_ok=True)
   if dest.exists():assert hashlib.sha256(dest.read_bytes()).hexdigest()==row['sha256']
   else:shutil.copyfile(src,dest)
   if meta.exists():assert meta.read_bytes()==prepared.read_bytes(),'Importer metadata differs; preserve and review instead of overwriting'
   else:shutil.copyfile(prepared,meta)
 print('Verified10raw sources; '+('staged10exact FBX+stable GUID metadata for authorized Unity job' if args.stage else 'NO copies/imports performed; use --stage only in authorized window'))
