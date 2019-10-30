using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Threading.Tasks;
using UnityEditor.PackageManager.UI;
using Random = UnityEngine.Random;

public class TerrainGen : MonoBehaviour
{
    public float heightScale;
    public int chunkLength;
    public int chunkWidth;
    public int numChunksX;
    public int numChunksY;
    public int randomSeed;
    public ColorBar colorBar;
    public AnimationCurve colorCurve;
    public Biome[] biomes;

    private GameObject[] _chunks;
    private MeshFilter[] _meshFilters;
    private MeshRenderer[] _meshRenderers;
    private Texture2D[] _meshTextures;
    private Material _meshMaterial;
    private const int NumOctaves = 10;
    private Vector2[] _octaveOffsets;
    private float _minHeight;
    private float _maxHeight;
    private int _prevSeed;


    private void Start()
    {
        _meshMaterial = new Material(Shader.Find("Standard"));
        _minHeight = float.MaxValue;
        _maxHeight = float.MinValue;
        _prevSeed = randomSeed;

        CreateChunks();

        Random.InitState(randomSeed);
        CreateOctaveOffsets(NumOctaves);


        // Generate height maps
        var heights = new float[numChunksX * numChunksY][,];
        for (var i = 0; i < numChunksY; i++)
        {
            for (var j = 0; j < numChunksX; j++)
            {
                _chunks[i * numChunksY + j].transform.position =
                    new Vector3(j * (chunkWidth - 1), 0, i * (chunkLength - 1));
                heights[i * numChunksY + j] =
                    GenerateHeightMap(chunkLength, chunkWidth, i * (chunkWidth - 1), j * (chunkLength - 1));
            }
        }

        NormaliseHeightMaps(heights);

        // Create Meshes and Textures
        _meshTextures = new Texture2D[numChunksX * numChunksY];
        for (var i = 0; i < numChunksX * numChunksY; i++)
        {
            var meshData = MeshGen.GenerateTerrainMesh(heights[i], heightScale);
            _meshTextures[i] = TextureFromHeightMap(heights[i]);
            DrawMeshes(meshData, _meshTextures[i], i);
        }
    }

    private void Update()
    {
        // Only update if seed was changed
        if (randomSeed == _prevSeed) return;
        _prevSeed = randomSeed;

        Random.InitState(randomSeed);
        CreateOctaveOffsets(NumOctaves);

        // Generate height maps
        var heights = new float[numChunksX * numChunksY][,];
        for (var i = 0; i < numChunksY; i++)
        {
            for (var j = 0; j < numChunksX; j++)
            {
                heights[i * numChunksY + j] =
                    GenerateHeightMap(chunkLength, chunkWidth, i * (chunkWidth - 1), j * (chunkLength - 1));
            }
        }

        NormaliseHeightMaps(heights);

        // Create Meshes and Textures
        for (var i = 0; i < numChunksX * numChunksY; i++)
        {
            var meshData = MeshGen.GenerateTerrainMesh(heights[i], heightScale);
            _meshTextures[i] = UpdateTextureFromHeightMap(heights[i], _meshTextures[i]);
            UpdateMeshes(meshData, _meshTextures[i], i);
        }
    }

    private void CreateChunks()
    {
        _chunks = new GameObject[numChunksX * numChunksY];
        _meshFilters = new MeshFilter[numChunksX * numChunksY];
        _meshRenderers = new MeshRenderer[numChunksX * numChunksY];

        for (var i = 0; i < numChunksX * numChunksY; i++)
        {
            _chunks[i] = new GameObject("Mesh " + i);
            _meshFilters[i] = _chunks[i].AddComponent<MeshFilter>();
            _meshRenderers[i] = _chunks[i].AddComponent<MeshRenderer>();
            _meshRenderers[i].sharedMaterial = new Material(_meshMaterial);
        }
    }

    private void CreateOctaveOffsets(int octaves)
    {
        _octaveOffsets = new Vector2[octaves];
        for (var i = 0; i < octaves; i++)
        {
            _octaveOffsets[i] = new Vector2(Random.Range(-10000, 10000),
                Random.Range(-10000, 10000));
        }
    }

    private float[,] GenerateHeightMap(int height, int width, int chunkOffsetX, int chunkOffsetY)
    {
//        var heights = new float[height, width];
//        Parallel.For(0, height,
//            i => Parallel.For(0, width,
//                j => heights[i, j] = GetPerlinAt(chunkOffsetY + i, chunkOffsetX + j, biomes[0])));
        var heights = GetPerlins(chunkOffsetY, chunkOffsetX, height, width, biomes[0].octaves, biomes[0]);

//        for (var i = 0; i < height; i++)
//        {
//            for (var j = 0; j < width; j++)
//            {
//                var noiseHeight = GetPerlinAt(chunkOffsetY + i, chunkOffsetX + j, biomes[0]);
//
//                // Store max/min values
//                if (noiseHeight > _maxHeight) _maxHeight = noiseHeight;
//                else if (noiseHeight < _minHeight) _minHeight = noiseHeight;
//
//                heights[i, j] = noiseHeight;
//            }
//        }

        // Store max/min values
        var max = heights.Cast<float>().Max();
        if (max > _maxHeight) _maxHeight = max;
        var min = heights.Cast<float>().Min();
        if (min < _minHeight) _minHeight = min;

        return heights;
    }

