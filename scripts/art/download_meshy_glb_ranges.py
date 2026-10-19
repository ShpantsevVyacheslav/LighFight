"""Download returned Meshy artifact URLs; never calls task/status endpoints."""
import concurrent.futures
import hashlib
import json
import os
from pathlib import Path
import struct
import sys
import urllib.request
from urllib.parse import urlparse

manifest = Path(sys.argv[1])
destination = Path(sys.argv[2])
for asset, url in json.loads(manifest.read_text()).items():
    if not asset.replace('-', '').replace('_', '').isalnum():
        raise ValueError('Invalid asset identifier')
    if urlparse(url).hostname != 'assets.meshy.ai':
        raise ValueError('Only returned Meshy asset URLs are supported')
    def fetch(start, end):
        request = urllib.request.Request(url, headers={'Range': f'bytes={start}-{end}'})
        with urllib.request.urlopen(request, timeout=60) as response:
            if response.status != 206:
                raise RuntimeError('Provider must support byte ranges')
            if not response.headers.get('Content-Range', '').startswith(f'bytes {start}-{end}/'):
                raise RuntimeError('Unexpected byte range')
            data = response.read()
        if len(data) != end - start + 1:
            raise RuntimeError('Incomplete byte range')
        return data
    magic, version, length = struct.unpack('<III', fetch(0, 11))
    if magic != 0x46546C67 or version != 2 or not 20 < length < 512 * 1024 * 1024:
        raise RuntimeError('Unexpected GLB header')
    folder = destination / asset
    folder.mkdir(parents=True, exist_ok=True)
    partial = folder / 'model.download-part'
    fd = os.open(partial, os.O_CREAT | os.O_TRUNC | os.O_WRONLY, 0o644)
    def chunk(pair):
        start, end = pair
        data = fetch(start, end)
        written = os.pwrite(fd, data, start)
        if written != len(data):
            raise RuntimeError('Incomplete disk write')
    try:
        ranges = [(i, min(length - 1, i + 4 * 1024 * 1024 - 1)) for i in range(0, length, 4 * 1024 * 1024)]
        with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
            list(pool.map(chunk, ranges))
        os.fsync(fd)
    finally:
        os.close(fd)
    if partial.stat().st_size != length:
        raise RuntimeError('Incomplete GLB file')
    final = folder / 'model.glb'
    os.replace(partial, final)
    receipt = {'bytes': length, 'sha256': hashlib.sha256(final.read_bytes()).hexdigest(), 'allRangesValidated': True}
    (folder / 'download-receipt.json').write_text(json.dumps(receipt, indent=2))
    print(json.dumps({'asset': asset, **receipt}), flush=True)
