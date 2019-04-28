using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour {
    void Start() {
        var dungeonGenerator = (DungeonGenerator) ScriptableObject.CreateInstance("DungeonGenerator");
        dungeonGenerator.Generate();
    }

    void Update() {

    }
}
