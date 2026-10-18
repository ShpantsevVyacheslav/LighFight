using System;
using System.Collections.Generic;

// Portable REVIEW candidate, no Unity dependency. All original support points survive.
// Exact leaf evaluator uses the original float LBS order. Bounds only skip points proven
// unable to improve the current float minimum. Not yet a production grounding adapter.
public sealed class ExactFootFeatureBounds
{
    public struct Statistics { public int ExactPoints, BoundsTested, PrunedPoints; }
    struct Tuple : IEquatable<Tuple>
    {
        public int A,B,C,D;
        public bool Equals(Tuple t) { return A==t.A&&B==t.B&&C==t.C&&D==t.D; }
        public override bool Equals(object value) { return value is Tuple && Equals((Tuple)value); }
        public override int GetHashCode() { unchecked { return ((A*397^B)*397^C)*397^D; } }
    }
    struct Node { public int Start,Count,Left,Right,A,B,C,D; }
    readonly int[] bones, order, roots, stack;
    readonly float[] weights, points;
    readonly Node[] nodes;
    readonly double[] low, high, maxBindAbsolute;
    readonly int boneCount;
    public int PointCount { get { return order.Length; } }
    public int NodeCount { get { return nodes.Length; } }
    public int GroupCount { get { return roots.Length; } }
    // u=2^-24; gamma64 safely covers <=4 four-term dot products, weights,
    // accumulations, and float-vs-double reassociation. Double dot roundoff is
    // additionally covered by16eps. Subnormal absolute guard is numeric only.
    const double FloatGamma64 = (64.0/16777216.0)/(1.0-64.0/16777216.0);
    const double DoubleGamma32 = 32.0*2.2204460492503131e-16;

