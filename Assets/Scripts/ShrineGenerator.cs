﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class ShrineGenerationTarget {
    public string AnchorTag;
    public ShrineGenerationConfig Config;
}

public class ShrineGenerator : MonoBehaviour {

    public GameObject ShrinePrefab;
    public ShrineGenerationTarget[] Targets;

    private List<GameObject> currentShrineInstances = new List<GameObject>();
    private int currentMeshId = 0;
    private List<Vector3> vertices;
    private List<int>[] triangles;

    public void Generate(int seed) {
        Clear();

        foreach (var target in Targets) {
            var prototype = Instantiate(ShrinePrefab, transform);
            GenerateShrine(target.Config, seed, prototype);

            var anchorPoints = GameObject.FindGameObjectsWithTag(target.AnchorTag);
            foreach (var anchorPoint in anchorPoints) {
                var newInstance = Instantiate(prototype, anchorPoint.transform);
                currentShrineInstances.Add(newInstance);
            }

            Destroy(prototype);
        }
    }

    public void Clear() {
        for (int i = 0; i < currentShrineInstances.Count; i++)
            Destroy(currentShrineInstances[i]);
        currentShrineInstances.Clear();
    }

    public void GenerateShrine(ShrineGenerationConfig config, int seed, GameObject targetObject) {
        var oldState = Random.state;
        Random.InitState(seed);

        var targetMeshRenderer = targetObject.GetComponent<MeshRenderer>();

        vertices = new List<Vector3>();
        triangles = new List<int>[targetMeshRenderer.sharedMaterials.Length];
        for (int i = 0; i < triangles.Length; i++)
            triangles[i] = new List<int>();

        int rotationalSymmetry = config.RotationalSymmetry.RandomValue();
        bool bilateralSymmetry = false;
        //bilateralSymmetry = Random.value < 0.5f;

        float sectionAngle = (Mathf.PI * 2) / rotationalSymmetry;
        float faceAngle = bilateralSymmetry ? sectionAngle * 0.5f : sectionAngle;
        float tierAngle = 0;

        float twistAngle = 0;
        if (Random.value < config.GlobalTwistChance)
            twistAngle = config.TwistAngle.RandomValue() * (Random.value > 0.5 ? 1 : -1);
        //Debug.Log($"twistAngle {twistAngle}");

        float tierHeight = config.TierHeight.RandomValue();
        float baseY = 0f;
        float topY = tierHeight;

        float tierRadius = config.Radius.RandomValue();
        Vector3 lr = RadialPoint(tierAngle + faceAngle, tierRadius, 0);
        Vector3 ll = RadialPoint(tierAngle, tierRadius, 0);

        tierRadius = config.Radius.RandomValue();
        Vector3 ul = RadialPoint(tierAngle, tierRadius, topY);
        Vector3 ur = RadialPoint(tierAngle + faceAngle, tierRadius, topY);

        currentMeshId = RandomMeshId(config, false);

        AddFace(new Vector3[] { lr, ll, ul, ur });

        int tierCount = config.TierCount.RandomValue();
        for (var ti = 1; ti < tierCount; ti++) {
            bool offsetTier = Random.value < config.OffsetTierChance;

            tierRadius = config.Radius.RandomValue();
            if (offsetTier) {
                tierHeight = config.OffsetTierHeight.RandomValue();
            } else {
                tierHeight = config.TierHeight.RandomValue();
            }

            baseY = topY;
            topY += tierHeight;

            lr = ur;
            ll = ul;

            currentMeshId = RandomMeshId(config, ti == tierCount);

            if (offsetTier) {
                tierAngle += sectionAngle * 0.5f;

                ul = RadialPoint(tierAngle, tierRadius, topY);
                ur = RadialPoint(tierAngle + faceAngle, tierRadius, topY);

                AddFace(new Vector3[] { lr, ul, ur });
                AddFace(new Vector3[] { ul, lr, ll });
            } else {
                // twist
                if (Random.value < config.SegmentTwistChance)
                    tierAngle += twistAngle;
                
                ul = RadialPoint(tierAngle, tierRadius, topY);
                ur = RadialPoint(tierAngle + faceAngle, tierRadius, topY);

                Vector3[] facePoints = new Vector3[] { lr, ll, ul, ur };

                // extrude
                int extrudeCount = 0;
                if (Random.value < config.ExtrudeChance)
                    extrudeCount = config.ExtrudeSteps.RandomValue();

                for (int i = 0; i < extrudeCount; i++) {
                    currentMeshId = RandomMeshId(config, i == extrudeCount);

                    float extrudeDist = config.BranchSegmentLength.RandomValue();
                    Vector3 extrudeVec = Normal(facePoints);
                    Vector3 noiseDirection = Random.insideUnitSphere;
                    extrudeVec = Vector3.Lerp(extrudeVec, noiseDirection, config.BranchDirectionNoise);
                    extrudeVec = Vector3.Lerp(extrudeVec, Vector3.up, config.BranchVerticalBias.RandomValue());
                    extrudeVec *= extrudeDist;

                    Vector3[] newFacePoints = ExtrudePoints(facePoints, extrudeVec);
                    Vector3 newFaceCenter = Center(newFacePoints);

                    float scaleAmount = config.BranchSegmentScale.RandomValue();
                    newFacePoints = ScalePoints(newFacePoints, scaleAmount, newFaceCenter);

                    // twist
                    if (Random.value < config.SegmentTwistChance)
                        newFacePoints = RotatePoints(newFacePoints, twistAngle, extrudeVec, newFaceCenter);

                    AddTubeFaces(facePoints, newFacePoints);

                    facePoints = newFacePoints;
                }

                if (Random.value < config.PeakChance) {
                    float peakDistance = config.BranchPeakSize.RandomValue();
                    AddPeak(facePoints, peakDistance);
                } else {
                    AddFace(facePoints);
                }
            }
        }

        float peakHeight = config.PeakSize.RandomValue();
        Vector3 peakVertex = new Vector3(0, topY + peakHeight, 0);

        AddFace(new Vector3[] { ur, ul, peakVertex });

        //if (bilateralSymmetry)
        //    AddMirrored(vertices, triangles);

        for (int i = 1; i < rotationalSymmetry; i++)
            AddRotations(sectionAngle * i);

        Mesh mesh = new Mesh();
        mesh.subMeshCount = triangles.Length;
        for (int i = 0; i < vertices.Count; i++)
            vertices[i] *= config.FinalScale;
        mesh.vertices = vertices.ToArray();
        for (int meshId = 0; meshId < triangles.Length; meshId++) {
            mesh.SetTriangles(triangles[meshId].ToArray(), meshId);
        }
        mesh.RecalculateNormals();
        targetObject.GetComponent<MeshFilter>().sharedMesh = mesh;

        Random.state = oldState;
    }

