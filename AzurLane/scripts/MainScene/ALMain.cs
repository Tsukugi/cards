using Godot;
using System;

public partial class ALMain : Control
{
    [Export]
    Button startBtn, optionsBtn, exitBtn;

    public static string ALSceneRootPath = "res://AzurLane/scenes";

    public override void _Ready()
    {
        base._Ready();
        startBtn.Pressed += onStartPressed;
        optionsBtn.Pressed += onOptionsPressed;
        exitBtn.Pressed += onExitPressed;
        startBtn.GrabFocus();
    }

    void onStartPressed()
    {
        this.ChangeScene($"{ALSceneRootPath}/match.tscn");
    }
    void onOptionsPressed()
    {
        this.ChangeScene($"{ALSceneRootPath}/options.tscn");
    }
    void onExitPressed() { GetTree().Quit(); }

    // Utils 

}
