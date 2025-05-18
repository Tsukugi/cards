using Godot;
using System;

public partial class ALMain : Control
{
    [Export]
    Button startBtn, optionsBtn, exitBtn, createGameBtn, joinBtn;
    ALLobbyUI lobby = null;

    public static string ALSceneRootPath = "res://AzurLane/scenes";

    public override void _Ready()
    {
        base._Ready();
        startBtn.Pressed += OnStartPressed;
        createGameBtn.Pressed += OnCreateGamePressed;
        joinBtn.Pressed += OnJoinPressed;
        optionsBtn.Pressed += OnOptionsPressed;
        exitBtn.Pressed += OnExitPressed;
        startBtn.GrabFocus();
        lobby = GetNode<ALLobbyUI>("LobbyManager");
        lobby.OnExitLobby -= OnExitLobbyHandler;
        lobby.OnExitLobby += OnExitLobbyHandler;
    }

    void OnStartPressed()
    {
        this.ChangeScene($"{ALSceneRootPath}/match.tscn");
    }
    void OnCreateGamePressed()
    {
        createGameBtn.Disabled = true;
        joinBtn.Disabled = true;
        lobby.Visible = true;
        lobby.CreateGame();
    }
    void OnJoinPressed()
    {
        createGameBtn.Disabled = true;
        joinBtn.Disabled = true;
        lobby.Visible = true;
        lobby.JoinGame();
    }
    void OnOptionsPressed()
    {
        this.ChangeScene($"{ALSceneRootPath}/options.tscn");
    }
    void OnExitPressed() { GetTree().Quit(); }

    void OnExitLobbyHandler()
    {
        createGameBtn.Disabled = false;
        joinBtn.Disabled = false;
        lobby.Visible = false;
    }
}
