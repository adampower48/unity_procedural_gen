using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[Serializable]
public class WeaponGen : MonoBehaviour
{
    public GameObject point;

    public SwordTemplate sword;
    public MaceTemplate mace;

    private GameObject[] _weapons;
    private Mesh[] _weaponMeshes;

    public int weaponSeed;
    public DimensionModifier[] dimensionModifiers;
    public MaterialModifier[] materialModifiers;

    // Start is called before the first frame update
    void Start()
    {
        // Create 5x5 grid of weapons
        _weapons = new GameObject[25];
        _weaponMeshes = new Mesh[25];
        for (var i = 0; i < _weapons.Length; i++)
        {
            sword.SetDimensionsSeed(weaponSeed + i);
            sword.UpdateDimensionsMods(dimensionModifiers);
            sword.UpdateMaterialMods(materialModifiers.OrderBy(x => Random.value).Take(2).ToArray());
            var pos = (i % 5) * 2 * Vector3.right + (i / 5) * 2 * Vector3.forward;
            _weapons[i] = sword.CreateObject(pos);
            _weaponMeshes[i] = _weapons[i].GetComponent<MeshFilter>().mesh;
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (var i = 0; i < _weapons.Length; i++)
        {
            sword.SetDimensionsSeed(weaponSeed + i);
            sword.UpdateDimensionsMods(dimensionModifiers);
            sword.UpdateMesh(_weaponMeshes[i]);
        }
    }
}

[Serializable]
public struct DimensionModifier
{
    // Implementation to be handled by individual template
    public string name;
    public float scale;
    public float heightScale;
    public float widthScale;
    public float depthScale;
}

[Serializable]
public struct MaterialModifier
{
    public string name;
    public Material material;
}


public abstract class WeaponTemplate
{
    public abstract Material Material { get; }
    public abstract string Name { get; }

    private Vector3[] _compactVertices;
    private int[] _compactTriangles;
    private Material _material;

    public abstract Vector3[] GetVertices();

    public abstract int[] GetTriangles();

    public abstract void SetDimensionsSeed(int seed);


    public virtual void UpdateMaterialMods(MaterialModifier[] mods)
    {
        // Blends all materials together
        
        if (!_material)
            _material = new Material(Material);

        _material.Lerp(Material,
            Helpers.MaterialAverage(mods.Select(mod => mod.material).ToArray()),
            0.5f);
    }

    public virtual GameObject CreateObject()
    {
        _compactTriangles = GetTriangles();
        _compactVertices = GetVertices();
        var meshInfo = Helpers.FixMesh(_compactVertices, _compactTriangles);

        // Create mesh
        var mesh = new Mesh
        {
            vertices = meshInfo.vertices,
            triangles = meshInfo.triangles
        };
        mesh.RecalculateNormals();

        // Create GameObject
        var gameObject = new GameObject(Name, typeof(MeshFilter), typeof(MeshRenderer));

        // Assign mesh
        gameObject.GetComponent<MeshFilter>().mesh = mesh;
        gameObject.GetComponent<MeshRenderer>().material.CopyPropertiesFromMaterial(_material);

        return gameObject;
    }

    public virtual GameObject CreateObject(Vector3 pos)
    {
        var obj = CreateObject();
        obj.transform.position = pos;
        return obj;
    }

    public virtual void UpdateMesh(Mesh mesh)
    {
        _compactVertices = GetVertices();
        var meshInfo = Helpers.FixMesh(_compactVertices, _compactTriangles);

        mesh.Clear();
        mesh.vertices = meshInfo.vertices;
        mesh.triangles = meshInfo.triangles;
        mesh.RecalculateNormals();
    }

    public virtual GameObject[] DrawPoints(GameObject point, GameObject parent)
    {
        var points = new GameObject[_compactVertices.Length];
        var i = 0;
        foreach (var vertex in _compactVertices)
        {
            var obj = Object.Instantiate(point, vertex, Quaternion.identity);
            points[i] = obj;
            obj.name = "Point " + i;
            obj.transform.parent = parent.transform;
            i++;
        }

        return points;
    }

