using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class FloatRange {
    public float Min;
    public float Max;

    public FloatRange(float min, float max) {
        Min = min;
        Max = max;
    }

    public float RandomValue() {
        float val = Random.value;
        return Mathf.Lerp(Min, Max, val);
    }
}

[Serializable]
public class IntRange {
    public int Min;
    public int Max;

    public IntRange(int min, int max) {
        Min = min;
        Max = max;
    }

    public int RandomValue() {
        float val = Random.value;
        return Mathf.FloorToInt(Mathf.Lerp(Min, Max + 0.999f, val));
    }
}