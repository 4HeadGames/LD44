using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour {
    void Start() {
        DungeonGenerator.Generate();
    }

    void Update() {

    }
}