    private Vector3 RadialPoint(float angle, float radius, float y) {
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        return new Vector3(x, y, z);
    }

    private Vector3 Center(Vector3[] points) {
        Vector3 res = Vector3.zero;
        for (var i = 0; i < points.Length; i++) {
            res += points[i];
        }
        res /= points.Length;
        return res;
    }

    private Vector3 Normal(Vector3[] points) {
        Vector3 res = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
        return res.normalized;
    }

    private Vector3[] ScalePoints(Vector3[] points, float scaleFactor, Vector3 center) {
        Vector3[] res = new Vector3[points.Length];
        for (var i = 0; i < points.Length; i++) {
            res[i] = Vector3.LerpUnclamped(center, points[i], scaleFactor);
        }
        return res;
    }

    private Vector3[] RotatePoints(Vector3[] points, float angle, Vector3 axis, Vector3 center) {
        Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis);
        Vector3[] res = new Vector3[points.Length];
        for (var i = 0; i < points.Length; i++) {
            Vector3 relPoint = points[i] - center;
            res[i] = center + rotation * relPoint;
        }
        return res;
    }

    private Vector3[] ExtrudePoints(Vector3[] facePoints, Vector3 offset) {
        Vector3[] newPoints = new Vector3[facePoints.Length];
        for (int i = 0; i < facePoints.Length; i++) {
            newPoints[i] = facePoints[i] + offset;
        }
        return newPoints;
    }

    // v v v these methods modify the main vertex/tri lists v v v

    private int AddVertex(Vector3 vertexPosition) {
        int newIndex = vertices.Count;
        vertices.Add(vertexPosition);
        return newIndex;
    }

    // given a list of vertex positions in a clockwise convex ring aligned
    // on a single plane, create a surface and add it to the mesh
    private void AddFace(Vector3[] facePoints) {
        int[] faceIds = AddVertices(facePoints);

        for (int i = 2; i < faceIds.Length; i++)
            AddTriangle(faceIds[0], faceIds[i - 1], faceIds[i]);
    }

    private void AddTubeFaces(Vector3[] startFacePoints, Vector3[] endFacePoints) {
        for (int i = 0; i < startFacePoints.Length; i++) {
            int j = (i + 1) % startFacePoints.Length;
            AddFace(new Vector3[] { startFacePoints[i], startFacePoints[j], endFacePoints[j], endFacePoints[i] });
        }
    }

    private void AddPeak(Vector3[] facePoints, float peakDistance) {
        Vector3 peakOffset = Normal(facePoints) * peakDistance;
        Vector3 peakPosition = Center(facePoints) + peakOffset;
        for (int i = 0; i < facePoints.Length; i++) {
            int j = (i + 1) % facePoints.Length;
            AddFace(new Vector3[] { peakPosition, facePoints[i], facePoints[j] });
        }
    }

    private int[] AddVertices(Vector3[] toAdd) {
        int[] vIds = new int[toAdd.Length];
        for (int i = 0; i < toAdd.Length; i++) {
            vIds[i] = AddVertex(toAdd[i]);
        }
        return vIds;
    }

    private void AddTriangle(int a, int b, int c) {
        triangles[currentMeshId].Add(a);
        triangles[currentMeshId].Add(b);
        triangles[currentMeshId].Add(c);
    }

    // copy the specified vertices and triangles, mirrored across the x axis
    private void AddMirrored() {
        for (int meshId = 0; meshId < triangles.Length; meshId++) {
            currentMeshId = meshId;
            List<Vector3> vList = new List<Vector3>(vertices);
            List<int> tList = new List<int>(triangles[meshId]);

            int vertexCount = vList.Count;
            int triCount = tList.Count / 3;

            List<int> newIds = new List<int>();
            for (var i = 0; i < vertexCount; i++) {
                var v = vList[i];
                newIds.Add(AddVertex(new Vector3(v.x, v.y, -v.z)));
            }

            for (var i = 0; i < triCount; i++) {
                int j = i * 3;
                AddTriangle(newIds[tList[j]], newIds[tList[j + 2]], newIds[tList[j + 1]]);
            }
        }
    }

    // copy the specified vertices and triangles, rotated by angle around the y axis
    private void AddRotations(float angle) {
        Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.up);

        List<int> newVertexIds = new List<int>();

        List<Vector3> vList = new List<Vector3>(vertices);
        int vertexCount = vList.Count;
        for (var i = 0; i < vertexCount; i++) {
            var v = vList[i];
            newVertexIds.Add(AddVertex(rotation * v));
        }

        for (int meshId = 0; meshId < triangles.Length; meshId++) {
            currentMeshId = meshId;
            
            List<int> tList = new List<int>(triangles[meshId]);
            int triCount = tList.Count / 3;
            for (var i = 0; i < triCount; i++) {
                int j = i * 3;
                AddTriangle(newVertexIds[tList[j]], newVertexIds[tList[j + 1]], newVertexIds[tList[j + 2]]);
            }
        }
    }

    private int RandomMeshId(ShrineGenerationConfig config, bool isEnd) {
        int res = 0;
        for (int i = 0; i < triangles.Length - 1; i++) {
            if (Random.value < (isEnd ? config.AccentChanceEnd : config.AccentChanceMid))
                res++;
            else
                return res;
        }
        return res;
    }
}