using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour {

    private void OnTriggerStay(Collider other) {
        var player = other.gameObject.GetComponent<PlayerStickyMovement>();
        if (player != null) {
            var gameController = FindObjectOfType<GameController>();
            gameController.PlayerEnteredPortal();
        }
    }
}