    public ExactFootFeatureBounds(int boneCount, int[] boneIndices, float[] originalWeights, float[] originalBindXYZ, int leafSize=64)
    {
        if (boneCount<1 || originalWeights==null || originalWeights.Length%4!=0 || leafSize<1) throw new ArgumentException("Support shape");
        int count=originalWeights.Length/4;
        if(count<1)throw new ArgumentException("Empty support is unsupported");
        if (boneIndices==null||boneIndices.Length!=count*4||originalBindXYZ==null||originalBindXYZ.Length!=count*12) throw new ArgumentException("Support shape");
        this.boneCount=boneCount; maxBindAbsolute=new double[boneCount*3]; bones=(int[])boneIndices.Clone(); weights=(float[])originalWeights.Clone(); points=(float[])originalBindXYZ.Clone();
        var groups=new Dictionary<Tuple,List<int>>();
        for(int i=0;i<count;i++)
        {
            for(int k=0;k<4;k++)
            {
                int q=i*4+k; float w=weights[q];
                if(!Finite(w)||w<0||w>1||(w>0&&(bones[q]<0||bones[q]>=boneCount))) throw new ArgumentException("Invalid original influence");
                if(w==0) bones[q]=0;
                for(int d=0;d<3;d++)
                {
                    if(!Finite(points[q*3+d])) throw new ArgumentException("Invalid original point");
                    if(w>0)maxBindAbsolute[bones[q]*3+d]=Math.Max(maxBindAbsolute[bones[q]*3+d],Math.Abs((double)points[q*3+d]));
                }
            }
            var key=new Tuple{A=bones[i*4],B=bones[i*4+1],C=bones[i*4+2],D=bones[i*4+3]};
            List<int> members; if(!groups.TryGetValue(key,out members)) { members=new List<int>();groups.Add(key,members); } members.Add(i);
        }
        order=new int[count];var builderNodes=new List<Node>();var builderLow=new List<double>();var builderHigh=new List<double>();var builderRoots=new List<int>();
        var comparer=new FeatureComparer(this);int start=0;
        foreach(var group in groups)
        {
            group.Value.CopyTo(order,start);
            builderRoots.Add(Build(group.Key,start,group.Value.Count,leafSize,builderNodes,builderLow,builderHigh,comparer));start+=group.Value.Count;
        }
        nodes=builderNodes.ToArray();low=builderLow.ToArray();high=builderHigh.ToArray();roots=builderRoots.ToArray();stack=new int[nodes.Length];
    }
    sealed class FeatureComparer : IComparer<int>
    {
        readonly ExactFootFeatureBounds owner;public int Axis;
        public FeatureComparer(ExactFootFeatureBounds owner){this.owner=owner;}
        public int Compare(int a,int b){return owner.Feature(a,Axis).CompareTo(owner.Feature(b,Axis));}
    }
    double Feature(int point,int dimension)
    {
        int influence=point*4+dimension/4;int axis=dimension%4;
        return axis==3?weights[influence]:(double)weights[influence]*points[influence*3+axis];
    }
    int Build(Tuple tuple,int start,int count,int leaf,List<Node> ns,List<double> lo,List<double> hi,FeatureComparer comparer)
    {
        int index=ns.Count; ns.Add(new Node());int bounds=index*16;
        for(int d=0;d<16;d++){lo.Add(double.PositiveInfinity);hi.Add(double.NegativeInfinity);}
        for(int n=start;n<start+count;n++) for(int d=0;d<16;d++)
        {double f=Feature(order[n],d);if(f<lo[bounds+d])lo[bounds+d]=f;if(f>hi[bounds+d])hi[bounds+d]=f;}
        var node=new Node{Start=start,Count=count,Left=-1,Right=-1,A=tuple.A,B=tuple.B,C=tuple.C,D=tuple.D};
        if(count>leaf)
        {
            int axis=0;for(int d=1;d<16;d++)if(hi[bounds+d]-lo[bounds+d]>hi[bounds+axis]-lo[bounds+axis])axis=d;
            comparer.Axis=axis;Array.Sort(order,start,count,comparer);int half=count/2;
            node.Left=Build(tuple,start,half,leaf,ns,lo,hi,comparer);node.Right=Build(tuple,start+half,count-half,leaf,ns,lo,hi,comparer);
        }
        ns[index]=node;return index;
    }
    static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    double Lower(int index,float[] rows)
    {
        var node=nodes[index];double bound=0,magnitude=0;int b=index*16;
        for(int k=0;k<4;k++)
        {
            int bone=k==0?node.A:k==1?node.B:k==2?node.C:node.D;
            for(int d=0;d<4;d++)
            {double c=rows[bone*4+d],l=low[b+k*4+d],h=high[b+k*4+d];bound+=c*(c>=0?l:h);magnitude+=Math.Abs(c)*Math.Max(Math.Abs(l),Math.Abs(h));}
        }
        if(magnitude>float.MaxValue/64.0)throw new ArithmeticException("Pose magnitude unsupported: no floor-clear claim");
        return bound-magnitude*(FloatGamma64+DoubleGamma32)-1e-30;
    }
    float Exact(int i,float[] rows)
    {
        float y=0;
        for(int k=0;k<4;k++)
        {
            int q=i*4+k;float w=weights[q];if(w==0)continue;int b=bones[q]*4,p=q*3;
            float value=rows[b]*points[p]+rows[b+1]*points[p+1]+rows[b+2]*points[p+2]+rows[b+3];
            y+=value*w;
        }
        if(!Finite(y))throw new ArithmeticException("Nonfinite float support result: no floor-clear claim");return y;
    }
    public float Minimum(float[] verticalBoneRows,out Statistics statistics)
    {
        if(verticalBoneRows==null||verticalBoneRows.Length!=boneCount*4)throw new ArgumentException("Pose row shape");
        for(int i=0;i<verticalBoneRows.Length;i++)if(!Finite(verticalBoneRows[i]))throw new ArgumentException("Nonfinite pose: no floor-clear claim");
        // Bound UNWEIGHTED intermediates too: tiny weights cannot legitimize a float
        // dot-product overflow that happens before weighting. Unsupported magnitude throws.
        for(int bone=0;bone<boneCount;bone++)
        {
            double magnitude=Math.Abs((double)verticalBoneRows[bone*4+3]);
            for(int d=0;d<3;d++)magnitude+=Math.Abs((double)verticalBoneRows[bone*4+d])*maxBindAbsolute[bone*3+d];
            if(magnitude>float.MaxValue/16.0)throw new ArithmeticException("Unweighted float dot magnitude unsupported: no floor-clear claim");
        }
        statistics=new Statistics();float best=float.PositiveInfinity;
        // One exact seed per group only initializes best. Every other point remains represented
        // by its conservative node bounds; these seeds are not a sparse support replacement.
        for(int i=0;i<roots.Length;i++){float value=Exact(order[nodes[roots[i]].Start],verticalBoneRows);statistics.ExactPoints++;if(value<best)best=value;}
        int top=0;for(int i=0;i<roots.Length;i++)stack[top++]=roots[i];
        while(top>0)
        {
            int index=stack[--top];statistics.BoundsTested++;
            if(Lower(index,verticalBoneRows)>=best){statistics.PrunedPoints+=nodes[index].Count;continue;}
            var node=nodes[index];
            if(node.Left<0)
            {
                for(int i=node.Start;i<node.Start+node.Count;i++){float value=Exact(order[i],verticalBoneRows);statistics.ExactPoints++;if(value<best)best=value;}
            }
            else
            {
                // Nearer child first improves pruning; both remain on the stack until proved safe.
                double a=Lower(node.Left,verticalBoneRows),b=Lower(node.Right,verticalBoneRows);statistics.BoundsTested+=2;
                if(a<b){stack[top++]=node.Right;stack[top++]=node.Left;}else{stack[top++]=node.Left;stack[top++]=node.Right;}
            }
        }
        return best;
    }
}
