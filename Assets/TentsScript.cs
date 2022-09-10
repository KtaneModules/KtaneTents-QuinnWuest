using System.Collections;
using System.Collections.Generic;
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
        foreach (var tree in _puzzle.Trees)
            _inputtedInfo[tree.Index] = SquareType.Tree;
        for (int i = 0; i < 12; i++)
            ClueTexts[i].text = _puzzle.Clues[i].ToString();
        UpdateVisuals();
        Debug.LogFormat("[Tents #{0}] Clues: {1}", _moduleId, _puzzle.Clues.Join(", "));
        Debug.LogFormat("[Tents #{0}] Trees: {1}", _moduleId, _puzzle.Trees.Join(", "));
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
        ResetButton.AddInteractionPunch();
        if (_moduleSolved)
            return false;
        UpdateVisuals();
        return false;
    }

    private KMSelectable.OnInteractHandler PressSquareHandler(int square)
    {
        return delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ResetButton.transform);
            ResetButton.AddInteractionPunch();
            if (_moduleSolved || _inputtedInfo[square] == SquareType.Tree)
                return false;
            _inputtedInfo[square] = (SquareType)(((int)_inputtedInfo[square] + 1) % 3);
            UpdateVisuals();
            if (Enumerable.Range(0, 36).All(i => _inputtedInfo[i] == SquareType.Tree || (_inputtedInfo[i] == SquareType.Tent) == _puzzle.Tents[i])
            return false;
        };
    }



#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} water A1 A2 A3 [set cells to water] | !{0} air B3 B4 C4 [set cells to air] | !{0} reset D1 D2 [set cells to unfilled] | !{0} rest air/water [set unfilled cells to air or water] | !{0} solve ##..###.....# [solve whole puzzle; # = water, . = air]| !{0} reset [reset the whole puzzle]";
#pragma warning restore 414


}
