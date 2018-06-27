using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class ShrineGenerationTarget {
    public string AnchorTag;
    public ShrineGenerationConfig Config;
    public bool BeaconEnabled;
}

public class ShrineGenerator : MonoBehaviour {

    public GameObject ShrinePrefab;
    public ShrineGenerationTarget[] Targets;
    public Material[] Materials;

    private List<GameObject> currentShrineInstances = new List<GameObject>();

    // returns number of beacons placed
    public int Generate(int seed) {
        Clear();

        int beaconsPlaced = 0;
        foreach (var target in Targets) {
            var prototype = Instantiate(ShrinePrefab, transform);

            Bounds shrineBounds = GenerateShrine(target.Config, seed, prototype);

            var protoShrine = prototype.GetComponentInChildren<ShrineBeacon>();
            if (target.BeaconEnabled) {
                protoShrine.gameObject.SetActive(true);
                protoShrine.SetupBeacon(shrineBounds.size.y);
            } else {
                protoShrine.gameObject.SetActive(false);
            }
            
            var anchorPoints = GameObject.FindGameObjectsWithTag(target.AnchorTag);
            foreach (var anchorPoint in anchorPoints) {
                var newInstance = Instantiate(prototype, anchorPoint.transform);
                currentShrineInstances.Add(newInstance);

                if (target.BeaconEnabled)
                    beaconsPlaced++;
            }

            prototype.SetActive(false);

            Destroy(prototype);
        }

        //Debug.Log($"placed {beaconsPlaced} beacons");

        return beaconsPlaced;
    }

    public void Clear() {
        for (int i = 0; i < currentShrineInstances.Count; i++)
            Destroy(currentShrineInstances[i]);
        currentShrineInstances.Clear();
    }

