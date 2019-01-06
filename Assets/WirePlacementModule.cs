using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WirePlacement;

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
    public KMRuleSeedable RuleSeedable;

    public KMSelectable MainSelectable;
    public Material[] WireMaterials;
    public Material CopperMaterial;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private List<WireInfo> _wireInfos;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[Wire Placement #{0}] Using rule seed: {1}", _moduleId, rnd.Seed);

        var coords = new[] { "A1", "A2", "A3", "A4", "B1", "C3", "B3", "B4", "C1", "C2", "B2", "C4", "D1", "D2", "D3", "D4" };
        var specialWireCoordinate = coords[rnd.Next(0, coords.Length)];
        var coordX = specialWireCoordinate[0] - 'A';
        var coordY = specialWireCoordinate[1] - '1';

        var colors = new List<WireColor> { WireColor.Black, WireColor.Blue, WireColor.Red, WireColor.White, WireColor.Yellow };

        // All of this convoluted code is here to recreate the rules the module had before rule-seed support
        for (var i = 0; i < 6; i++)
            rnd.Next(0, 10);

        while (colors.Count < 10)
            colors.Add(new[] { WireColor.Black, WireColor.Blue, WireColor.Red, WireColor.White, WireColor.Yellow }[rnd.Next(0, 5)]);

        rnd.ShuffleFisherYates(colors);
        colors = new List<WireColor> { colors[4], colors[0], colors[5], colors[7], colors[3], colors[2], colors[1], colors[8], colors[6], colors[9] };

        var columns = new List<string[]>();
        coords = new[] { "C3", "A1", "C4", "A4", "B3", "B2", "D3", "B4", "A3", "D1", "A2", "C1", "B1", "C2", "D4", "D2" };
        rnd.ShuffleFisherYates(coords);
        columns.Add(coords.Take(10).ToArray());

        coords = new[] { "A2", "A3", "D3", "D1", "C3", "C4", "A4", "C1", "B3", "C2", "B1", "B2", "B4", "D2", "A1", "D4" };
        rnd.ShuffleFisherYates(coords);
        columns.Add(coords.Take(10).ToArray());

        coords = new[] { "A3", "B3", "C2", "B4", "B1", "D2", "C1", "A2", "A4", "C3", "D1", "A1", "D3", "C4", "D4", "B2" };
        rnd.ShuffleFisherYates(coords);
        columns.Add(coords.Take(10).ToArray());

        coords = new[] { "C1", "A2", "A3", "B2", "D3", "B1", "C2", "C3", "C4", "D1", "B3", "D2", "D4", "A4", "B4", "A1" };
        rnd.ShuffleFisherYates(coords);
        columns.Add(coords.Take(10).ToArray());

        coords = new[] { "A1", "A2", "D4", "A4", "C2", "C3", "B4", "C1", "C4", "B1", "D2", "B2", "B3", "A3", "D1", "D3" };
        rnd.ShuffleFisherYates(coords);
        columns.Add(coords.Take(10).ToArray());

        var isSolved = false;

        retry:
        _wireInfos = new List<WireInfo>();
        var taken = Ut.NewArray<bool>(4, 4);
        var px = 0;
        var py = 0;
        var specialWireColor = Rnd.Range(0, 5);

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
            var color = (WireColor) ((px == coordX && py == coordY) || ((vert ? px : px + 1) == coordX && (vert ? py + 1 : py) == coordY) ? specialWireColor : Rnd.Range(0, 5));

            Func<string, bool> mustCut = coord =>
            {
                var x = coord[0] - 'A';
                var y = coord[1] - '1';
                return (px == x && py == y) || (px == (vert ? x : x - 1) && py == (vert ? y - 1 : y));
            };

            _wireInfos.Add(new WireInfo
            {
                Index = i,
                Column = px,
                Row = py,
                Color = color,
                IsVertical = vert,
                MustCut = Enumerable.Range(0, 10).Any(row => colors[row] == color && mustCut(columns[specialWireColor][row]))
            });
        }
        if (_wireInfos.All(w => !w.MustCut))
            goto retry;

        Debug.LogFormat("[Wire Placement #{1}] {2} wire is {0}.", (WireColor) specialWireColor, _moduleId, specialWireCoordinate);

        foreach (var wireFE in _wireInfos)
        {
            var wire = wireFE;

            Debug.LogFormat("[Wire Placement #{6}] {0} wire (#{5}) {1} from {2}{3} {4} be cut.", wire.Color, wire.IsVertical ? "vertical" : "horizontal", (char) ('A' + wire.Column), wire.Row + 1, wire.MustCut ? "must" : "must not", wire.Index + 1, _moduleId);

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

            var selectable = wireObj.GetComponent<KMSelectable>();
            selectable.OnInteract = delegate
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

                Debug.LogFormat("[Wire Placement #{6}] Cutting {0} wire (#{5}) {1} from {2}{3} was {4}.", wire.Color, wire.IsVertical ? "vertical" : "horizontal", (char) (wire.Column + 'A'), wire.Row + 1, wire.MustCut ? "correct" : "incorrect", wire.Index + 1, _moduleId);

                if (!wire.MustCut)
                {
                    Module.HandleStrike();
                }
                else if (_wireInfos.All(w => !w.MustCut || w.IsCut))
                {
                    isSolved = true;
                    Module.HandlePass();
                }

                return false;
            };

            MainSelectable.Children[wire.Column + 4 * wire.Row] = selectable;
            MainSelectable.Children[wire.Column + (wire.IsVertical ? 0 : 1) + 4 * (wire.Row + (wire.IsVertical ? 1 : 0))] = selectable;
        }
        MainSelectable.UpdateChildren();
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Cut wires with “!{0} cut A2 B4 D3”.";
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        var pieces = command.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length == 0 || pieces[0] != "cut")
            return null;

        var list = new List<KMSelectable>();
        foreach (var piece in pieces.Skip(1))
        {
            if (piece.Length != 2 || !"abcd".Contains(piece[0]) || !"1234".Contains(piece[1]))
                return null;
            list.Add(MainSelectable.Children[4 * (piece[1] - '1') + (piece[0] - 'a')]);
        }
        return list.ToArray();
    }
}
