using Godot;
using System;

public partial class ALMain : Control
{
    bool isGameCreated = false;
    [Export]
    Button startBtn, optionsBtn, exitBtn, createGameBtn, joinBtn;
    ALLobbyUI lobby = null;

    ALMainDebug debug;

    public static string ALSceneRootPath = "res://AzurLane/scenes";

    public bool IsGameCreated { get => isGameCreated; }

    public override void _Ready()
    {
        base._Ready();
        debug = new(this);
        startBtn.Pressed += OnStartPressed;
        createGameBtn.Pressed += OnCreateGamePressed;
        joinBtn.Pressed += OnJoinPressed;
        optionsBtn.Pressed += OnOptionsPressed;
        exitBtn.Pressed += OnExitPressed;
        startBtn.GrabFocus();
        lobby = GetNode<ALLobbyUI>("LobbyManager");
        lobby.OnExitLobby -= OnExitLobbyHandler;
        lobby.OnExitLobby += OnExitLobbyHandler;

        Callable.From(() => debug.AutoSyncStart()).CallDeferred();
    }

    public void OnStartPressed()
    {
        StartMatch($"{ALSceneRootPath}/match.tscn");
    }
    public void OnCreateGamePressed()
    {
        createGameBtn.Disabled = true;
        joinBtn.Disabled = true;
        lobby.Visible = true;
        isGameCreated = true;
        CreateGame();
    }
    public void OnJoinPressed()
    {
        startBtn.Disabled = true;
        createGameBtn.Disabled = true;
        joinBtn.Disabled = true;
        lobby.Visible = true;
        JoinGame();
    }
    void OnOptionsPressed()
    {
        this.ChangeScene($"{ALSceneRootPath}/options.tscn");
    }
    void OnExitPressed() { GetTree().Quit(); }

    void OnExitLobbyHandler()
    {
        startBtn.Disabled = false;
        createGameBtn.Disabled = false;
        joinBtn.Disabled = false;
        lobby.Visible = false;
        isGameCreated = false;
    }

    public static void StartMatch(string path) => Network.Instance.RequestStartMatch(path);
    public static void CheckConnection() => Network.Instance.CheckConnection();
    public static Error JoinGame(string address = "") => Network.Instance.JoinGame(address);
    public static Error CreateGame() => Network.Instance.CreateGame();
}
