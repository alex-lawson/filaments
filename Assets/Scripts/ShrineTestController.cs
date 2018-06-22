﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShrineTestController : MonoBehaviour {

    public ShrineGenerator Shrines;

    public int CurrentSeed { get; private set; }

    private void Start() {
        Generate();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space))
            Generate();
    }

    public void Generate() {
        RandomizeSeed();
        Shrines.Generate(CurrentSeed);
    }

    private void RandomizeSeed() {
        CurrentSeed = Random.Range(int.MinValue, int.MaxValue);
    }
}