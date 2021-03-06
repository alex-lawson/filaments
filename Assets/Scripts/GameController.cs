﻿using System.Collections;
using System.Collections.Generic;
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
    public float LevelCompleteDelay;
    public float LevelCompleteTime;
    public AnimationCurve BeaconFlightCurve;
    public float BeaconRingHeight;
    public float BeaconRingRadius;
    public int CurrentSeed { get; private set; }

    private Coroutine generatingCoroutine;
    private PortalPool portalPool;
    private bool reviving = false;
    bool levelComplete = false;

    private float beaconsRemaining = 0;

    private void Start() {
        StartCoroutine(DoRegenerate(Color.white, false));
    }

    private void Reset() {
        ClearAll();
        ScreenFadeOverlay.enabled = false;
        reviving = false;
        levelComplete = false;
    }

    private void Update() {
        if (!reviving && !Dungeon.CurrentDungeonBounds.Contains(Player.transform.position)) {
            StartCoroutine(DoPlayerRevive());
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Return)) {
            if (generatingCoroutine != null)
                StopCoroutine(generatingCoroutine);

            generatingCoroutine = StartCoroutine(DoRegenerate(null));
        }

        if (Input.GetKeyDown(KeyCode.T)) {
            var beacons = FindObjectsOfType<ShrineBeacon>();
            foreach (var b in beacons)
                b.ActivateBeacon();
        }
#endif
        if (Input.GetButtonDown("Level Skip")) {
            if (generatingCoroutine != null)
                StopCoroutine(generatingCoroutine);

            generatingCoroutine = StartCoroutine(DoRegenerate(Color.white));
        }

        if (Input.GetButtonDown("Cancel"))
            Application.Quit();
    }

    public void PlayerEnteredPortal() {
        if (!reviving) {
            Color fadeColor = Color.HSVToRGB(Colors.BaseHues[0], 0.2f, 1.0f);
            StartCoroutine(DoRegenerate(fadeColor));
        }
    }

    public void BeaconActivated() {
        beaconsRemaining--;
        if (beaconsRemaining <= 0 && !levelComplete) {
            StartCoroutine(DoCompleteLevel());
        }
    }

    private IEnumerator DoCompleteLevel() {
        var wfeof = new WaitForEndOfFrame();

        levelComplete = true;

        yield return new WaitForSeconds(LevelCompleteDelay);

        var beacons = FindObjectsOfType<ShrineBeacon>();
        List<Vector3> beaconBasePositions = new List<Vector3>();
        foreach (var b in beacons) {
            b.FadeLight();
            beaconBasePositions.Add(b.transform.position);
        }

        List<Vector3> beaconRingPositions = new List<Vector3>();
        float angle = 2 * Mathf.PI / beacons.Length;
        for (var i = 0; i < beacons.Length; i++) {
            float thisAngle = angle * i;
            beaconRingPositions.Add(new Vector3(Mathf.Cos(thisAngle) * BeaconRingRadius, BeaconRingHeight, Mathf.Sin(thisAngle) * BeaconRingRadius));
        }

        Transform beaconRig = portalPool.gameObject.GetComponentInChildren<Spin>().transform;

        float timer = 0;
        while (timer < LevelCompleteTime) {
            float ratio = timer / LevelCompleteTime;
            float curved = BeaconFlightCurve.Evaluate(ratio);

            for (var i = 0; i < beacons.Length; i++) {
                Vector3 endPoint = beaconRig.TransformPoint(beaconRingPositions[i]);
                beacons[i].transform.position = Vector3.Lerp(beaconBasePositions[i], endPoint, curved);
            }

            timer += Time.deltaTime;

            yield return wfeof;
        }

        for (var i = 0; i < beacons.Length; i++) {
            beacons[i].transform.SetParent(beaconRig);
            beacons[i].transform.localPosition = beaconRingPositions[i];
        }

        portalPool.Uncover();
    }

    private void ClearAll() {
        Dungeon.Clear();
        Shrines.Clear();
        Orbs.Clear();
    }

    private bool GenerateAll() {
        RandomizeSeed();

        Player.GetComponent<CapsuleCollider>().enabled = false;

        Colors.GenerateColors(CurrentSeed);
        Dungeon.Seed = CurrentSeed;
        Dungeon.Generate(true);

        levelComplete = false;

        beaconsRemaining = Shrines.Generate(CurrentSeed);

        if (beaconsRemaining == 0) {
            //Debug.Log("generation failed, no beacons placed!");
            return false;
        } else {
            //Debug.Log($"placed {beaconsRemaining} beacons");
        }

        portalPool = FindObjectOfType<PortalPool>();
        if (portalPool == null)
            return false;

        portalPool.Generate(CurrentSeed);

        Orbs.Generate(CurrentSeed);

        Player.GetComponent<CapsuleCollider>().enabled = true;

        return true;
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

    private IEnumerator DoRegenerate(Color? fadeColor, bool doFadeOut = true) {
        var wfeof = new WaitForEndOfFrame();

        reviving = true;

        if (fadeColor.HasValue) {
            ScreenFadeOverlay.enabled = true;

            if (doFadeOut) {
                float outTimer = 0;
                while (outTimer < FadeTimeFast) {
                    float alphaRatio = outTimer / FadeTimeFast;
                    ScreenFadeOverlay.color = new Color(fadeColor.Value.r, fadeColor.Value.g, fadeColor.Value.b, alphaRatio);

                    outTimer += Time.deltaTime;

                    yield return wfeof;
                }
            }

            ScreenFadeOverlay.color = fadeColor.Value;
        }

        bool succeeded = false;
        while (!succeeded) {
            ClearAll();

            yield return wfeof;

            succeeded = GenerateAll();

            yield return wfeof;
        }

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
