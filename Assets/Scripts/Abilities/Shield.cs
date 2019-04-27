using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour, IAbility
{
    public void Activate()
    {
        Debug.Log("Shield activated.");
    }
}
