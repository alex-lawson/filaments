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
    public float FadeTimeSlow;
    public float FadePauseTime;
    public float FadeTimeFast;
    public float GenerationRate;
    public int BenchmarkIterations;
    public int CurrentSeed { get; private set; }

    private Coroutine generatingCoroutine;
    private bool reviving = false;
    private Bounds currentPortalBounds = new Bounds();

    private void Start() {
        GenerateAll();

        PlacePlayerInDungeon();

        StartCoroutine(DoFadeIn(Color.white));
    }

    private void Reset() {
        ClearAll();
        ScreenFadeOverlay.enabled = false;
        reviving = false;
    }

    private void Update() {
        if (!reviving && !Dungeon.CurrentDungeonBounds.Contains(Player.transform.position)) {
            StartCoroutine(DoPlayerRevive());
        }

        if (!reviving && currentPortalBounds.Contains(Player.transform.position)) {
            Color fadeColor = Color.HSVToRGB(Colors.BaseHues[0], 0.8f, 1.0f);
            StartCoroutine(DoRegenerate(fadeColor));
        }

        if (Input.GetKeyDown(KeyCode.Return)) {
            if (generatingCoroutine != null)
                StopCoroutine(generatingCoroutine);

            generatingCoroutine = StartCoroutine(DoRegenerate(Color.white));
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    private void ClearAll() {
        Dungeon.Clear();
        Shrines.Clear();
        Orbs.Clear();
    }

    private void GenerateAll() {
        RandomizeSeed();

        Colors.GenerateColors(CurrentSeed);
        Dungeon.Seed = CurrentSeed;
        Dungeon.Generate(true);
        Shrines.Generate(CurrentSeed);
        Orbs.Generate(CurrentSeed);

        var pp = FindObjectOfType<PortalPool>() as PortalPool;
        if (pp != null)
            pp.GenerateMesh(CurrentSeed);

        // TODO: fix this super gross hack when I'm less tired
        var portal = GameObject.FindGameObjectWithTag("Portal");
        if (portal != null) {
            var portalMesh = portal.GetComponent<MeshRenderer>();
            currentPortalBounds = portalMesh.bounds;
        }
    }

    private IEnumerator DoFadeIn(Color fadeColor) {
        var wfeof = new WaitForEndOfFrame();

        ScreenFadeOverlay.enabled = true;
        ScreenFadeOverlay.color = fadeColor;

        reviving = true;

        float inTimer = 0;
        while (inTimer < FadeTimeFast) {
            float alphaRatio = 1 - inTimer / FadeTimeFast;
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
        while (outTimer < FadeTimeSlow) {
            float alphaRatio = outTimer / FadeTimeSlow;
            ScreenFadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alphaRatio);

            outTimer += Time.deltaTime;

            yield return wfeof;
        }

        ScreenFadeOverlay.color = fadeColor;

        PlacePlayerInDungeon();

        yield return new WaitForSeconds(FadePauseTime);

        float inTimer = 0;
        while (inTimer < FadeTimeFast) {
            float alphaRatio = 1 - inTimer / FadeTimeFast;
            ScreenFadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alphaRatio);

            inTimer += Time.deltaTime;

            yield return wfeof;
        }

        ScreenFadeOverlay.enabled = false;
        reviving = false;
    }

    private IEnumerator DoRegenerate(Color? fadeColor) {
        var wfeof = new WaitForEndOfFrame();

        reviving = true;

        if (fadeColor.HasValue) {
            ScreenFadeOverlay.enabled = true;

            float outTimer = 0;
            while (outTimer < FadeTimeFast) {
                float alphaRatio = outTimer / FadeTimeFast;
                ScreenFadeOverlay.color = new Color(fadeColor.Value.r, fadeColor.Value.g, fadeColor.Value.b, alphaRatio);

                outTimer += Time.deltaTime;

                yield return wfeof;
            }

            ScreenFadeOverlay.color = fadeColor.Value;
        }

        ClearAll();

        yield return wfeof;

        GenerateAll();

        yield return wfeof;

        PlacePlayerInDungeon();

        if (fadeColor.HasValue) {
            yield return new WaitForSeconds(FadePauseTime);

            float inTimer = 0;
            while (inTimer < FadeTimeFast) {
                float alphaRatio = 1 - inTimer / FadeTimeFast;
                ScreenFadeOverlay.color = new Color(fadeColor.Value.r, fadeColor.Value.g, fadeColor.Value.b, alphaRatio);

                inTimer += Time.deltaTime;

                yield return wfeof;
            }

            ScreenFadeOverlay.enabled = false;
        }

        reviving = false;
    }

    private void PlacePlayerInDungeon() {
        Player.GetComponent<PlayerStickyMovement>()?.Reset(Dungeon.transform);
        Player.GetComponentInChildren<PlayerStickyLook>()?.Reset();
    }

    private void RandomizeSeed() {
        CurrentSeed = Random.Range(int.MinValue, int.MaxValue);
    }
}
