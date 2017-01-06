using System;
using System.Collections.Generic;
using System.Linq;
using WirePlacement;
using UnityEngine;

using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Wire Placement
/// Created by lumbud84, implemented by Timwi
/// </summary>
public class WirePlacementModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    public KMSelectable MainSelectable;
    public Material[] WireMaterials;
    public Material CopperMaterial;

    void Start()
    {
        Debug.Log("[Wire Placement] Started");

        for (int i = 0; i < 8; i++)
        {
            var j = i;
            var seg = Rnd.Range(3, 5);
            var seed = Rnd.Range(0, int.MaxValue);
            var wireColor = Rnd.Range(0, WireMaterials.Length);

            var wireObj = MainSelectable.transform.Find(string.Format("Wire {0}", i + 1));
            wireObj.GetComponent<MeshFilter>().mesh = MeshGenerator.GenerateWire(.0304, seg, MeshGenerator.WirePiece.Uncut, false, seed);
            wireObj.GetComponent<MeshRenderer>().material = WireMaterials[wireColor];

            wireObj.localPosition = new Vector3(-0.0608f * (i % 2), .029f, 0.0304f * (i / 2) - 0.0608f);
            wireObj.localScale = new Vector3(1, 1, 1);
            wireObj.localEulerAngles = new Vector3(0, 0, 0);

            var wireHighlight = wireObj.transform.Find(string.Format("Wire {0} highlight", i + 1));
            var highlightMesh = MeshGenerator.GenerateWire(.0304, seg, MeshGenerator.WirePiece.Uncut, true, seed);
            wireHighlight.GetComponent<MeshFilter>().mesh = highlightMesh;
            wireHighlight.localPosition = new Vector3(0, 0, 0);
            wireHighlight.localScale = new Vector3(1, 1, 1);
            wireHighlight.localEulerAngles = new Vector3(0, 0, 0);

            var wireHighlightClone = wireHighlight.Find("Highlight(Clone)");
            if (wireHighlightClone != null)
                wireHighlightClone.GetComponent<MeshFilter>().mesh = highlightMesh;

            var cutMesh = MeshGenerator.GenerateWire(.0304, seg, MeshGenerator.WirePiece.Cut, false, seed);
            var cutHighlightMesh = MeshGenerator.GenerateWire(.0304, seg, MeshGenerator.WirePiece.Cut, true, seed);
            var copperMesh = MeshGenerator.GenerateWire(.0304, seg, MeshGenerator.WirePiece.Copper, false, seed);
            var interacted = false;

            wireObj.GetComponent<KMSelectable>().OnInteract = delegate
            {
                if (interacted)
                    return false;
                interacted = true;

                wireObj.GetComponent<MeshFilter>().mesh = cutMesh;
                var copperObj = new GameObject { name = string.Format("Wire {0} copper", j + 1) }.transform;
                copperObj.parent = wireObj;
                copperObj.localPosition = new Vector3(0, 0, 0);
                copperObj.localEulerAngles = new Vector3(0, 0, 0);
                copperObj.localScale = new Vector3(1, 1, 1);
                copperObj.gameObject.AddComponent<MeshFilter>().mesh = copperMesh;
                copperObj.gameObject.AddComponent<MeshRenderer>().material = CopperMaterial;

                wireHighlight.GetComponent<MeshFilter>().mesh = cutHighlightMesh;
                wireHighlightClone = wireHighlight.Find("Highlight(Clone)");
                if (wireHighlightClone != null)
                    wireHighlightClone.GetComponent<MeshFilter>().mesh = cutHighlightMesh;
                return false;
            };
        }
    }

    void ActivateModule()
    {
        Debug.Log("[Wire Placement] Activated");
    }
}
