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
        // Slit faces using the same vertices so they are independent.

        var triSet = new HashSet<int>();
        var vertices = new List<Vector3>(verts);

        for (var i = 0; i < tris.Length; i++)
        {
            var tri = tris[i];
            if (triSet.Contains(tri))
            {
                // Create new vert at same location
                vertices.Add(verts[tri]);
                // Change triangle to use new vert
                tris[i] = vertices.Count - 1;
            }
            else
            {
                triSet.Add(tri);
            }
        }

        return new MeshInfo
        {
            vertices = vertices.ToArray(),
            triangles = tris
        };
    }
}