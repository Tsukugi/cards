

// TODO: Think about i18ns
using System.Collections.Generic;
using Godot;

public class ALPhase
{
    public string Reset = "Reset Phase";
    public string Preparation = "Preparation Phase";
    public string Main = "Main Phase";
    public string Battle = "Battle Phase";
    public string End = "End Phase";
    public string GetPhaseByIndex(int index)
    {
        List<string> phases = [Reset, Preparation, Main, Battle, End];
        if (!phases.Count.IsInsideBounds(index)) { GD.PrintErr("[getPhaseByIndex] Index can't be linked to any known phase"); return ""; }
        return phases[index];
    }
}