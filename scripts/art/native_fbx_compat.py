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


def check_round_trip(before,after):
    assert before['triangles']==after['triangles'], 'Topology count changed'
    assert len(before['positions'])==len(after['positions']), 'Vertex count changed'
    assert len(before['uv'])==len(after['uv']), 'UV count changed'
    position_error=max(abs(a-b) for left,right in zip(before['positions'],after['positions']) for a,b in zip(left,right))
    weight_error=0
    for left,right in zip(before['weights'],after['weights']):
        assert set(left)==set(right), 'Weight assignments changed'
        weight_error=max(weight_error,max((abs(left[k]-right[k]) for k in left),default=0))
    uv_error=max(abs(a-b) for left,right in zip(before['uv'],after['uv']) for a,b in zip(left,right))
    assert position_error<1e-5 and weight_error<1e-6 and uv_error<1e-5, 'Surface or weights changed on FBX round trip'
    return {'positionMaxError':position_error,'weightMaxError':weight_error,'uvMaxError':uv_error}

