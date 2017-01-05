using System;
using System.Collections.Generic;
using System.Linq;
using WirePlacement;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Wire Placement
/// Created by Timwi
/// </summary>
public class WirePlacementModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    void Start()
    {
        Debug.Log("[Wire Placement] Started");
    }

    void ActivateModule()
    {
        Debug.Log("[Wire Placement] Activated");
    }
}
