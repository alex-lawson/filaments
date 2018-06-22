using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GameController : MonoBehaviour {

    public GameObject Player;
    public DungeonGenerator Dungeon;
    public ColorSchemer Colors;
    public ShrineGenerator Shrines;
    public OrbGenerator Orbs;
    public float GenerationRate;
    public int BenchmarkIterations;
    public int CurrentSeed { get; private set; }

	private void Start () {
        Dungeon.OnGenerationComplete.AddListener(new UnityAction(OnDungeonGenComplete));

        RandomizeSeed();
        Colors.GenerateColors(CurrentSeed);
        Dungeon.Seed = CurrentSeed;
        Dungeon.Generate(true);
        Shrines.Generate(CurrentSeed);
        Orbs.Generate(CurrentSeed);
    }

    private void Clear() {
        Dungeon.Clear();
        Shrines.Clear();
        Orbs.Clear();
    }
	
	private void Update () {
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

            Clear();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    private IEnumerator DoRegenerate(bool sync) {
        var wfeof = new WaitForEndOfFrame();

        Clear();

        yield return wfeof;

        RandomizeSeed();

        Colors.GenerateColors(CurrentSeed);
        Dungeon.Seed = CurrentSeed;
        Dungeon.Generate(sync);

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

        Shrines.Generate(CurrentSeed);
        Orbs.Generate(CurrentSeed);
    }

    private IEnumerator DoBenchmark(int iterations) {
        float elapsed = 0;
        float highest = 0;
        float lowest = float.MaxValue;
        int highestSeed = -1;
        int lowestSeed = -1;

        Dungeon.UseRandomSeed = false;

        for (var i = 0; i < iterations; i++) {
            Clear();

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

    private void RandomizeSeed() {
        CurrentSeed = Random.Range(int.MinValue, int.MaxValue);
    }
}