    private void NormaliseHeightMaps(IEnumerable<float[,]> heightMaps)
    {
        // Normalize in range [0, 1]
        foreach (var heightMap in heightMaps)
        {
            var height = heightMap.GetLength(0);
            var width = heightMap.GetLength(1);

            Parallel.For(0, height,
                i => Parallel.For(0, width,
                    j => heightMap[i, j] = Mathf.InverseLerp(_minHeight, _maxHeight, heightMap[i, j])));
        }
    }


    private Color[] GenerateColorMap(float[,] heightMap)
    {
        var sampledBC = Helpers.SampleColorBar(colorBar, colorCurve, 256);

        var height = heightMap.GetLength(0);
        var width = heightMap.GetLength(1);

        var terrainColors = new Color[height * width];
        Parallel.For(0, height, delegate(int i)
        {
            for (var j = 0; j < width; j++)
            {
                terrainColors[j * height + i] = Helpers.EvalSampledColorBar(sampledBC, heightMap[i, j]);
            }
        });

        return terrainColors;
    }

    private Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        var height = heightMap.GetLength(0);
        var width = heightMap.GetLength(1);

        var colors = GenerateColorMap(heightMap);

        var tex = new Texture2D(height, width)
        {
            filterMode = FilterMode.Point
        };
        tex.SetPixels(colors);
        tex.Apply();

        return tex;
    }

    private Texture2D UpdateTextureFromHeightMap(float[,] heightMap, Texture2D tex)
    {
        var colors = GenerateColorMap(heightMap);
        tex.SetPixels(colors);
        tex.Apply();

        return tex;
    }


    public void DrawMeshes(MeshData meshData, Texture2D texture, int idx)
    {
        _meshFilters[idx].sharedMesh = meshData.CreateMesh();
        _meshRenderers[idx].sharedMaterial.mainTexture = texture;
    }


    public void UpdateMeshes(MeshData meshData, Texture2D texture, int idx)
    {
        _meshFilters[idx].sharedMesh = meshData.UpdateMesh(_meshFilters[idx].sharedMesh);
        _meshRenderers[idx].sharedMaterial.mainTexture = texture;
    }

    private float GetPerlinAt(int x, int y, Biome biome)
    {
        // Calculate value at this position
        float amplitude = 1;
        float freq = 1;
        float noiseHeight = 0;
        for (var k = 0; k < biome.octaves; k++)
        {
            var i = x * freq / biome.noiseScale + _octaveOffsets[k].x;
            var j = y * freq / biome.noiseScale + _octaveOffsets[k].y;

            var perlinVal = biome.heightCurve.Evaluate(Mathf.PerlinNoise(i, j));

            noiseHeight += (2 * perlinVal - 1) * amplitude;
            amplitude *= biome.persistence;
            freq *= biome.lacunarity;
        }

        return noiseHeight;
    }

    private float[,] GetPerlins(int x, int y, int height, int width, int octaves, Biome biome)
    {
        var sampledAC = Helpers.SampleAnimationCurve(biome.heightCurve, 256);

        var amplitudes = new float[octaves];
        var freqs = new float[octaves];
        Parallel.For(0, octaves, delegate(int i)
        {
            amplitudes[i] = (float) Math.Pow(biome.persistence, i);
            freqs[i] = (float) Math.Pow(biome.lacunarity, i);
        });


        var noiseHeights = new float[height, width];
        Parallel.For(0, height, delegate(int i)
        {
            for (var j = 0; j < width; j++)
            {
                for (var k = 0; k < octaves; k++)
                {
                    var I = (x + i) * freqs[k] / biome.noiseScale + _octaveOffsets[k].x;
                    var J = (y + j) * freqs[k] / biome.noiseScale + _octaveOffsets[k].y;
                    var perlin = Helpers.EvalSampledAnimCurve(sampledAC, Mathf.PerlinNoise(I, J));
                    noiseHeights[i, j] += (2 * perlin - 1) * amplitudes[k];
                }
            }
        });

        return noiseHeights;
    }
}

[Serializable]
public struct Biome
{
    public string name;
    public int octaves;
    public float noiseScale;
    public float lacunarity;
    public float persistence;
    public AnimationCurve heightCurve;
}


public static class MeshGen
{
    public static MeshData GenerateTerrainMesh(float[,] heights, float heightScale)
    {
        var width = heights.GetLength(0);
        var height = heights.GetLength(1);

        var meshData = new MeshData(width, height);

        var vertexIndex = 0;
        for (var i = 0; i < height; i++)
        {
            for (var j = 0; j < width; j++)
            {
                meshData.vertices[vertexIndex] = new Vector3(i, heights[i, j] * heightScale, j); // Add vertex
                meshData.uvs[vertexIndex] = new Vector2(i / (float) width, j / (float) height);

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

    private int _triangleIndex; // keeps track of next triangle index

    public MeshData(int width, int height)
    {
        vertices = new Vector3[width * height];
        triangles = new int[(width - 1) * (height - 1) * 6];
        uvs = new Vector2[width * height];
    }

    public void AddTriangle(int a, int b, int c)
        // a, b, c: indices of vertices of triangle
    {
        triangles[_triangleIndex] = a;
        triangles[_triangleIndex + 1] = b;
        triangles[_triangleIndex + 2] = c;
        _triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        var mesh = new Mesh
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