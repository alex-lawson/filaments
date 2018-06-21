using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarController : MonoBehaviour {

    public Light Sun;
    public Light Moon;
    public Transform Player;
    public SkyController Sky;
    public float SMin;
    public float SMax;
    public float VMin;
    public float VMax;
    public float HVariance;
    public float MoonVFactor;
    public float SkyVFactor;

    private void Update () {
        AlignStars();
    }

    public void RandomizeStarColors(int seed) {
        var oldRandomState = Random.state;
        Random.InitState(seed);

        float sunS = Random.Range(SMin, SMax);
        float sunV = Random.Range(VMin, VMax);
        float sunH = Random.Range(0, 1.0f);

        float moonS = Random.Range(SMin, SMax);
        float moonV = sunV * MoonVFactor;
        float moonH = (sunH + 0.5f) % 1;
        moonH += (Random.value - 0.5f) * HVariance;

        float skyV = moonV * SkyVFactor;

        Color sunColor = Color.HSVToRGB(sunH, sunS, sunV);
        Color moonColor = Color.HSVToRGB(moonH, moonS, moonV);
        Color skyColor = Color.HSVToRGB(moonH, moonS, skyV);

        Sun.color = sunColor;
        Moon.color = moonColor;
        Sky.SetColor(skyColor);

        //Debug.Log($"sunH {sunH} moonH {moonH} sunColor {sunColor} moonColor {moonColor} skyColor {skyColor}");

        Random.state = oldRandomState;
    }

    private void AlignStars() {
        Sun.transform.rotation = Quaternion.AngleAxis(90, Player.right) * Player.rotation;
        Moon.transform.rotation = Quaternion.AngleAxis(-90, Player.right) * Player.rotation;
    }
}
