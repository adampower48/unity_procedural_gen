using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

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


    // Start is called before the first frame update
    void Start()
    {
        _swordGo = sword.CreateObject();
        _swordMesh = _swordGo.GetComponent<MeshFilter>().mesh;
        _points = sword.DrawPoints(point, _swordGo);
        
        
        _maceGo = mace.CreateObject();
        _maceMesh = _maceGo.GetComponent<MeshFilter>().mesh;
        _points2 = mace.DrawPoints(point, _maceGo);

        _maceGo.transform.position += Vector3.right * 2;

    }

    // Update is called once per frame
    void Update()
    {
        sword.UpdateMesh(_swordMesh);
        sword.UpdatePoints(_points, _swordMesh.vertices);
        
        mace.UpdateMesh(_maceMesh);
        mace.UpdatePoints(_points2, _maceMesh.vertices);
    }
}


public abstract class WeaponTemplate
{
    public abstract Material Material { get; }
    public abstract string Name { get; }

    public abstract Vector3[] GetVertices();

    public abstract int[] GetTriangles();

    public virtual GameObject CreateObject()
    {
        var triangles = GetTriangles();
        var vertices = GetVertices();
        var meshInfo = Helpers.FixMesh(vertices, triangles);

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
        var vertices = GetVertices();
        var triangles = GetTriangles();
        var meshInfo = Helpers.FixMesh(vertices, triangles);

//        var triangles = mesh.triangles;
        mesh.Clear();
        mesh.vertices = meshInfo.vertices;
        mesh.triangles = meshInfo.triangles;
        mesh.RecalculateNormals();
    }

    public virtual GameObject[] DrawPoints(GameObject point, GameObject parent)
    {
        var vertices = parent.GetComponent<MeshFilter>().mesh.vertices;
        var points = new GameObject[vertices.Length];
        var i = 0;
        foreach (var vertex in vertices)
        {
            var obj = Object.Instantiate(point, vertex, Quaternion.identity);
            points[i] = obj;
            obj.name = "Point " + i;
            obj.transform.parent = parent.transform;
            i++;
        }

        return points;
    }

    public virtual void UpdatePoints(GameObject[] points, Vector3[] vertices)
    {
        for (var i = 0; i < vertices.Length; i++)
            points[i].transform.localPosition = vertices[i];
    }
}

[Serializable]
public class SwordTemplate : WeaponTemplate
{
    [Range(0, 10)] public float height;
    [Range(0, 1)] public float heightInnerRatio;
    [Range(0, 10)] public float depth;
    [Range(0, 10)] public float width;
    [Range(0, 1)] public float widthInnerRatio;

    public Material defaultMaterial;

    public override Material Material => defaultMaterial;

    public override string Name => "Sword";

    private const int NumFaces = 18;
    private float _widthInner;
    private float _heightInner;

    public SwordTemplate()
    {
    }

    public SwordTemplate(int seed)
    {
        var rand = new System.Random(seed);
        // todo: derive dimensions from seed
        
        
    }

    public override Vector3[] GetVertices()
    {
        _widthInner = width * widthInnerRatio;
        _heightInner = height * heightInnerRatio;


        var vertices = new Vector3[14];
        vertices[0] = Vector3.zero; // origin
        vertices[1] = Vector3.up * height; // tip

        // Lower outline
        var left = Vector3.left * (width / 2);
        var forward = Vector3.forward * (depth / 2);

        vertices[2] = left; // left
        vertices[3] = left * _widthInner + forward; // left forward
        vertices[4] = -left * _widthInner + forward; // right forward
        vertices[5] = -left; // right
        vertices[6] = -left * _widthInner - forward; // right backward
        vertices[7] = left * _widthInner - forward; // left backward

        // Upper outline
        var up = Vector3.up * _heightInner;
        for (var i = 2; i < 8; i++)
            vertices[i + 6] = vertices[i] + up;

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
    public float height;
    public float width;
    public float depth;
    public float spikeHeightRatio;
    public float spikeWidthRatio;
    public float spikeDepthRatio;


    private float _heightInner;
    private float _widthInner;
    private float _depthInner;
    private const int NumFaces = 30;


    public Material defaultMaterial;

    public override Material Material => defaultMaterial;
    public override string Name => "Mace";

    public override Vector3[] GetVertices()
    {
        _heightInner = height * spikeHeightRatio;
        _widthInner = width * spikeWidthRatio;
        _depthInner = depth * spikeDepthRatio;

        var vertices = new Vector3[14];

        // Origin
        vertices[0] = Vector3.zero;

        // Inner box base
        vertices[1] = Vector3.forward * (_depthInner / 2) + Vector3.left * (_widthInner / 2);
        vertices[2] = Vector3.forward * (_depthInner / 2) + Vector3.right * (_widthInner / 2);
        vertices[3] = Vector3.back * (_depthInner / 2) + Vector3.right * (_widthInner / 2);
        vertices[4] = Vector3.back * (_depthInner / 2) + Vector3.left * (_widthInner / 2);

        // Inner box top
        for (var i = 0; i < 4; i++)
        {
            vertices[i + 5] = vertices[i + 1] + Vector3.up * _heightInner;
        }

        // Spike tips
        vertices[9] = Vector3.left * (width / 2) + Vector3.up * (_heightInner / 2);
        vertices[10] = Vector3.forward * (depth / 2) + Vector3.up * (_heightInner / 2);
        vertices[11] = Vector3.right * (width / 2) + Vector3.up * (_heightInner / 2);
        vertices[12] = Vector3.back * (depth / 2) + Vector3.up * (_heightInner / 2);
        vertices[13] = Vector3.up * height;

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
}