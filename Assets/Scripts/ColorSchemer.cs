using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MaterialColorConfig {
    public Material TargetMaterial;
    public string ColorParameterName = "_Color";
    public int BaseHueIndex;
    public FloatRange SaturationRange;
    public FloatRange ValueRange;

    private Color originalColor;

    public void ApplyColor(Color newColor) {
        TargetMaterial.SetColor(ColorParameterName, newColor);
    }

    public void StoreOriginalColor() {
        originalColor = TargetMaterial.GetColor(ColorParameterName);
    }

    public void RestoreOriginalColor() {
        TargetMaterial.SetColor(ColorParameterName, originalColor);
    }
}

[System.Serializable]
public class LightColorConfig {
    public Light TargetLight;
    public int BaseHueIndex;
    public FloatRange SaturationRange;
    public FloatRange ValueRange;

    public void ApplyColor(Color newColor) {
        TargetLight.color = newColor;
    }
}

public class ColorSchemer : MonoBehaviour {

    public int BaseHueCount = 2;
    public float[] BaseHues { get; private set; }
    public float BaseHueVariance;
    public MaterialColorConfig[] MaterialColors;
    public LightColorConfig[] LightColors;
    public FloatRange AmbientSaturationRange;
    public FloatRange AmbientValueRange;

    private void Awake() {
        for (int i = 0; i < MaterialColors.Length; i++)
            MaterialColors[i].StoreOriginalColor();
    }

    private void OnDestroy() {
        for (int i = 0; i < MaterialColors.Length; i++)
            MaterialColors[i].RestoreOriginalColor();
    }

    public void GenerateColors(int seed) {
        var oldRandomState = Random.state;
        Random.InitState(seed);

        GenerateBaseHues();

        float ambientH = BaseHues[0];
        float ambientS = AmbientSaturationRange.RandomValue();
        float ambientV = AmbientValueRange.RandomValue();
        Color ambientColor = Color.HSVToRGB(ambientH, ambientS, ambientV);
        RenderSettings.ambientLight = ambientColor;

        for (int i = 0; i < MaterialColors.Length; i++) {
            var mcc = MaterialColors[i];
            Color color = GenerateColor(mcc.BaseHueIndex, mcc.SaturationRange, mcc.ValueRange);
            mcc.ApplyColor(color);
        }

        for (int i = 0; i < LightColors.Length; i++) {
            var lcc = LightColors[i];
            Color color = GenerateColor(lcc.BaseHueIndex, lcc.SaturationRange, lcc.ValueRange);
            lcc.ApplyColor(color);
        }

        Random.state = oldRandomState;
    }

    private void GenerateBaseHues() {
        BaseHues = new float[BaseHueCount];
        float hueSeparation = 1.0f / BaseHueCount;
        float hueAngle = Random.value;
        for (int i = 0; i < BaseHueCount; i++) {
            float h = hueAngle + Random.value * BaseHueVariance;
            h = h % 1.0f;
            BaseHues[i] = h;
            hueAngle += hueSeparation;
        }
    }

    private Color GenerateColor(int baseHueIndex, FloatRange satRange, FloatRange valRange) {
        float h = BaseHues[baseHueIndex];
        float s = satRange.RandomValue();
        float v = valRange.RandomValue();
        return Color.HSVToRGB(h, s, v);
    }
}
