using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blink : MonoBehaviour, IAbility {
    public void Activate() {
        var player = GameObject.Find("Player");
        player.transform.Translate(Vector3.forward * 10f);
    }
}
