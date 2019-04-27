using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ghost : MonoBehaviour, IAbility
{
    public void Activate()
    {
        Debug.Log("Ghost activated.");
    }
}