    public Bounds GenerateShrine(ShrineGenerationConfig config, int seed, GameObject targetObject) {
        var oldState = Random.state;
        Random.InitState(seed);

        // start of setup stage

        // choose symmetry before anything else so that it can be shared by other generators
        int rotationalSymmetry = config.RotationalSymmetry.RandomValue();

        // create seeds to isolate static generation of various stages
        int plinthSeed = Random.Range(int.MinValue, int.MaxValue);
        int mainSeed = Random.Range(int.MinValue, int.MaxValue);
        int capSeed = Random.Range(int.MinValue, int.MaxValue);

        var sliceVertices = new List<Vector3>();
        var sliceTriangles = new List<int>[Materials.Length];
        for (int i = 0; i < sliceTriangles.Length; i++)
            sliceTriangles[i] = new List<int>();

        int currentMeshId = 0;

        float sliceAngle = (Mathf.PI * 2) / rotationalSymmetry;
        float currentAngle = 0;

        float twistAngle = 0;
        if (Random.value < config.GlobalTwistChance)
            twistAngle = config.TwistAngle.RandomValue() * (Random.value > 0.5 ? 1 : -1);
        //Debug.Log($"twistAngle {twistAngle}");

        float tierRadius = 0;
        float tierHeight = 0;
        float topY = 0;

        Vector3 lr = Vector3.zero;
        Vector3 ll = Vector3.zero;
        Vector3 ul = Vector3.zero;
        Vector3 ur = Vector3.zero;

        // end of setup stage, start of plinth stage

        Random.InitState(plinthSeed);

        tierRadius = config.PlinthBaseRadius.RandomValue();

        ul = MeshGen.RadialPoint(currentAngle, tierRadius, topY);
        ur = MeshGen.RadialPoint(currentAngle + sliceAngle, tierRadius, topY);

        float tierRadiusFactor = config.PlinthRadiusFactor.RandomValue();
        tierHeight = config.PlinthTierHeight.RandomValue();
        int plinthTierCount = config.PlinthTierCount.RandomValue();
        for (var pti = 0; pti < plinthTierCount; pti++) {
            // add vertical face

            topY += tierHeight;

            lr = ur;
            ll = ul;

            ul = MeshGen.RadialPoint(currentAngle, tierRadius, topY);
            ur = MeshGen.RadialPoint(currentAngle + sliceAngle, tierRadius, topY);

            MeshGen.AddFace(new List<Vector3> { ul, ur, lr, ll }, ref sliceVertices, ref sliceTriangles[currentMeshId]);

            // add horizontal face
            if (pti < plinthTierCount - 1) {
                if (twistAngle != 0) {
                    // find the minimum radius with twists
                    float apotheum = tierRadius * Mathf.Cos(Mathf.PI / rotationalSymmetry);
                    float innerAngle = Mathf.PI / rotationalSymmetry - Mathf.Abs(twistAngle);
                    float radiusRatio = 1 / Mathf.Cos(innerAngle);
                    float minRadius = apotheum * radiusRatio;
                    tierRadius = Mathf.Min(minRadius, tierRadius * tierRadiusFactor);

                    currentAngle += twistAngle;
                } else {
                    tierRadius *= tierRadiusFactor;
                }

                lr = ur;
                ll = ul;

                ul = MeshGen.RadialPoint(currentAngle, tierRadius, topY);
                ur = MeshGen.RadialPoint(currentAngle + sliceAngle, tierRadius, topY);

                MeshGen.AddFace(new List<Vector3> { ul, ur, lr, ll }, ref sliceVertices, ref sliceTriangles[currentMeshId]);
            }
        }

        ul = MeshGen.RadialPoint(currentAngle, tierRadius, topY);
        ur = MeshGen.RadialPoint(currentAngle + sliceAngle, tierRadius, topY);

        Vector3 plinthTopVertex = new Vector3(0, topY, 0);

        MeshGen.AddFace(new List<Vector3> { ur, ul, plinthTopVertex }, ref sliceVertices, ref sliceTriangles[currentMeshId]);

        // end of plinth stage, start of main stage

        Random.InitState(mainSeed);

        tierRadius = config.Radius.RandomValue();

        ul = MeshGen.RadialPoint(currentAngle, tierRadius, topY);
        ur = MeshGen.RadialPoint(currentAngle + sliceAngle, tierRadius, topY);

        int tierCount = config.TierCount.RandomValue();
        for (var ti = 1; ti < tierCount; ti++) {
            bool offsetTier = Random.value < config.OffsetTierChance;

            tierRadius = config.Radius.RandomValue();
            if (offsetTier) {
                tierHeight = config.OffsetTierHeight.RandomValue();
            } else {
                tierHeight = config.TierHeight.RandomValue();
            }

            topY += tierHeight;

            lr = ur;
            ll = ul;

            currentMeshId = RandomMeshId(config, ti == tierCount);

            if (offsetTier) {
                currentAngle += sliceAngle * 0.5f;

                ul = MeshGen.RadialPoint(currentAngle, tierRadius, topY);
                ur = MeshGen.RadialPoint(currentAngle + sliceAngle, tierRadius, topY);

                MeshGen.AddFace(new List<Vector3> { lr, ul, ur }, ref sliceVertices, ref sliceTriangles[currentMeshId]);
                MeshGen.AddFace(new List<Vector3> { ul, lr, ll }, ref sliceVertices, ref sliceTriangles[currentMeshId]);
            } else {
                if (Random.value < config.SegmentTwistChance)
                    currentAngle += twistAngle;

                ul = MeshGen.RadialPoint(currentAngle, tierRadius, topY);
                ur = MeshGen.RadialPoint(currentAngle + sliceAngle, tierRadius, topY);

                List<Vector3> facePoints = new List<Vector3> { ul, ur, lr, ll };

                int extrudeCount = 0;
                if (Random.value < config.ExtrudeChance)
                    extrudeCount = config.ExtrudeSteps.RandomValue();

                for (int i = 0; i < extrudeCount; i++) {
                    currentMeshId = RandomMeshId(config, i == extrudeCount);

                    float extrudeDist = config.BranchSegmentLength.RandomValue();
                    Vector3 extrudeVec = MeshGen.Normal(facePoints);
                    Vector3 noiseDirection = Random.insideUnitSphere;
                    extrudeVec = Vector3.Lerp(extrudeVec, noiseDirection, config.BranchDirectionNoise);
                    extrudeVec = Vector3.Lerp(extrudeVec, Vector3.up, config.BranchVerticalBias.RandomValue());
                    extrudeVec *= extrudeDist;

                    List<Vector3> newFacePoints = MeshGen.TranslateVertices(facePoints, extrudeVec);
                    Vector3 newFaceCenter = MeshGen.Center(newFacePoints);

                    float scaleAmount = config.BranchSegmentScale.RandomValue();
                    newFacePoints = MeshGen.ScaleVertices(newFacePoints, scaleAmount, newFaceCenter);

                    if (Random.value < config.SegmentTwistChance)
                        newFacePoints = MeshGen.RotateVertices(newFacePoints, twistAngle, extrudeVec, newFaceCenter);

                    MeshGen.AddTubeFaces(facePoints, newFacePoints, ref sliceVertices, ref sliceTriangles[currentMeshId]);

                    facePoints = newFacePoints;
                }

                if (Random.value < config.PeakChance) {
                    float peakDistance = config.BranchPeakSize.RandomValue();
                    MeshGen.AddPeak(facePoints, peakDistance, ref sliceVertices, ref sliceTriangles[currentMeshId]);
                } else {
                    MeshGen.AddFace(facePoints, ref sliceVertices, ref sliceTriangles[currentMeshId]);
                }
            }
        }

        // end of main stage, start of cap stage

        Random.InitState(capSeed);

        float peakHeight = config.PeakSize.RandomValue();
        Vector3 peakVertex = new Vector3(0, topY + peakHeight, 0);
        MeshGen.AddFace(new List<Vector3> { ur, ul, peakVertex }, ref sliceVertices, ref sliceTriangles[currentMeshId]);

        // end of cap stage, all randomization should be finished

        sliceVertices = MeshGen.ScaleVertices(sliceVertices, config.FinalScale, Vector3.zero);

        // rotate and copy slice to build final mesh

        List<Vector3> finalVertices = new List<Vector3>(sliceVertices);
        List<int>[] finalTriangles = new List<int>[sliceTriangles.Length];
        for (int i = 0; i < finalTriangles.Length; i++)
            finalTriangles[i] = new List<int>(sliceTriangles[i]);

        for (var i = 1; i < rotationalSymmetry; i++) {
            float angle = i * sliceAngle;
            List<Vector3> rotatedVertices = MeshGen.RotateVertices(sliceVertices, angle, Vector3.up, Vector3.zero);
            for (var j = 0; j < sliceTriangles.Length; j++)
                MeshGen.CombineLists(rotatedVertices, sliceTriangles[j], ref finalVertices, ref finalTriangles[j]);
        }

        Mesh mesh = MeshGen.BuildMesh(finalVertices, finalTriangles);

        // assign mesh and materials to target object

        targetObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        targetObject.GetComponent<MeshRenderer>().materials = Materials;

        var collider = targetObject.GetComponent<MeshCollider>();
        if (collider != null) {
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
        }

        Random.state = oldState;

        return mesh.bounds;
    }

    private int RandomMeshId(ShrineGenerationConfig config, bool isEnd) {
        int res = 0;
        for (int i = 0; i < Materials.Length - 1; i++) {
            if (Random.value < (isEnd ? config.AccentChanceEnd : config.AccentChanceMid))
                res++;
            else
                return res;
        }
        return res;
    }
}
