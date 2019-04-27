using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blink : MonoBehaviour, IAbility
{
    public void Activate()
    {
        Debug.Log("Blink activated.");
    }
}
