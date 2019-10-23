using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Helpers
{
    public static float Wrap(float val, float min, float max)
    {
        // min inclusive, max exclusive
        return (val - min) % (max - min) + min;
    }

    public static int Wrap(int val, int min, int max)
    {
        // min inclusive, max exclusive
        return (val - min) % (max - min) + min;
    }

    public static double Wrap(double val, double min, double max)
    {
        // min inclusive, max exclusive
        return (val - min) % (max - min) + min;
    }

    public static int Clip(int val, int min, int max)
    {
        if (val < min) return min;
        if (val > max) return max;
        return val;
    }

    public static float Clip(float val, float min, float max)
    {
        if (val < min) return min;
        if (val > max) return max;
        return val;
    }

    public static double Clip(double val, double min, double max)
    {
        if (val < min) return min;
        if (val > max) return max;
        return val;
    }

    public struct MeshInfo
    {
        public Vector3[] vertices;
        public int[] triangles;
    }

    public static MeshInfo FixMesh(Vector3[] verts, int[] tris)
    {
        // Split faces using the same vertices so they are independent.

        var newVerts = new List<Vector3>(verts);
        var newTris = (int[]) tris.Clone();

        var triSet = new HashSet<int>();
        for (var i = 0; i < newTris.Length; i++)
        {
            var tri = newTris[i];
            if (triSet.Contains(tri))
            {
                // Create new vert at same location
                newVerts.Add(verts[tri]);
                // Change triangle to use new vert
                newTris[i] = newVerts.Count - 1;
            }
            else
            {
                triSet.Add(tri);
            }
        }

        return new MeshInfo
        {
            vertices = newVerts.ToArray(),
            triangles = newTris
        };
    }

    public static Material MaterialAverage(Material[] mats)
    {
        // Blends materials together

        var baseMat = new Material(mats[0]);
        for (var i = 1; i < mats.Length; i++)
        {
            baseMat.Lerp(baseMat, mats[i], (float) i / (i + 1));
        }

        return baseMat;
    }
}

public class HelperRandom
{
    private System.Random _rand;

    public HelperRandom()
    {
        _rand = new System.Random();
    }

    public HelperRandom(int seed)
    {
        _rand = new System.Random(seed);
    }

    public void SetSeed(int seed)
    {
        _rand = new System.Random(seed);
    }

    public float FloatRange(float min, float max)
    {
        return (float) _rand.NextDouble() * (max - min) + min;
    }

    public int IntRange(int min, int max)
    {
        return (int) _rand.NextDouble() * (max - min) + min;
    }

    public double NormalValue(double mean, double std)
    {
        // https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform

        var u1 = _rand.NextDouble();
        var u2 = _rand.NextDouble();

        // Basic form
        var z1 = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
//        var z2 = Math.Sqrt(-2 * Math.Log(u1)) * Math.Sin(2 * Math.PI * u2); // Second value can be generated

        return z1 * std + mean;
    }

    public double NormalValue(NormalRange range)
    {
        return Helpers.Clip(NormalValue(range.mean, range.std), range.min, range.max);
    }
}

[Serializable]
public struct NormalRange
{
    public double min;
    public double max;
    public double mean;
    [Min(0)] public double std;
}