    public virtual void UpdatePoints(GameObject[] points)
    {
        for (var i = 0; i < _compactVertices.Length; i++)
            points[i].transform.localPosition = _compactVertices[i];
    }
}

[Serializable]
public class SwordTemplate : WeaponTemplate
{
    public NormalRange height;
    public NormalRange width;
    public NormalRange depth;
    public NormalRange heightInnerRatio;
    public NormalRange depthInnerRatio;

    private float _height;
    private float _depth;
    private float _width;
    private float _heightInnerRatio;
    private float _widthInnerRatio;

    public Material defaultMaterial;

    public override Material Material => defaultMaterial;

    public override string Name => "Sword";

    private const int NumFaces = 18;


    public SwordTemplate(int seed)
    {
        SetDimensionsSeed(seed);
    }

    public override void SetDimensionsSeed(int seed)
    {
        // Generate weapon dimensions from a seed
        var rand = new HelperRandom(seed);

        _height = (float) rand.NormalValue(height);
        _width = (float) rand.NormalValue(width);
        _depth = (float) rand.NormalValue(depth);
        _heightInnerRatio = (float) rand.NormalValue(heightInnerRatio);
        _widthInnerRatio = (float) rand.NormalValue(depthInnerRatio);
    }

    public void UpdateDimensionsMods(DimensionModifier[] mods)
    {
        foreach (var mod in mods)
        {
            _height *= mod.scale * mod.heightScale;
            _depth *= mod.scale * mod.depthScale;
            _width *= mod.scale * mod.widthScale;
        }
    }

    public override Vector3[] GetVertices()
    {
        var vertices = new Vector3[14];

        // "Unit" vectors
        var left = Vector3.left * (_width / 2);
        var right = Vector3.right * (_width / 2);
        var forward = Vector3.forward * (_depth / 2);
        var back = Vector3.back * (_depth / 2);
        var up = Vector3.up * _height;

        var leftInner = left * _widthInnerRatio;
        var rightInner = right * _widthInnerRatio;
        var upInner = up * _heightInnerRatio;


        vertices[0] = Vector3.zero; // origin
        vertices[1] = up; // tip

        // Lower outline
        vertices[2] = left;
        vertices[3] = leftInner + forward;
        vertices[4] = rightInner + forward;
        vertices[5] = right;
        vertices[6] = rightInner + back;
        vertices[7] = leftInner + back;

        // Upper outline
        for (var i = 2; i < 8; i++)
            vertices[i + 6] = vertices[i] + upInner;

        return vertices;
    }

    public override int[] GetTriangles()
    {
        var triangles = new int[NumFaces * 3];
        var curIndex = 0;

        // Tip faces
        for (var i = 8; i < 14; i++)
        {
            triangles[curIndex] = 1; // tip
            triangles[curIndex + 1] = i;
            triangles[curIndex + 2] = Helpers.Wrap(i + 1, 8, 14);
            curIndex += 3;
        }

        // side faces
        for (var i = 2; i < 8; i++)
        {
            // triangle 1
            triangles[curIndex] = i;
            triangles[curIndex + 1] = Helpers.Wrap(i + 1, 2, 8);
            triangles[curIndex + 2] = i + 6;

            // triangle 2
            triangles[curIndex + 3] = Helpers.Wrap(i + 1, 2, 8) + 6;
            triangles[curIndex + 4] = i + 6;
            triangles[curIndex + 5] = Helpers.Wrap(i + 1, 2, 8);

            curIndex += 6;
        }

        return triangles;
    }
}

[Serializable]
public class MaceTemplate : WeaponTemplate
{
    public NormalRange height;
    public NormalRange width;
    public NormalRange depth;
    public NormalRange spikeHeightRatio;
    public NormalRange spikeWidthRatio;
    public NormalRange spikeDepthRatio;


