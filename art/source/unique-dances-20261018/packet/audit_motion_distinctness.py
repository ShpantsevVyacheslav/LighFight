"""Audit donor joint rotations sampled at equal normalized phases.

Input: {clips:[{name, samples:[{joint:[x,y,z,w],...},...]}]}.
Supply the same body joints and phase sample count for every clip. Excludes root
translation/scale, minimizes cyclic phase offsets, and treats q and -q equally.
This rejects simple relabel/time-scale/start-offset copies; visual audit remains
required to establish genuinely different authored choreography.
"""
import argparse
import json
import math


def angle(a, b):
    na = math.sqrt(sum(x*x for x in a))
    nb = math.sqrt(sum(x*x for x in b))
    if not na or not nb or not all(math.isfinite(x) for x in a+b):
        raise ValueError('invalid quaternion')
    dot = min(1., abs(sum(x*y for x, y in zip(a, b)) / (na*nb)))
    return math.degrees(2*math.acos(dot))


def distance(a, b):
    if len(a) != len(b) or len(a) < 16:
        raise ValueError('at least 16 equal normalized-phase samples required')
    joints = sorted(a[0])
    if len(joints) < 8 or any(sorted(frame) != joints for frame in a+b):
        raise ValueError('at least 8 consistent major body joints required')
    return min(math.sqrt(sum(angle(a[i][j], b[(i+shift) % len(b)][j])**2
                             for i in range(len(a)) for j in joints)
                         / (len(a)*len(joints))) for shift in range(len(a)))


def audit(clips, threshold=5):
    pairs = []
    for i, first in enumerate(clips):
        for second in clips[i+1:]:
            rms = distance(first['samples'], second['samples'])
            pairs.append(dict(first=first['name'], second=second['name'],
                              minimum_cyclic_rms_degrees=rms, distinct=rms >= threshold))
    return dict(all_distinct=all(p['distinct'] for p in pairs), pairs=pairs,
                qualification='Numerical duplicate rejection only; authored-motion visual review required.')


if __name__ == '__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('input'); parser.add_argument('--threshold',type=float,default=5)
    args=parser.parse_args()
    with open(args.input) as f: data=json.load(f)
    report=audit(data['clips'],args.threshold)
    print(json.dumps(report,indent=2))
    raise SystemExit(0 if report['all_distinct'] else 1)
