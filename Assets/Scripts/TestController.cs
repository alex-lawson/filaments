using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TestController : MonoBehaviour {

    public DungeonGenerator Dungeon;

	void Start () {
        Dungeon.OnGenerationComplete.AddListener(new UnityAction(PlacePlayerInDungeon));

        Dungeon.Generate();
	}
	
	void Update () {
        if (Input.GetKeyDown(KeyCode.Return)) {
            StopAllCoroutines();

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                StartCoroutine(DoRegenerateOverTime(0.03f));
            else
                StartCoroutine(DoRegenerate());
        }
    }

    private IEnumerator DoRegenerate() {
        Dungeon.Clear();

        yield return new WaitForEndOfFrame();

        Dungeon.Generate(true);
    }

    private IEnumerator DoRegenerateOverTime(float placeInterval) {
        Dungeon.Clear();

        yield return new WaitForEndOfFrame();

        Dungeon.Generate(false);

        var waitForInterval = new WaitForSeconds(placeInterval);
        while (Dungeon.Generating) {
            yield return waitForInterval;
            Dungeon.StepGeneration();
        }
    }

    private void PlacePlayerInDungeon() {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) {
            player.transform.position = Dungeon.transform.position;
            player.transform.Translate(new Vector3(0, 0.5f, 0));
        }
    }
}