    private float _height;
    private float _width;
    private float _depth;
    private float _spikeHeightRatio;
    private float _spikeWidthRatio;
    private float _spikeDepthRatio;

    private const int NumFaces = 30;

    public Material defaultMaterial;

    public override Material Material => defaultMaterial;
    public override string Name => "Mace";

    public override Vector3[] GetVertices()
    {
        var vertices = new Vector3[14];

        // "Unit" Vectors
        var left = Vector3.left * (_width / 2);
        var right = Vector3.right * (_width / 2);
        var forward = Vector3.forward * (_depth / 2);
        var back = Vector3.back * (_depth / 2);
        var up = Vector3.up * _height;

        var leftInner = left * _spikeWidthRatio;
        var rightInner = right * _spikeWidthRatio;
        var forwardInner = forward * _spikeDepthRatio;
        var backInner = back * _spikeDepthRatio;
        var upInner = up * _spikeHeightRatio;

        // Origin
        vertices[0] = Vector3.zero;

        // Inner box base
        vertices[1] = leftInner + forwardInner;
        vertices[2] = rightInner + forwardInner;
        vertices[3] = rightInner + backInner;
        vertices[4] = leftInner + backInner;

        // Inner box top
        for (var i = 0; i < 4; i++)
        {
            vertices[i + 5] = vertices[i + 1] + upInner;
        }

        // Spike tips
        vertices[9] = left + upInner / 2;
        vertices[10] = forward + upInner / 2;
        vertices[11] = right + upInner / 2;
        vertices[12] = back + upInner / 2;
        vertices[13] = up;

        return vertices;
    }

    public override int[] GetTriangles()
    {
        var triangles = new int[NumFaces * 3];
        var curIndex = 0;

        // Bottom
        for (var i = 0; i < 4; i++)
        {
            triangles[curIndex] = 0;
            triangles[curIndex + 2] = i + 1;
            triangles[curIndex + 1] = Helpers.Wrap(i + 2, 1, 5);
            curIndex += 3;
        }

        // Sides
        for (var i = 0; i < 4; i++)
        {
            // Get corners for this side
            var offsets = new int[]
            {
                i + 1,
                i + 5,
                Helpers.Wrap(i + 6, 5, 9),
                Helpers.Wrap(i + 2, 1, 5),
            };

//            var str = offsets.Aggregate("", (current, ind) => current + (ind + " "));
//            Debug.Log(str);

            // Create faces for this side
            for (var j = 0; j < 4; j++)
            {
                triangles[curIndex] = Helpers.Wrap(10 + i, 9, 13);
                triangles[curIndex + 1] = offsets[Helpers.Wrap(j + 1, 0, 4)];
                triangles[curIndex + 2] = offsets[j];
                curIndex += 3;
            }
        }

        // Top
        for (var i = 0; i < 4; i++)
        {
            triangles[curIndex] = 13;
            triangles[curIndex + 1] = i + 5;
            triangles[curIndex + 2] = Helpers.Wrap(i + 6, 5, 9);
            curIndex += 3;
        }


        return triangles;
    }

    public override void SetDimensionsSeed(int seed)
    {
        // Generate weapon dimensions from a seed
        var rand = new HelperRandom(seed);
        _height = (float) rand.NormalValue(height);
        _width = (float) rand.NormalValue(width);
        _depth = (float) rand.NormalValue(depth);
        _spikeHeightRatio = (float) rand.NormalValue(spikeHeightRatio);
        _spikeWidthRatio = (float) rand.NormalValue(spikeWidthRatio);
        _spikeDepthRatio = (float) rand.NormalValue(spikeDepthRatio);
    }

    public void UpdateDimensionsMods(DimensionModifier[] mods)
    {
        foreach (var mod in mods)
        {
            _height *= mod.scale * mod.heightScale;
            _depth *= mod.scale * mod.depthScale;
            _width *= mod.scale * mod.widthScale;
        }
    }
}