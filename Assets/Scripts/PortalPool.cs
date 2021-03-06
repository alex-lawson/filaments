﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PortalPool : MonoBehaviour {

    public Light PoolLight;
    public GameObject PoolCover;
    public float UncoverTime;
    public IntRange RotationalSymmetry;
    public float OuterRadius;
    public float InnerRadius;
    public float LipHeight;
    public float PoolDepth;

    private List<Vector3> vertices;
    private List<int> triangles;

    public void Uncover() {
        StopAllCoroutines();
        StartCoroutine(DoUncover());
    }

    public void Generate(int seed) {
        StopAllCoroutines();

        GenerateMesh(seed);

        GenerateCoverMesh(seed);

        PoolCover.transform.localPosition = new Vector3(0, LipHeight, 0);

        var cs = GameObject.FindGameObjectWithTag("ColorSchemer").GetComponent<ColorSchemer>();
        Color poolLightColor = Color.HSVToRGB(cs.BaseHues[0], 0.8f, 1.0f);
        PoolLight.color = poolLightColor;
    }

    private IEnumerator DoUncover() {
        var wfeof = new WaitForEndOfFrame();

        float top = LipHeight;
        float bottom = -PoolDepth - 0.01f;

        float timer = 0;
        while (timer < UncoverTime) {
            float ratio = timer / UncoverTime;
            float y = Mathf.Lerp(top, bottom, ratio);
            PoolCover.transform.localPosition = new Vector3(0, y, 0);

            timer += Time.deltaTime;

            yield return wfeof;
        }
    }

    private void GenerateCoverMesh(int seed) {
        var oldState = Random.state;
        Random.InitState(seed);

        int rotationalSymmetry = RotationalSymmetry.RandomValue() * 2;

        vertices = new List<Vector3>();
        triangles = new List<int>();

        float sectionAngle = (Mathf.PI * 2) / rotationalSymmetry;
        float baseAngle = sectionAngle * 0.5f;

        List<Vector3> facePoints = new List<Vector3>();
        for (var i = 0; i < rotationalSymmetry; i++) {
            facePoints.Add(RadialPoint(baseAngle - i * sectionAngle, InnerRadius, 0));
        }

        AddFace(facePoints.ToArray());

        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        PoolCover.GetComponent<MeshFilter>().sharedMesh = mesh;

        var collider = PoolCover.GetComponent<MeshCollider>();
        if (collider != null) {
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
        }

        Random.state = oldState;
    }
    
    private void GenerateMesh(int seed) {
        var oldState = Random.state;
        Random.InitState(seed);

        int rotationalSymmetry = RotationalSymmetry.RandomValue() * 2;

        vertices = new List<Vector3>();
        triangles = new List<int>();

        float sectionAngle = (Mathf.PI * 2) / rotationalSymmetry;
        float baseAngle = sectionAngle * 0.5f;

        Vector3 lr = Vector3.zero;
        Vector3 ll = Vector3.zero;
        Vector3 ul = Vector3.zero;
        Vector3 ur = Vector3.zero;

        // create outer lip face

        ll = RadialPoint(baseAngle, OuterRadius, 0);
        lr = RadialPoint(baseAngle + sectionAngle, OuterRadius, 0);

        ul = RadialPoint(baseAngle, OuterRadius, LipHeight);
        ur = RadialPoint(baseAngle + sectionAngle, OuterRadius, LipHeight);

        AddFace(new Vector3[] { ul, ur, lr, ll });

        // create top surface

        ll = ul;
        lr = ur;

        ul = RadialPoint(baseAngle, InnerRadius, LipHeight);
        ur = RadialPoint(baseAngle + sectionAngle, InnerRadius, LipHeight);

        AddFace(new Vector3[] { ul, ur, lr, ll });

        // create pool vertical surface

        ll = ul;
        lr = ur;

        ul = RadialPoint(baseAngle, InnerRadius, -PoolDepth);
        ur = RadialPoint(baseAngle + sectionAngle, InnerRadius, -PoolDepth);

        AddFace(new Vector3[] { ul, ur, lr, ll });

        // create pool bottom

        ll = ul;
        lr = ur;

        Vector3 poolCenter = new Vector3(0, -PoolDepth, 0);

        AddFace(new Vector3[] { ur, ul, poolCenter });

        AddRotations(sectionAngle, rotationalSymmetry);

        // create final mesh

        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;

        var collider = GetComponent<MeshCollider>();
        if (collider != null) {
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
        }

        Random.state = oldState;
    }

    private Vector3 RadialPoint(float angle, float radius, float y) {
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        return new Vector3(x, y, z);
    }
    
    // v v v these methods modify the main vertex/tri lists v v v

    private int AddVertex(Vector3 vertexPosition) {
        int newIndex = vertices.Count;
        vertices.Add(vertexPosition);
        return newIndex;
    }

    private int[] AddVertices(Vector3[] toAdd) {
        int[] vIds = new int[toAdd.Length];
        for (int i = 0; i < toAdd.Length; i++) {
            vIds[i] = AddVertex(toAdd[i]);
        }
        return vIds;
    }

    private void AddFace(Vector3[] facePoints) {
        int[] faceIds = AddVertices(facePoints);

        for (int i = 2; i < faceIds.Length; i++)
            AddTriangle(faceIds[0], faceIds[i - 1], faceIds[i]);
    }

    private void AddTriangle(int a, int b, int c) {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    private void AddRotations(float angle, int iterations) {
        List<Vector3> copyVertices = new List<Vector3>(vertices);
        List<int> copyTriangles = new List<int>(triangles);

        for (var ri = 1; ri < iterations; ri++) {
            Quaternion rotation = Quaternion.AngleAxis(angle * ri * Mathf.Rad2Deg, Vector3.up);

            List<int> newVertexIds = new List<int>();
            for (var i = 0; i < copyVertices.Count; i++) {
                var v = copyVertices[i];
                newVertexIds.Add(AddVertex(rotation * v));
            }

            int triCount = copyTriangles.Count / 3;
            for (var i = 0; i < triCount; i++) {
                int j = i * 3;
                AddTriangle(newVertexIds[copyTriangles[j]], newVertexIds[copyTriangles[j + 1]], newVertexIds[copyTriangles[j + 2]]);
            }
        }
    }
}
