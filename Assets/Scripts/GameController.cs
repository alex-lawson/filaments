using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameController : MonoBehaviour {

    public GameObject Player;
    public DungeonGenerator Dungeon;
    public ColorSchemer Colors;
    public ShrineGenerator Shrines;
    public OrbGenerator Orbs;
    public Image ScreenFadeOverlay;
    public float ScreenFadeOutTime;
    public float ScreenFadePauseTime;
    public float ScreenFadeInTime;
    public float GenerationRate;
    public int BenchmarkIterations;
    public int CurrentSeed { get; private set; }

    private Coroutine generatingCoroutine;
    private bool reviving = false;

    private void Start() {
        //Dungeon.OnGenerationComplete.AddListener(new UnityAction(OnDungeonGenComplete));

        RandomizeSeed();
        Colors.GenerateColors(CurrentSeed);
        Dungeon.Seed = CurrentSeed;
        Dungeon.Generate(true);
        Shrines.Generate(CurrentSeed);
        Orbs.Generate(CurrentSeed);

        StartCoroutine(DoFadeIn());
    }

    private void Reset() {
        Clear();
        ScreenFadeOverlay.enabled = false;
        reviving = false;
    }

    private void Clear() {
        Dungeon.Clear();
        Shrines.Clear();
        Orbs.Clear();
    }

    private void Update() {
        if (!reviving && !Dungeon.CurrentDungeonBounds.Contains(Player.transform.position)) {
            StartCoroutine(DoPlayerRevive());
        }

        if (Input.GetKeyDown(KeyCode.Return)) {
            if (generatingCoroutine != null)
                StopCoroutine(generatingCoroutine);

            bool sync = !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            generatingCoroutine = StartCoroutine(DoRegenerate(sync, true));
        }

        if (Input.GetKeyDown(KeyCode.B)) {
            if (generatingCoroutine != null)
                StopCoroutine(generatingCoroutine);

            generatingCoroutine = StartCoroutine(DoBenchmark(BenchmarkIterations));
        }

        if (Input.GetKeyDown(KeyCode.C)) {
            if (generatingCoroutine != null)
                StopCoroutine(generatingCoroutine);

            Clear();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    private IEnumerator DoFadeIn() {
        var wfeof = new WaitForEndOfFrame();

        Color fadeColor = RenderSettings.skybox.GetColor("_Tint");

        ScreenFadeOverlay.enabled = true;
        ScreenFadeOverlay.color = fadeColor;

        reviving = true;

        float inTimer = 0;
        while (inTimer < ScreenFadeInTime) {
            float alphaRatio = 1 - inTimer / ScreenFadeInTime;
            ScreenFadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alphaRatio);

            inTimer += Time.deltaTime;

            yield return wfeof;
        }

        ScreenFadeOverlay.enabled = false;
        reviving = false;
    }

    private IEnumerator DoPlayerRevive() {
        var wfeof = new WaitForEndOfFrame();

        Color fadeColor = RenderSettings.skybox.GetColor("_Tint");

        ScreenFadeOverlay.enabled = true;
        reviving = true;

        float outTimer = 0;
        while (outTimer < ScreenFadeOutTime) {
            float alphaRatio = outTimer / ScreenFadeOutTime;
            ScreenFadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alphaRatio);

            outTimer += Time.deltaTime;

            yield return wfeof;
        }

        ScreenFadeOverlay.color = fadeColor;

        PlacePlayerInDungeon();

        yield return new WaitForSeconds(ScreenFadePauseTime);

        float inTimer = 0;
        while (inTimer < ScreenFadeInTime) {
            float alphaRatio = 1 - inTimer / ScreenFadeInTime;
            ScreenFadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alphaRatio);

            inTimer += Time.deltaTime;

            yield return wfeof;
        }

        ScreenFadeOverlay.enabled = false;
        reviving = false;
    }

    private IEnumerator DoRegenerate(bool sync, bool withFade) {
        var wfeof = new WaitForEndOfFrame();

        reviving = true;

        if (withFade) {
            ScreenFadeOverlay.enabled = true;

            float outTimer = 0;
            while (outTimer < ScreenFadeOutTime) {
                float alphaRatio = outTimer / ScreenFadeOutTime;
                ScreenFadeOverlay.color = new Color(0, 0, 0, alphaRatio);

                outTimer += Time.deltaTime;

                yield return wfeof;
            }

            ScreenFadeOverlay.color = Color.black;
        }

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

        yield return wfeof;

        PlacePlayerInDungeon();

        if (withFade) {
            float inTimer = 0;
            while (inTimer < ScreenFadeInTime) {
                float alphaRatio = 1 - inTimer / ScreenFadeInTime;
                ScreenFadeOverlay.color = new Color(0, 0, 0, alphaRatio);

                inTimer += Time.deltaTime;

                yield return wfeof;
            }

            ScreenFadeOverlay.enabled = false;
        }

        reviving = false;
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
        Debug.Log($"completed {iterations} iterations in {elapsed:F5}s ({avg * 1000:F1}ms avg, {highest * 1000:F1}ms max, {lowest * 1000:F1}ms min)");
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
