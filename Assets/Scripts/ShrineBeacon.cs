using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShrineBeacon : MonoBehaviour {

    public static Vector3 ActivatorPosition = Vector3.zero;

    public LensFlare ExtraFlare;
    public float BeaconDistance;
    public int ColorBaseHueIndex;
    public float ColorSaturation;
    public float ColorValue;
    public bool BeaconActive = false;
    public float ActivationDistance;
    public float InactiveLightIntensity;
    public float ActiveLightIntensity;
    public float InactiveFlareBrightness;
    public float ActiveFlareBrightness;
    public float ExtraInactiveFlareBrightness;
    public float ActivateTime;
    public AnimationCurve ActivateCurve;
    public float LightFadeTime;

    private Light beaconLight;
    private LensFlare beaconFlare;
    private Color color;

    private void Awake() {
        beaconLight = GetComponent<Light>();
        beaconFlare = GetComponent<LensFlare>();
    }

    private void Start () {
        
	}

	private void Update () {
		if (!BeaconActive) {
            if ((ActivatorPosition - transform.position).sqrMagnitude < ActivationDistance * ActivationDistance)
                ActivateBeacon();
        }
        //else if (BeaconActive) {
        //    if ((ActivatorPosition - transform.position).sqrMagnitude > ActivationDistance * ActivationDistance)
        //        Deactivate();
        //}
	}

    public void SetupBeacon(float shrineHeight) {
        beaconLight.transform.localPosition = new Vector3(0, shrineHeight + BeaconDistance, 0);

        ColorSchemer colors = GameObject.FindGameObjectWithTag("ColorSchemer").GetComponent<ColorSchemer>();
        color = Color.HSVToRGB(colors.BaseHues[ColorBaseHueIndex], ColorSaturation, ColorValue);
        beaconLight.color = color;
        beaconFlare.color = color;
        ExtraFlare.color = color;

        DeactivateBeacon();
    }

    public void ActivateBeacon() {
        BeaconActive = true;
        StartCoroutine(DoActivate());
        var gameController = FindObjectOfType<GameController>();
        gameController.BeaconActivated();
    }

    public void DeactivateBeacon() {
        BeaconActive = false;
        beaconLight.intensity = InactiveLightIntensity;
        beaconFlare.brightness = InactiveFlareBrightness;
        ExtraFlare.brightness = ExtraInactiveFlareBrightness;
    }

    public void FadeLight() {
        StartCoroutine(DoFadeLight());
    }

    private IEnumerator DoActivate() {
        var wfeof = new WaitForEndOfFrame();

        float timer = 0;
        while (timer < ActivateTime) {
            float ratio = timer / ActivateTime;
            float curved = ActivateCurve.Evaluate(ratio);

            beaconLight.intensity = Mathf.LerpUnclamped(InactiveLightIntensity, ActiveLightIntensity, curved);
            beaconFlare.brightness = Mathf.LerpUnclamped(InactiveFlareBrightness, ActiveFlareBrightness, curved);
            ExtraFlare.brightness = Mathf.LerpUnclamped(ExtraInactiveFlareBrightness, ActiveFlareBrightness, curved);

            timer += Time.deltaTime;

            yield return wfeof;
        }

        beaconLight.intensity = ActiveLightIntensity;
        beaconFlare.brightness = ActiveFlareBrightness;
        ExtraFlare.brightness = ActiveFlareBrightness;
    }

    private IEnumerator DoFadeLight() {
        var wfeof = new WaitForEndOfFrame();

        float timer = 0;
        while (timer < LightFadeTime) {
            float ratio = timer / LightFadeTime;

            beaconLight.intensity = Mathf.Lerp(ActiveLightIntensity, 0, ratio);

            timer += Time.deltaTime;

            yield return wfeof;
        }

        beaconLight.intensity = 0;
    }
}
