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

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        var solutions = Ut.NewArray(
            new { Color = WireColor.Black, Locations = "B1,D4,A4,D2,B4" },
            new { Color = WireColor.Blue, Locations = "A2,C4,A1,C4,D4" },
            new { Color = WireColor.Blue, Locations = "C3,C2,C1,D3,B1" },
            new { Color = WireColor.Red, Locations = "A1,B3,C4,B2,B3" },
            new { Color = WireColor.Red, Locations = "C4,D3,B1,C1,C2" },
            new { Color = WireColor.White, Locations = "B2,C1,B4,A1,C1" },
            new { Color = WireColor.White, Locations = "D3,D2,D4,B3,B2" },
            new { Color = WireColor.Yellow, Locations = "A3,C3,A2,A4,A3" },
            new { Color = WireColor.Yellow, Locations = "D1,A1,B2,B4,A4" },
            new { Color = WireColor.Yellow, Locations = "D2,D1,D2,A2,D1" }
        );

        List<WireInfo> wireInfos;
        var isSolved = false;

        retry:
        wireInfos = new List<WireInfo>();
        var taken = Ut.NewArray<bool>(4, 4);
        var px = 0;
        var py = 0;
        var c3Color = Rnd.Range(0, 5);

        for (int i = 0; i < 8; i++)
        {
            while (taken[px][py])
            {
                px++;
                if (px == 4)
                {
                    py++;
                    px = 0;
                }
            }

            // Rare situation in which the board is impossible to fill
            if (py == 3 && (px == 3 || taken[px + 1][py]))
                goto retry;

            var vert = px == 3 || taken[px + 1][py] ? true : py == 3 || taken[px][py + 1] ? false : Rnd.Range(0, 2) == 0;
            taken[px][py] = true;
            taken[vert ? px : px + 1][vert ? py + 1 : py] = true;
            var color = (WireColor) ((px == 2 && py == 2) || ((vert ? px : px + 1) == 2 && (vert ? py + 1 : py) == 2) ? c3Color : Rnd.Range(0, 5));

            Func<string, bool> mustCut = coord =>
            {
                var x = coord[0] - 'A';
                var y = coord[1] - '1';
                return (px == x && py == y) || (px == (vert ? x : x - 1) && py == (vert ? y - 1 : y));
            };

            wireInfos.Add(new WireInfo
            {
                Index = i,
                Column = px,
                Row = py,
                Color = color,
                IsVertical = vert,
                MustCut = solutions.Where(sol => sol.Color == color).Any(sol => mustCut(sol.Locations.Split(',')[c3Color]))
            });
        }
        if (wireInfos.All(w => !w.MustCut))
            goto retry;

        Debug.LogFormat("[Wire Placement #{1}] C3 wire is {0}.", (WireColor) c3Color, _moduleId);

        foreach (var wireFE in wireInfos)
        {
            var wire = wireFE;

            Debug.LogFormat("[Wire Placement #{6}] {0} wire (#{5}) {1} from {2},{3} {4} be cut.", wire.Color, wire.IsVertical ? "vertical" : "horizontal", wire.Column + 1, wire.Row + 1, wire.MustCut ? "must" : "must not", wire.Index + 1, _moduleId);

            var seg = Rnd.Range(3, 5);
            var seed = Rnd.Range(0, int.MaxValue);

            var wireObj = MainSelectable.transform.Find(string.Format("Wire {0}", wire.Index + 1));
            wireObj.GetComponent<MeshFilter>().mesh = MeshGenerator.GenerateWire(.0304, seg, MeshGenerator.WirePiece.Uncut, false, seed);
            wireObj.GetComponent<MeshRenderer>().material = WireMaterials[(int) wire.Color];

            wireObj.localPosition = new Vector3(0.0304f * wire.Column - 0.0608f, .029f, -0.0304f * wire.Row + 0.0304f);
            wireObj.localScale = new Vector3(1, 1, 1);
            wireObj.localEulerAngles = new Vector3(0, wire.IsVertical ? 90f : 0f, 0);

            var wireHighlight = wireObj.transform.Find(string.Format("Wire {0} highlight", wire.Index + 1));
            var highlightMesh = MeshGenerator.GenerateWire(.0304, seg, MeshGenerator.WirePiece.Uncut, true, seed);
            wireHighlight.GetComponent<MeshFilter>().mesh = highlightMesh;

            var wireHighlightClone = wireHighlight.Find("Highlight(Clone)");
            if (wireHighlightClone != null)
                wireHighlightClone.GetComponent<MeshFilter>().mesh = highlightMesh;

            var cutMesh = MeshGenerator.GenerateWire(.0304, seg, MeshGenerator.WirePiece.Cut, false, seed);
            var cutHighlightMesh = MeshGenerator.GenerateWire(.0304, seg, MeshGenerator.WirePiece.Cut, true, seed);
            var copperMesh = MeshGenerator.GenerateWire(.0304, seg, MeshGenerator.WirePiece.Copper, false, seed);

            wireObj.GetComponent<KMSelectable>().OnInteract = delegate
            {
                if (isSolved || wire.IsCut)
                    return false;
                wire.IsCut = true;

                wireObj.GetComponent<KMSelectable>().AddInteractionPunch();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, wireObj);

                wireObj.GetComponent<MeshFilter>().mesh = cutMesh;
                var copperObj = new GameObject { name = string.Format("Wire {0} copper", wire.Index + 1) }.transform;
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

                Debug.LogFormat("[Wire Placement #{6}] Cutting {0} wire (#{5}) {1} from {2},{3} was {4}.", wire.Color, wire.IsVertical ? "vertical" : "horizontal", wire.Column + 1, wire.Row + 1, wire.MustCut ? "correct" : "incorrect", wire.Index + 1, _moduleId);

                if (!wire.MustCut)
                {
                    Module.HandleStrike();
                }
                else if (wireInfos.All(w => !w.MustCut || w.IsCut))
                {
                    isSolved = true;
                    Module.HandlePass();
                }

                return false;
            };

            MainSelectable.Children[wire.Column + 4 * wire.Row] = wireObj.GetComponent<KMSelectable>();
            MainSelectable.Children[wire.Column + (wire.IsVertical ? 0 : 1) + 4 * (wire.Row + (wire.IsVertical ? 1 : 0))] = wireObj.GetComponent<KMSelectable>();
        }
        MainSelectable.UpdateChildren();
    }
}
