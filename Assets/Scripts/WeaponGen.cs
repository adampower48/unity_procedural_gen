using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class WeaponGen : MonoBehaviour
{
    public GameObject point;
    public SwordTemplate sword;

    private GameObject swordGO;
    private Mesh swordMesh;
    private GameObject[] points;

    // Start is called before the first frame update
    void Start()
    {
        var vertices = sword.GetVertices();
        var triangles = sword.GetTriangles();
        points = new GameObject[vertices.Length];

        // Create mesh
        Mesh mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateNormals();

        // Create sword GameObject
        swordGO = new GameObject("Sword", typeof(MeshFilter), typeof(MeshRenderer));

        // Assign mesh
        swordMesh = mesh;
        swordGO.GetComponent<MeshFilter>().mesh = mesh;
        swordGO.GetComponent<MeshRenderer>().material = sword.material;

        // Points
        var i = 0;
        foreach (var vertex in sword.GetVertices())
        {
            //            Debug.Log(vertex);
            var obj = Instantiate(point, vertex, Quaternion.identity);
            points[i] = obj;
            obj.name = "Point " + i.ToString();
            obj.transform.parent = swordGO.transform;
            i++;
        }

        foreach (var vertex in sword.GetTriangles())
        {
            Debug.Log(vertex);
        }
    }

    // Update is called once per frame
    void Update()
    {
        var vertices = sword.GetVertices();
        var triangles = swordMesh.triangles;
        swordMesh.Clear();
        swordMesh.vertices = vertices;
        swordMesh.triangles = triangles;
        swordMesh.RecalculateNormals();

        for (int i = 0; i < vertices.Length; i++)
        {
            points[i].transform.position = vertices[i];
        }

    }
}

[Serializable]
public class SwordTemplate
{
    [Range(0, 10)]
    public float height;
    [Range(0, 1)]
    public float heightInnerRatio;
    [Range(0, 10)]
    public float depth;
    [Range(0, 10)]
    public float width;
    [Range(0, 1)]
    public float widthInnerRatio;

    public Material material;

    private int numFaces = 18;
    private float widthInner;
    private float heightInner;

    public SwordTemplate() { }

    public SwordTemplate(int seed)
    {
        System.Random rand = new System.Random(seed);
    }

    public Vector3[] GetVertices()
    {
        widthInner = width * widthInnerRatio;
        heightInner = height * heightInnerRatio;


        Vector3[] vertices = new Vector3[14];
        vertices[0] = Vector3.zero; // origin
        vertices[1] = Vector3.up * height; // tip

        // Lower outline
        var left = Vector3.left * width / 2;
        var forward = Vector3.forward * depth / 2;

        vertices[2] = left; // left
        vertices[3] = left * widthInner + forward; // left forward
        vertices[4] = -left * widthInner + forward; // right forward
        vertices[5] = -left; // right
        vertices[6] = -left * widthInner - forward; // right backward
        vertices[7] = left * widthInner - forward; // left backward

        // Upper outline
        var up = Vector3.up * heightInner;
        for (int i = 2; i < 8; i++) vertices[i + 6] = vertices[i] + up;

        return vertices;
    }

    public int[] GetTriangles()
    {
        int[] triangles = new int[numFaces * 3];
        var curIndex = 0;

        // Tip faces
        for (int i = 8; i < 14; i++)
        {
            triangles[curIndex] = 1; // tip
            triangles[curIndex + 1] = i;
            triangles[curIndex + 2] = Helpers.Wrap(i + 1, 8, 14);
            curIndex += 3;
        }

        // side faces
        for (int i = 2; i < 8; i++)
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