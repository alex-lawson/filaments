using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TestController : MonoBehaviour {

    public GameObject Player;
    public DungeonGenerator Dungeon;
    public float GenerationRate;
    public int BenchmarkIterations;
    public StarController Stars;
    public OrbController Orbs;

	void Start () {
        Dungeon.OnGenerationComplete.AddListener(new UnityAction(OnDungeonGenComplete));

        Dungeon.Generate(true);
        Stars.RandomizeStarColors(Dungeon.Seed);
        Orbs.Generate(Dungeon.Seed, Stars.Moon.color);
        RenderSettings.ambientLight = Stars.Moon.color;
    }
	
	void Update () {
        if (!Dungeon.CurrentDungeonBounds.Contains(Player.transform.position)) {
            PlacePlayerInDungeon();
        }

        if (Input.GetKeyDown(KeyCode.Return)) {
            StopAllCoroutines();

            bool sync = !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            StartCoroutine(DoRegenerate(sync));
        }

        if (Input.GetKeyDown(KeyCode.B)) {
            StopAllCoroutines();

            StartCoroutine(DoBenchmark(BenchmarkIterations));
        }

        if (Input.GetKeyDown(KeyCode.C)) {
            StopAllCoroutines();

            Dungeon.Clear();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    private IEnumerator DoRegenerate(bool sync) {
        var wfeof = new WaitForEndOfFrame();

        Dungeon.Clear();

        yield return wfeof;

        Dungeon.Generate(sync);

        Stars.RandomizeStarColors(Dungeon.Seed);
        RenderSettings.ambientLight = Stars.Moon.color;

        if (!sync) {
            float elapsed = 0;
            int steps = 0;
            while (Dungeon.Generating) {
                yield return wfeof;

                elapsed += Time.deltaTime;
                int targetSteps = Mathf.CeilToInt(elapsed * GenerationRate);

                while (steps < targetSteps) {
                    Dungeon.StepGeneration();
                    steps++;
                }
            }
        }

        Orbs.Generate(Dungeon.Seed, Stars.Moon.color);
    }

    private IEnumerator DoBenchmark(int iterations) {
        float elapsed = 0;
        float highest = 0;
        float lowest = float.MaxValue;
        int highestSeed = -1;
        int lowestSeed = -1;

        Dungeon.UseRandomSeed = false;

        for (var i = 0; i < iterations; i++) {
            Dungeon.Clear();

            yield return new WaitForEndOfFrame();

            float startTime = Time.realtimeSinceStartup;

            Dungeon.Seed = i;
            Dungeon.Generate(true);

            float thisTime = Time.realtimeSinceStartup - startTime;
            if (thisTime > highest) {
                highest = thisTime;
                highestSeed = Dungeon.Seed;
            }
            if (thisTime < lowest) {
                lowest = thisTime;
                lowestSeed = Dungeon.Seed;
            }
            elapsed += thisTime;

            yield return new WaitForEndOfFrame();
        }

        float avg = elapsed / iterations;
        Debug.Log($"completed {iterations} iterations in {elapsed:F5}s ({avg * 1000:F1}ms avg, {highest*1000:F1}ms max, {lowest*1000:F1}ms min)");
        Debug.Log($"slowest seed {highestSeed}, fastest seed {lowestSeed}");
    }

    private void OnDungeonGenComplete() {
        PlacePlayerInDungeon();
    }

    private void PlacePlayerInDungeon() {
        Player.GetComponent<PlayerStickyMovement>()?.Reset();
        Player.GetComponentInChildren<PlayerStickyLook>()?.Reset();
        Player.transform.position = Dungeon.transform.position;
    }
}
