using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour {
    void Start() {
        var dungeonGenerator = (DungeonGenerator)ScriptableObject.CreateInstance("DungeonGenerator");
        var dungeon = dungeonGenerator.Generate();

        var player = GameObject.Find("Player");
        if (player != null) {
            var spawn = new Vector3(
                dungeon.spawnRoom.entrance.x, 5, dungeon.spawnRoom.entrance.y);
            player.transform.position = Vector3.MoveTowards(
                spawn, new Vector3(dungeon.spawnRoom.position.x, 5, dungeon.spawnRoom.position.y), 5);
        }
    }

    void Update() {

    }
}
