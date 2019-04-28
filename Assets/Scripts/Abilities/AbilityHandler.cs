using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityHandler : MonoBehaviour {
    public GameObject ability1;
    public GameObject ability2;

    void Start() {

    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Q)) {
            ability1.GetComponent<IAbility>().Activate();
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            ability2.GetComponent<IAbility>().Activate();
        }
    }
}
