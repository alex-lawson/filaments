using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TestController : MonoBehaviour {

    public DungeonGenerator Dungeon;

	void Start () {
        Dungeon.OnGenerationComplete.AddListener(new UnityAction(PlacePlayerInDungeon));

        Dungeon.Generate();
	}
	
	void Update () {
        if (Input.GetKeyDown(KeyCode.Return)) {
            StopAllCoroutines();

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                StartCoroutine(DoRegenerateOverTime(0.03f));
            else
                StartCoroutine(DoRegenerate());
        }

        if (Input.GetKeyDown(KeyCode.B)) {
            StopAllCoroutines();

            StartCoroutine(DoBenchmark(100));
        }
    }

    private IEnumerator DoRegenerate() {
        Dungeon.Clear();

        yield return new WaitForEndOfFrame();

        Dungeon.Generate(true);
    }

    private IEnumerator DoRegenerateOverTime(float placeInterval) {
        Dungeon.Clear();

        yield return new WaitForEndOfFrame();

        Dungeon.Generate(false);

        var waitForInterval = new WaitForSeconds(placeInterval);
        while (Dungeon.Generating) {
            yield return waitForInterval;
            Dungeon.StepGeneration();
        }
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

    private void PlacePlayerInDungeon() {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) {
            player.transform.position = Dungeon.transform.position;
            player.transform.Translate(new Vector3(0, 0.5f, 0));
        }
    }
}
