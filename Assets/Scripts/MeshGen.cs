using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGen {

    public static Mesh BuildMesh(List<Vector3> vertices, List<int> triangles) {
        Mesh mesh = new Mesh();

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        return mesh;
    }

    public static Mesh BuildMesh(List<Vector3> vertices, List<int>[] triangles) {
        Mesh mesh = new Mesh();

        mesh.SetVertices(vertices);

        mesh.subMeshCount = triangles.Length;
        for (int meshId = 0; meshId < triangles.Length; meshId++) {
            mesh.SetTriangles(triangles[meshId].ToArray(), meshId);
        }

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        return mesh;
    }

    public static void AssignMesh(GameObject targetObject, Mesh mesh) {
        targetObject.GetComponent<MeshFilter>().sharedMesh = mesh;

        var collider = targetObject.GetComponent<MeshCollider>();
        if (collider != null) {
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
        }
    }

    public static void AssignMesh(GameObject targetObject, Mesh mesh, Material[] materials) {
        targetObject.GetComponent<MeshRenderer>().materials = materials;
        AssignMesh(targetObject, mesh);
    }

    // add a vertex to the specified vertex list and return the index of the vertex added
    public static int AddVertex(Vector3 newVertex, ref List<Vector3> vertices) {
        int newIndex = vertices.Count;
        vertices.Add(newVertex);
        return newIndex;
    }

    // add a list of vertices to the specified vertex list and return an array of indices to the vertices added
    public static int[] AddVertices(List<Vector3> newVertices, ref List<Vector3> vertices) {
        int vc = newVertices.Count;
        int[] vIds = new int[vc];
        for (int i = 0; i < vc; i++) {
            vIds[i] = AddVertex(newVertices[i], ref vertices);
        }
        return vIds;
    }

    // add 3 vertex indices constituting a new triangle to the specified triangle list
    public static void AddTriangle(int a, int b, int c, ref List<int> triangles) {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    // combine the specified input lists with the specified output lists, adjusting vertex ids as necessary
    public static void CombineLists(List<Vector3> inVertices, List<int> inTriangles, ref List<Vector3> outVertices, ref List<int> outTriangles) {
        int[] newVertexIds = AddVertices(inVertices, ref outVertices);
        for (int i = 0; i < inTriangles.Count; i++)
            outTriangles.Add(newVertexIds[inTriangles[i]]);
    }

    // return a point at angle angle and height y on a vertical cylinder with the specified radius
    public static Vector3 RadialPoint(float angle, float radius, float y) {
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        return new Vector3(x, y, z);
    }

    // return a center (average) point of the specified vertices
    public static Vector3 Center(List<Vector3> vertices) {
        int vc = vertices.Count;
        Vector3 res = Vector3.zero;
        for (var i = 0; i < vc; i++)
            res += vertices[i];
        res /= vc;
        return res;
    }

    // return a normal vector for the specified face vertices (only considers the first 3 points)
    public static Vector3 Normal(List<Vector3> vertices) {
        Vector3 res = Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]);
        return res.normalized;
    }

    // return a copy of the specified vertices, scaled by scaleFactor around center
    public static List<Vector3> ScaleVertices(List<Vector3> vertices, float scaleFactor, Vector3 center) {
        List<Vector3> res = new List<Vector3>();
        for (var i = 0; i < vertices.Count; i++)
            res.Add(Vector3.LerpUnclamped(center, vertices[i], scaleFactor));
        return res;
    }

    // return a copy of the specified vertices, rotated by angle around the specified center and axis
    public static List<Vector3> RotateVertices(List<Vector3> vertices, float angle, Vector3 axis, Vector3 center) {
        Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis);
        List<Vector3> res = new List<Vector3>();
        for (var i = 0; i < vertices.Count; i++) {
            Vector3 relPoint = vertices[i] - center;
            res.Add(center + rotation * relPoint);
        }
        return res;
    }

    // return a copy of the specified vertices, translated by offset
    public static List<Vector3> TranslateVertices(List<Vector3> vertices, Vector3 offset) {
        List<Vector3> res = new List<Vector3>();
        for (int i = 0; i < vertices.Count; i++)
            res.Add(vertices[i] + offset);
        return res;
    }

    // return a copy of the specified vertices, mirrored across the plane ABC
    public static List<Vector3> MirrorVertices(List<Vector3> vertices, Vector3 a, Vector3 b, Vector3 c) {
        throw new System.NotImplementedException("MirrorVertices not yet implemented!");
    }

    // given a list of vertex positions in a clockwise convex ring aligned
    // on a single plane, create a surface and add it to the mesh
    public static void AddFace(List<Vector3> face, ref List<Vector3> vertices, ref List<int> triangles) {
        int[] faceIds = AddVertices(face, ref vertices);

        for (int i = 2; i < faceIds.Length; i++)
            AddTriangle(faceIds[0], faceIds[i - 1], faceIds[i], ref triangles);
    }

    // given a ring of clockwise vertex positions on a plane, add triangular faces in a fan to the specified center point
    public static void AddFanFaces(List<Vector3> face, Vector3 centerPoint, ref List<Vector3> vertices, ref List<int> triangles) {
        int vc = face.Count;
        for (int i = 0; i < vc; i++) {
            int j = (i + 1) % vc;
            AddFace(new List<Vector3> { centerPoint, face[i], face[j] }, ref vertices, ref triangles);
        }
    }

    // given two rings of clockwise vertex positions on a plane, add a tube of face quads connecting them
    public static void AddTubeFaces(List<Vector3> startVertices, List<Vector3> endVertices, ref List<Vector3> vertices, ref List<int> triangles) {
        int vc = startVertices.Count;
        for (int i = 0; i < vc; i++) {
            int j = (i + 1) % vc;
            AddFace(new List<Vector3> { startVertices[i], startVertices[j], endVertices[j], endVertices[i] }, ref vertices, ref triangles);
        }
    }

    // add a new vertex and faces forming a peak at the specified distance along the face's normal
    public static void AddPeak(List<Vector3> face, float peakDistance, ref List<Vector3> vertices, ref List<int> triangles) {
        Vector3 faceCenter = Center(face);
        Vector3 faceNormal = Normal(face);
        Vector3 peakPoint = faceCenter + faceNormal * peakDistance;
        AddFanFaces(face, peakPoint, ref vertices, ref triangles);
    }
}
