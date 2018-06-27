using System;
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

    public void Uncover() {
        StopAllCoroutines();
        StartCoroutine(DoUncover());
    }

    public void Generate(int seed) {
        StopAllCoroutines();

        GeneratePoolMesh(seed);

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

        // double this because triangular portals are too awkward
        int rotationalSymmetry = RotationalSymmetry.RandomValue() * 2;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float sectionAngle = (Mathf.PI * 2) / rotationalSymmetry;
        float baseAngle = sectionAngle * 0.5f;

        List<Vector3> facePoints = new List<Vector3>();
        for (var i = 0; i < rotationalSymmetry; i++)
            facePoints.Add(MeshGen.RadialPoint(baseAngle - i * sectionAngle, InnerRadius, 0));

        MeshGen.AddFace(facePoints, ref vertices, ref triangles);

        Mesh mesh = MeshGen.BuildMesh(vertices, triangles);

        MeshGen.AssignMesh(PoolCover, mesh);

        Random.state = oldState;
    }
    
    private void GeneratePoolMesh(int seed) {
        var oldState = Random.state;
        Random.InitState(seed);

        // double this because triangular portals are too awkward
        int rotationalSymmetry = RotationalSymmetry.RandomValue() * 2;

        List<Vector3> sliceVertices = new List<Vector3>();
        List<int> sliceTriangles = new List<int>();

        float sliceAngle = (Mathf.PI * 2) / rotationalSymmetry;
        float baseAngle = sliceAngle * 0.5f;

        Vector3 lr = Vector3.zero;
        Vector3 ll = Vector3.zero;
        Vector3 ul = Vector3.zero;
        Vector3 ur = Vector3.zero;

        // create outer lip face

        ll = MeshGen.RadialPoint(baseAngle, OuterRadius, 0);
        lr = MeshGen.RadialPoint(baseAngle + sliceAngle, OuterRadius, 0);

        ul = MeshGen.RadialPoint(baseAngle, OuterRadius, LipHeight);
        ur = MeshGen.RadialPoint(baseAngle + sliceAngle, OuterRadius, LipHeight);

        MeshGen.AddFace(new List<Vector3> { ul, ur, lr, ll }, ref sliceVertices, ref sliceTriangles);

        // create top surface

        ll = ul;
        lr = ur;

        ul = MeshGen.RadialPoint(baseAngle, InnerRadius, LipHeight);
        ur = MeshGen.RadialPoint(baseAngle + sliceAngle, InnerRadius, LipHeight);

        MeshGen.AddFace(new List<Vector3> { ul, ur, lr, ll }, ref sliceVertices, ref sliceTriangles);

        // create pool vertical surface

        ll = ul;
        lr = ur;

        ul = MeshGen.RadialPoint(baseAngle, InnerRadius, -PoolDepth);
        ur = MeshGen.RadialPoint(baseAngle + sliceAngle, InnerRadius, -PoolDepth);

        MeshGen.AddFace(new List<Vector3> { ul, ur, lr, ll }, ref sliceVertices, ref sliceTriangles);

        // create pool bottom

        ll = ul;
        lr = ur;

        Vector3 poolCenter = new Vector3(0, -PoolDepth, 0);

        MeshGen.AddFace(new List<Vector3> { ur, ul, poolCenter }, ref sliceVertices, ref sliceTriangles);

        List<Vector3> finalVertices = new List<Vector3>(sliceVertices);
        List<int> finalTriangles = new List<int>(sliceTriangles);

        for (var i = 1; i < rotationalSymmetry; i++) {
            float angle = i * sliceAngle;
            List<Vector3> rotatedVertices = MeshGen.RotateVertices(sliceVertices, angle, Vector3.up, Vector3.zero);
            MeshGen.CombineLists(rotatedVertices, sliceTriangles, ref finalVertices, ref finalTriangles);
        }

        // create and assign final mesh

        Mesh mesh = MeshGen.BuildMesh(finalVertices, finalTriangles);

        MeshGen.AssignMesh(gameObject, mesh);

        Random.state = oldState;
    }
}
