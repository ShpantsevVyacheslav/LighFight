"""Pure silhouette gates shared by review and synthetic regression fixtures."""
import numpy as np
from collections import deque

def hole_sizes(mask):
    h, w = mask.shape
    seen = mask.copy()
    holes = np.zeros_like(mask)
    sizes = []
    for y in range(h):
        for x in range(w):
            if seen[y,x]: continue
            q = deque([(y,x)]); seen[y,x] = True; cells = []; border = False
            while q:
                cy,cx = q.popleft(); cells.append((cy,cx))
                border |= cy == 0 or cx == 0 or cy == h-1 or cx == w-1
                for ny,nx in ((cy-1,cx),(cy+1,cx),(cy,cx-1),(cy,cx+1)):
                    if 0 <= ny < h and 0 <= nx < w and not seen[ny,nx]:
                        seen[ny,nx] = True; q.append((ny,nx))
            if not border:
                sizes.append(len(cells))
                for cy,cx in cells: holes[cy,cx] = True
    return holes, np.asarray(sizes, dtype=np.int64)

def silhouette_metrics(source, candidate):
    source = np.asarray(source, dtype=bool)
    candidate = np.asarray(candidate, dtype=bool)
    if source.shape != candidate.shape or source.ndim != 2:
        raise ValueError('Expected equally sized two-dimensional masks')
    union = source | candidate
    if not union.any():
        raise ValueError('Empty masks do not constitute evidence')
    holes, sizes = hole_sizes(source)
    return {
        'silhouetteIoU': float(np.count_nonzero(source & candidate) / union.sum()),
        'sourceMaskPixels': int(source.sum()),
        'candidateMaskPixels': int(candidate.sum()),
        'lostSourcePixels': int(np.count_nonzero(source & ~candidate)),
        'gainedCandidatePixels': int(np.count_nonzero(candidate & ~source)),
        'sourceHolePixels': int(holes.sum()),
        'sourceHoleComponentsAreaAtLeast1_4_16': [int(np.count_nonzero(sizes >= k)) for k in (1, 4, 16)],
        'sourceHolePixelsCoveredFraction': float(np.count_nonzero(holes & candidate) / holes.sum()) if holes.any() else 0.,
    }
