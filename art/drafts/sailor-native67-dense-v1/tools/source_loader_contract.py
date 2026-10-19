"""Pure namespace-shape validation: no Blender or source import side effects."""
OUTER=('OUT','start','args','body','donor','dm','scene','cam','cd','nativeNames','nativeJoint','nativeWeight','rawDm','meta','report','actual','group_hash','ns')
INNER=('clips','ref','clay')
def validate(ns):
 missing=[k for k in OUTER if k not in ns]
 assert not missing, 'Primary loader outer keys missing: '+repr(missing)
 assert isinstance(ns['ns'],dict),'Nested source definitions must be dict'
 missing=[k for k in INNER if k not in ns['ns']]
 assert not missing,'Primary loader nested source keys missing: '+repr(missing)
 assert callable(ns['actual']) and callable(ns['group_hash'])
 assert len(ns['nativeNames'])==67 and len(set(ns['nativeNames']))==67
 assert ns['nativeJoint'].shape==(49992,4) and ns['nativeWeight'].shape==(49992,4) and ns['rawDm'].shape==(49992,3)
 assert len(ns['ns']['clips'])==8,'Expected exact eight source-native actions'
 assert 'positionsBlenderLocal' in ns['ns']['ref']
 assert 'nativeTrueBindWitness' in ns['report']
 return ns['ns']
