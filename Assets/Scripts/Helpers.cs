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
}