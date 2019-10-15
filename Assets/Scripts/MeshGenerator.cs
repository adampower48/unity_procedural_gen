using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Random = UnityEngine.Random;

[Serializable]
public class ColorRange
{
    public float maxVal;
    public Color color;
}

public class MeshGenerator: MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public float heightScale;
    public int terrainLength;
    public int terrainWidth;


    public float noiseScale;
    public int octaves;
    public float persistance;
    public float lacunarity;
    public int randomSeed;

    public AnimationCurve heightCurve;
    public ColorRange[] colRanges;

    Texture2D meshTexture;


    void Start()
    {
        Random.InitState(randomSeed);
        var heights = GenerateHeightMap(terrainLength, terrainWidth);
        var meshData = MeshGen.GenerateTerrainMesh(heights, heightScale);
        meshTexture = TextureFromHeightMap(heights);
        DrawMesh(meshData, meshTexture);
    }

    void Update()
    {
        Random.InitState(randomSeed);
        var heights = GenerateHeightMap(terrainLength, terrainWidth);
        var meshData = MeshGen.GenerateTerrainMesh(heights, heightScale);
        meshTexture = UpdateTextureFromHeightMap(heights, meshTexture);
        UpdateMesh(meshData, meshTexture);
    }


    float[,] GenerateHeightMap(int height, int width)
    {

        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            octaveOffsets[i] = new Vector2(Random.Range(-10000, 10000), 
                Random.Range(-10000, 10000));
        }


        float[,] heights = new float[height, width];


        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                // Calculate value at this position
                float amplitude = 1;
                float freq = 1;
                float noiseHeight = 0;
                for (int k = 0; k < octaves; k++)
                {
                    float x = i * freq / noiseScale + octaveOffsets[k].x;
                    float y = j * freq / noiseScale + octaveOffsets[k].y;

                    float perlinVal = heightCurve.Evaluate(Mathf.PerlinNoise(x, y));

                    noiseHeight += (2 * perlinVal - 1) * amplitude;
                    amplitude *= persistance;
                    freq *= lacunarity;
                }

                // Store max/min values
                if (noiseHeight > maxHeight) maxHeight = noiseHeight; 
                else if (noiseHeight < minHeight) minHeight = noiseHeight;


                heights[i, j] = noiseHeight;

            }
        }


        //Debug.Log(minHeight);
        //Debug.Log(maxHeight);

        // Normalize in range [0, 1]
        
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                // heights[i, j] =  (heights[i, j] - minHeight) / (maxHeight - minHeight);
                heights[i, j] = Mathf.InverseLerp(minHeight, maxHeight, heights[i, j]);
                heights[i, j] = Mathf.Max(new float[]{heights[i, j], colRanges[0].maxVal}); // Flatten water level
            }
        }

        return heights;
    }

    Color[] GenerateColorMap(float[,] heightMap)
    {
        int height = heightMap.GetLength(0);
        int width = heightMap.GetLength(1);

        Color[] terrainColors = new Color[height * width];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                terrainColors[j * height + i] = GetTerrainColor(heightMap[i, j]);
            }
        }

        return terrainColors;
    }

    Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int height = heightMap.GetLength(0);
        int width = heightMap.GetLength(1);

        var colors = GenerateColorMap(heightMap);

        Texture2D tex = new Texture2D(height, width)
        {
            filterMode = FilterMode.Point
        };
        tex.SetPixels(colors);
        tex.Apply();

        return tex;
    }

    Texture2D UpdateTextureFromHeightMap(float[,] heightMap, Texture2D tex)
    {
        var colors = GenerateColorMap(heightMap);
        tex.SetPixels(colors);
        tex.Apply();

        return tex;

    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

    public void UpdateMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.UpdateMesh(meshFilter.sharedMesh);
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

    Color GetTerrainColor(float val)
    {
        foreach (ColorRange c in colRanges)
        {
            if (val > c.maxVal)
            {
                continue;
            }

            return c.color;
        }

        return Color.black;
    }
}


public static class MeshGen
{
    public static MeshData GenerateTerrainMesh(float[,] heights, float heightScale)
    {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);

        MeshData meshData = new MeshData(width, height);

        int vertexIndex = 0;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                meshData.vertices[vertexIndex] = new Vector3(i, heights[i, j] * heightScale, j); // Add vertex
                meshData.uvs[vertexIndex] = new Vector2(i / (float)width, j / (float)height);

                if (i < height - 1 && j < width - 1) // Add triangles
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + width + 1, vertexIndex + width);
                    meshData.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + width + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }

}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex; // keeps track of next triangle index

    public MeshData(int width, int height)
    {
        vertices = new Vector3[width * height];
        triangles = new int[(width - 1) * (height  - 1) * 6];
        uvs = new Vector2[width * height];
    }

    public void AddTriangle(int a, int b, int c)
        // a, b, c: indices of vertices of triangle
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
            uv = uvs
        };
        mesh.RecalculateNormals();
        return mesh;
    }

    public Mesh UpdateMesh(Mesh mesh)
    {
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }


}