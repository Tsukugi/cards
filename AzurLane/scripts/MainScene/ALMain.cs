using Godot;
using System;

public partial class ALMain : Control
{
    bool isGameCreated = false;
    [Export]
    Button startBtn, optionsBtn, exitBtn, createGameBtn, joinBtn, debugBtn;
    [Export]
    Label errorLabel;
    [Export]
    Panel debugPanel;
    [Export]
    Button simulateErrorBtn, debugCloseBtn;
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
        debugBtn.Pressed += OnDebugPressed;
        optionsBtn.Pressed += OnOptionsPressed;
        exitBtn.Pressed += OnExitPressed;
        startBtn.GrabFocus();
        lobby = GetNode<ALLobbyUI>("LobbyManager");
        lobby.OnExitLobby -= OnExitLobbyHandler;
        lobby.OnExitLobby += OnExitLobbyHandler;
        Network.Instance.ServerDisconnected += OnServerDisconnectedHandler;
        Network.Instance.ConnectionFailed += OnConnectionFailedHandler;

        if (errorLabel is not null) errorLabel.Visible = false;
        if (debugPanel is not null) debugPanel.Visible = false;
        if (simulateErrorBtn is not null) simulateErrorBtn.Pressed += OnSimulateErrorPressed;
        if (debugCloseBtn is not null) debugCloseBtn.Pressed += OnDebugClosePressed;

        Callable.From(debug.AutoSyncStart).CallDeferred();
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
        var result = CreateGame();
        if (result != Error.Ok)
        {
            HandleNetworkError($"Create Game failed: {result}");
        }
        else ClearError();

    }
    public void OnJoinPressed()
    {
        startBtn.Disabled = true;
        createGameBtn.Disabled = true;
        joinBtn.Disabled = true;
        lobby.Visible = true;
        var result = JoinGame();
        if (result != Error.Ok)
        {
            HandleNetworkError($"Join Game failed: {result}");
        }
        else ClearError();
    }
    void OnDebugPressed()
    {
        if (debugPanel is null) return;
        debugPanel.Visible = true;
    }

    void OnDebugClosePressed()
    {
        if (debugPanel is null) return;
        debugPanel.Visible = false;
    }

    void OnSimulateErrorPressed()
    {
        HandleNetworkError("Simulated error message.");
        if (debugPanel is not null) debugPanel.Visible = false;
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
        ExitLobby();
        ClearError();
    }

    void OnServerDisconnectedHandler()
    {
        HandleNetworkError("Disconnected from server.");
    }

    void OnConnectionFailedHandler()
    {
        HandleNetworkError("Connection failed.");
    }

    void HandleNetworkError(string message)
    {
        startBtn.Disabled = false;
        createGameBtn.Disabled = false;
        joinBtn.Disabled = false;
        lobby.Visible = false;
        isGameCreated = false;
        ExitLobby();
        ShowError(message);
    }

    void ShowError(string message)
    {
        if (errorLabel is null) return;
        errorLabel.Text = message;
        errorLabel.Visible = true;
    }

    void ClearError()
    {
        if (errorLabel is null) return;
        errorLabel.Text = "";
        errorLabel.Visible = false;
    }

    public static void ExitLobby() => Network.Instance.CloseConnection();
    public static void StartMatch(string path) => Network.Instance.RequestStartMatch(path);
    public static void CheckConnection() => Network.Instance.CheckConnection();
    public static Error JoinGame(string address = Network.DefaultServerIP) => Network.Instance.JoinGame(address);
    public static Error CreateGame() => Network.Instance.CreateGame();
}
