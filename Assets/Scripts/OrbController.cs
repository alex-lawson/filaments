using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbController : MonoBehaviour {

    public GameObject OrbPrefab;
    public Material OrbMaterial;
    public DungeonGenerator Dungeon;
    public float MoonHueOffset;
    public float OrbDensity;
    public float OrbMinDistance;
    public float OrbMaxDistance;
    public float OrbCollisionClearance;
    public int MaxPlaceFails;

    private List<GameObject> orbInstances = new List<GameObject>();

    public void Generate(int seed, Color moonColor) {
        Clear();
        RandomizeOrbColor(seed, moonColor);
        PlaceOrbs(seed);
    }

	public void Clear() {
        foreach (var orb in orbInstances)
            Destroy(orb);
        orbInstances.Clear();
    }

    public void PlaceOrbs(int seed) {
        var oldRandomState = Random.state;
        Random.InitState(seed);

        int failsToAbort = MaxPlaceFails;
        int placed = 0;

        var dungeonParts = Dungeon.CurrentDungeonPartInstances;
        dungeonParts.Shuffle();
        int partCount = dungeonParts.Count;
        int orbCount = Mathf.CeilToInt(partCount * OrbDensity);
        int i = 0;
        while (placed < orbCount && failsToAbort > 0) {
            Vector3 partPos = dungeonParts[i].transform.position;
            Vector3 direction = Random.insideUnitSphere.normalized;
            float distance = Random.Range(OrbMinDistance, OrbMaxDistance);
            Vector3 orbPos = partPos + direction * distance;
            if (!Physics.CheckSphere(orbPos, OrbCollisionClearance)) {
                var newOrb = Instantiate(OrbPrefab, transform);
                newOrb.transform.position = orbPos;
                orbInstances.Add(newOrb);
                placed++;
            } else {
                failsToAbort--;
            }

            i = (i + 1) % partCount;
        }

        //Debug.Log($"placed {placed} orbs with {MaxPlaceFails - failsToAbort} failures");

        Random.state = oldRandomState;
    }

    public void RandomizeOrbColor(int seed, Color moonColor) {
        var oldRandomState = Random.state;
        Random.InitState(seed);

        float moonH, moonS, moonV;
        Color.RGBToHSV(moonColor, out moonH, out moonS, out moonV);
        float h = moonH + (Random.value - 0.5f) * MoonHueOffset;
        float s = Random.Range(0.6f, 0.8f);
        Color orbColor = Color.HSVToRGB(h, s, 1.0f);
        OrbPrefab.GetComponent<Light>().color = orbColor;
        OrbMaterial.SetColor("_ReflectionColor", orbColor);

        Random.state = oldRandomState;
    }
}
