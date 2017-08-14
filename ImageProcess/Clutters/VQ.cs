using System;
using System.Collections.Generic;

/// <summary>
/// Vector Quantization
/// LBG
/// http://data-compression.com/vq.shtml
/// </summary>
public class VQ
{
    private List<Vector<float>> samples;

    /// <summary>
    /// target number of code vectors
    /// </summary>
    private int N;

    /// <summary>
    /// Epsilon of splitting step
    /// </summary>
    private float E;

    /// <summary>
    /// dimention of vector
    /// </summary>
    private int k;

    /// <summary>
    /// code book
    /// </summary>
    private Dictionary<Vector<float>, Vector<float>> Q;

    public VQ(List<Vector<float>> samples, int N, float E)
    {
        this.samples = samples;
        this.N = N;
        this.E = E;
        k = samples[0].dimention;
        Q = new Dictionary<Vector<float>, Vector<float>>();
    }

    public List<Vector<float>> Process()
    {
        List<Vector<float>> c = new List<Vector<float>>();
        Vector<float> cc = new Vector<float>(k);
        foreach (var x in samples) cc = VectorPlus(cc, x);
        cc = VectorDiv(cc, samples.Count);
        c.Add(cc);
        int t = 0;
        while (c.Count < N)
        {
            c = SplitCodeVector(c);
            float d = float.MaxValue;
            float d1 = float.MaxValue;
            do
            {
                d = d1;
                CalcCodeBook(c);
                d1 = MinimumAvgSqrDist();
                c = CalcCodeVectors();
                t += 1;
            }
            while ((d - d1) / d1 > E);
        }
        Console.WriteLine("vq loop times: " + t);
        CalcCodeBook(c);
        return c;
    }

    public Dictionary<Vector<float>, Vector<float>> codeBook
    {
        get
        {
            return Q;
        }
    }

    private List<Vector<float>> SplitCodeVector(List<Vector<float>> c)
    {
        List<Vector<float>> cp = new List<Vector<float>>(c.Count * 2);
        for (int i = 0; i < c.Count; i++)
        {
            cp.Add(VectorMul(c[i], 1 + E));
            cp.Add(VectorMul(c[i], 1 - E));
        }
        return cp;
    }

    private void CalcCodeBook(List<Vector<float>> c)
    {
        Q.Clear();
        foreach (var x in samples)
        {
            float min = float.MaxValue;
            Vector<float> cm = null;
            foreach (var ci in c)
            {
                float dist = VectorDist(x, ci);
                if (dist < min)
                {
                    min = dist;
                    cm = ci;
                }
            }
            Q.Add(x, cm);
        }
    }

    private List<Vector<float>> CalcCodeVectors()
    {
        Dictionary<Vector<float>, Vector<float>> vp = new Dictionary<Vector<float>, Vector<float>>();
        Dictionary<Vector<float>, float> vc = new Dictionary<Vector<float>, float>();
        foreach (var pair in Q)
        {
            var vi = pair.Key;
            var ci = pair.Value;
            if (!vp.ContainsKey(ci)) vp.Add(ci, new Vector<float>(k));
            vp[ci] = VectorPlus(vp[ci], vi);
            if (!vc.ContainsKey(ci)) vc.Add(ci, 0);
            vc[ci] += 1;
        }
        List<Vector<float>> ca = new List<Vector<float>>();
        foreach (var pair in vp)
        {
            var va = VectorDiv(pair.Value, vc[pair.Key]);
            ca.Add(va);
        }
        return ca;
    }

    private float MinimumAvgSqrDist()
    {
        float dist = 0;
        foreach (var vi in samples)
        {
            var minus = VectorMinus(vi, Q[vi]);
            dist += VectorSqrLen(minus);
        }
        dist /= (samples.Count * k);
        return dist;
    }

    private float VectorDist(Vector<float> v1, Vector<float> v2)
    {
        return VectorSqrLen(VectorMinus(v1, v2));
    }

    private float VectorSqrLen(Vector<float> v1)
    {
        float dist = 0;
        for (int i = 0; i < v1.dimention; i++)
        {
            dist += v1[i] * v1[i];
        }
        return dist;
    }

    private Vector<float> VectorPlus(Vector<float> v1, Vector<float> v2)
    {
        Vector<float> v = new Vector<float>(v1.dimention);
        for (int i = 0; i < v1.dimention; i++)
        {
            v[i] = v1[i] + v2[i];
        }
        return v;
    }

    private Vector<float> VectorMinus(Vector<float> v1, Vector<float> v2)
    {
        Vector<float> v = new Vector<float>(v1.dimention);
        for (int i = 0; i < v1.dimention; i++)
        {
            v[i] = v1[i] - v2[i];
        }
        return v;
    }

    private Vector<float> VectorMul(Vector<float> v1, float d)
    {
        Vector<float> v = new Vector<float>(v1.dimention);
        for (int i = 0; i < v1.dimention; i++)
        {
            v[i] = v1[i] * d;
        }
        return v;
    }

    private Vector<float> VectorDiv(Vector<float> v1, float d)
    {
        Vector<float> v = new Vector<float>(v1.dimention);
        float d1 = 1 / d;
        for (int i = 0; i < v1.dimention; i++)
        {
            v[i] = v1[i] * d1;
        }
        return v;
    }
}