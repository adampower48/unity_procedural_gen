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
    private GameObject[] _points;
    private GameObject[] _points2;

    public SwordTemplate sword;
    private GameObject _swordGo;
    private Mesh _swordMesh;

    public MaceTemplate mace;
    private GameObject _maceGo;
    private Mesh _maceMesh;

    private WeaponTemplate[] _weapons;
    private Mesh[] _weaponMeshes;

    public int weaponSeed;


    // Start is called before the first frame update
    void Start()
    {
        sword.SetDimensionsSeed(weaponSeed);
        _swordGo = sword.CreateObject();
        _swordMesh = _swordGo.GetComponent<MeshFilter>().mesh;
        _points = sword.DrawPoints(point, _swordGo);


        mace.SetDimensionsSeed(weaponSeed);
        _maceGo = mace.CreateObject();
        _maceMesh = _maceGo.GetComponent<MeshFilter>().mesh;
        _points2 = mace.DrawPoints(point, _maceGo);

        _maceGo.transform.position += Vector3.right * 2;
    }

    // Update is called once per frame
    void Update()
    {
        sword.SetDimensionsSeed(weaponSeed);
        sword.UpdateMesh(_swordMesh);
        sword.UpdatePoints(_points);

        mace.SetDimensionsSeed(weaponSeed);
        mace.UpdateMesh(_maceMesh);
        mace.UpdatePoints(_points2);
    }
}


public abstract class WeaponTemplate
{
    public abstract Material Material { get; }
    public abstract string Name { get; }

    private Vector3[] _compactVertices;
    private int[] _compactTriangles;

    public abstract Vector3[] GetVertices();

    public abstract int[] GetTriangles();

    public abstract void SetDimensionsSeed(int seed);

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
        gameObject.GetComponent<MeshRenderer>().material = Material;

        return gameObject;
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

    private float _height;
    private float _depth;
    private float _width;
    [Range(0, 1)] public float heightInnerRatio;
    [Range(0, 1)] public float widthInnerRatio;

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
        heightInnerRatio = rand.FloatRange(0, 1);
        widthInnerRatio = rand.FloatRange(0, 1);
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

        var leftInner = left * widthInnerRatio;
        var rightInner = right * widthInnerRatio;
        var upInner = up * heightInnerRatio;


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
    
    
    private float _height;
    private float _width;
    private float _depth;
    public float spikeHeightRatio;
    public float spikeWidthRatio;
    public float spikeDepthRatio;

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

        var leftInner = left * spikeWidthRatio;
        var rightInner = right * spikeWidthRatio;
        var forwardInner = forward * spikeDepthRatio;
        var backInner = back * spikeDepthRatio;
        var upInner = up * spikeHeightRatio;

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
        spikeHeightRatio = rand.FloatRange(0.2f, 1);
        spikeWidthRatio = rand.FloatRange(0.2f, 1);
        spikeDepthRatio = rand.FloatRange(0.2f, 1);
    }
}