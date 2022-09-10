using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Tents;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Tents
/// Created by Quinn Wuest & Timwi
/// </summary>
public class TentsScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public Mesh Highlight;

    public KMSelectable[] SquareSels;
    public MeshRenderer[] SquareRenderers;
    public TextMesh[] ClueTexts;
    public KMSelectable ResetButton;
    public Material EmptyMat;
    public Material GrassMat;
    public Material TentMat;
    public Material TreeMat;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private TentsPuzzle _puzzle;

    private enum SquareType
    {
        Empty,
        Grass,
        Tent,
        Tree
    }

    private SquareType[] _inputtedInfo = new SquareType[36];

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < SquareSels.Length; i++)
            SquareSels[i].OnInteract = PressSquareHandler(i);
        ResetButton.OnInteract = ResetButtonPressed;

        _puzzle = TentsPuzzle.GenerateTentsPuzzle();
        Reset();
        for (int i = 0; i < 12; i++)
            ClueTexts[i].text = _puzzle.Clues[i].ToString();
        UpdateVisuals();

        // SVG logging!
        var tentSvg = @"<path transform='translate({0} {1}) scale(.01)' d='M44.002 22a2 2 0 0 0-1.063.305 2 2 0 0 0-.634 2.756l6.09 9.742L24.93 70H24c-2 0-2 5-2 5h56s0-5-2-5h-.893L53.154 34.873l6.51-9.764a2 2 0 0 0-.555-2.773A2 2 0 0 0 57.986 22a2 2 0 0 0-1.65.89l-5.508 8.262-5.133-8.213A2 2 0 0 0 44.002 22zm6.389 17.018L60 64l10.332 5.904.06.096H50V39.605l.39-.587z' />";
        var treeSvg = @"<g transform='translate({0} {1}) scale(.01)' stroke='#000' stroke-width='3'><path fill='#804803' d='M42 60h16v28H42z'/><path fill='#0c0' d='M41.893 12C27.575 12.06 16 23.682 16 38c0 14.36 11.64 26 26 26h16c14.36 0 26-11.64 26-26S72.36 12 58 12H41.893z'/></g>";

        var svg = string.Format(@"<svg xmlns='http://www.w3.org/2000/svg' viewBox='-0.1 -1.1 7.2 7.2' font-size='.9'><path stroke-width='.1' stroke='black' fill='#9f9' d='M0 0h6v6H0z' /><path stroke-width='.05' stroke='black' fill='none' d='M1 0v6M2 0v6M3 0v6M4 0v6M5 0v6 M0 1h6M0 2h6M0 3h6M0 4h6M0 5h6' />{0}{1}{2}</svg>",
            _puzzle.Tents.Select(t => string.Format(tentSvg, t.X, t.Y)).Join(""),
            _puzzle.Trees.Select(t => string.Format(treeSvg, t.X, t.Y)).Join(""),
            Enumerable.Range(0, 12)
                .Where(i => _puzzle.Clues[i] != null)
                .Select(i => string.Format("<text x='{0}' y='{1}' text-anchor='{3}'>{2}</text>", i < 6 ? i + .5 : 6.2, i < 6 ? -.2 : i - 6 + .85, _puzzle.Clues[i].Value, i < 6 ? "middle" : "start"))
                .Join(""));

        Debug.LogFormat("[Tents #{0}]=svg[Solution:]{1}", _moduleId, svg);
    }

    private void Reset()
    {
        for (var i = 0; i < 36; i++)
            _inputtedInfo[i] = SquareType.Empty;
        foreach (var tree in _puzzle.Trees)
            _inputtedInfo[tree.Index] = SquareType.Tree;
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < 36; i++)
        {
            SquareRenderers[i].sharedMaterial =
                _inputtedInfo[i] == SquareType.Tree ? TreeMat :
                _inputtedInfo[i] == SquareType.Tent ? TentMat :
                _inputtedInfo[i] == SquareType.Grass ? GrassMat :
                EmptyMat;
        }
    }

    private bool ResetButtonPressed()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ResetButton.transform);
        ResetButton.AddInteractionPunch(0.5f);
        if (_moduleSolved)
            return false;
        Reset();
        UpdateVisuals();
        return false;
    }

    private KMSelectable.OnInteractHandler PressSquareHandler(int square)
    {
        return delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ResetButton.transform);
            SquareSels[square].AddInteractionPunch(0.2f);
            if (_moduleSolved || _inputtedInfo[square] == SquareType.Tree)
                return false;
            _inputtedInfo[square] = (SquareType) (((int) _inputtedInfo[square] + 1) % 3);
            UpdateVisuals();
            if (Enumerable.Range(0, 36).All(i => _inputtedInfo[i] == SquareType.Tree ||
                (_inputtedInfo[i] != SquareType.Empty && (_inputtedInfo[i] == SquareType.Tent) == _puzzle.Tents.Any(t => t.Index == i))))
            {
                Module.HandlePass();
                _moduleSolved = true;
                Debug.LogFormat("[Tents #{0}] Module solved.", _moduleId);
            }
            return false;
        };
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} tent A1 A2 A3 [set cells to tent] | !{0} grass B3 B4 C4 [set cells to grass] | !{0} reset D1 D2 [set cells to unfilled] | !{0} rest tent/grass [set unfilled cells to tents or grass] | !{0} solve ##..###.....# [solve whole puzzle; # = tent, . = grass/tree] | !{0} reset [reset the whole puzzle]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        if ((m = Regex.Match(command, @"^\s*(?:(?<grass>grass|g)|(?<tent>tent|t)|reset|r)\s+([a-f][1-6]\s*)+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            var coords = m.Groups[1].Captures.Cast<Capture>().Select(capture => coord(capture.Value.Trim())).ToArray();
            if (coords.Any(c => c == null))
                yield break;
            if (coords.Any(c => _inputtedInfo[c.Value] == SquareType.Tree))
            {
                yield return "sendtochaterror You can’t change a square that has a tree on it.";
                yield break;
            }
            yield return null;
            var desiredValue = (int) (m.Groups["grass"].Success ? SquareType.Grass : m.Groups["tent"].Success ? SquareType.Tent : SquareType.Empty);
            yield return coords.SelectMany(c => Enumerable.Repeat(SquareSels[c.Value], (3 + desiredValue - (int) _inputtedInfo[c.Value]) % 3)).ToArray();
        }

        if (Regex.IsMatch(command, @"^\s*(reset|r)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            yield return new[] { ResetButton };
        }

        if ((m = Regex.Match(command, @"^\s*(?:rest|r)\s+(?:grass|(?<t>tent))\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            var presses = Enumerable.Range(0, _inputtedInfo.Length).Where(ix => _inputtedInfo[ix] == SquareType.Empty).Select(ix => SquareSels[ix]);
            yield return m.Groups["t"].Success ? presses.Concat(presses).ToArray().Shuffle() : presses.ToArray().Shuffle();
        }

        if ((m = Regex.Match(command, @"^\s*(?:solve|s)\s+([\.#]{36})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            var btns = new List<KMSelectable>();
            for (var i = 0; i < 36; i++)
                if (_inputtedInfo[i] != SquareType.Tree)
                    btns.AddRange(Enumerable.Repeat(SquareSels[i], (3 + (m.Groups[1].Value[i] == '#' ? 2 : 1) - (int) _inputtedInfo[i]) % 3));
            yield return btns.Shuffle();
        }
    }

    private int? coord(string unparsed)
    {
        if (unparsed.Length == 2 && "ABCDEFabcdef".Contains(unparsed[0]) && "123456".Contains(unparsed[1]))
            return "ABCDEFabcdef".IndexOf(unparsed[0]) % 6 + 6 * "123456".IndexOf(unparsed[1]);
        return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (_moduleSolved)
            yield break;

        for (var cell = 0; cell < 6 * 6; cell++)
            if (_inputtedInfo[cell] != SquareType.Tree)
            {
                var desired = _puzzle.Tents.Any(t => t.Index == cell) ? SquareType.Tent : SquareType.Grass;
                while (_inputtedInfo[cell] != desired)
                {
                    SquareSels[cell].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
            }

        while (!_moduleSolved)
            yield return true;
    }
}
