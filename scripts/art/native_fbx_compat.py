"""Strict bind-transform gate for joining native exports, not retargeting."""
import math

def require_same_rig(reference, candidate, tolerance=1e-5):
    if set(reference) != set(candidate):
        raise ValueError('Different bone inventory')
    for name, (parent, matrix) in reference.items():
        other_parent, other_matrix = candidate[name]
        if len(matrix)!=16 or len(other_matrix)!=16 or not all(math.isfinite(v) for v in matrix+other_matrix):
            raise ValueError('Invalid bind matrix: '+name)
        if parent != other_parent or max(abs(a-b) for a,b in zip(matrix, other_matrix)) > tolerance:
            raise ValueError('Different bind rig: ' + name)
