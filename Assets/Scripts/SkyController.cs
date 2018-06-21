using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyController : MonoBehaviour {

    public Material SkyboxMaterial;

    private Color originalColor;

    private void Awake() {
        originalColor = SkyboxMaterial.GetColor("_Tint");
    }

    private void OnDestroy() {
        SkyboxMaterial.SetColor("_Tint", originalColor);
    }

    public void SetColor(Color color) {
        SkyboxMaterial.SetColor("_Tint", color);
        RenderSettings.ambientLight = color;
    }
}
