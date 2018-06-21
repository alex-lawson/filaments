using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyController : MonoBehaviour {

    public Material SkyboxMaterial;

	public void SetColor(Color color) {
        SkyboxMaterial.SetColor("_Tint", color);
        RenderSettings.ambientLight = color;
    }
}
