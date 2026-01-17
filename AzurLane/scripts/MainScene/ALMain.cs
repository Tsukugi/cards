using Godot;
using System;

public partial class ALMain : Control
{
    bool isGameCreated = false;
    bool isHostLobbyMode = false;
    [Export]
    Button startBtn, optionsBtn, exitBtn, createGameBtn, joinBtn, debugBtn;
    [Export]
    Label errorLabel;
    [Export]
    Panel debugPanel;
    [Export]
    Button simulateErrorBtn, debugCloseBtn;
    [Export]
    LineEdit joinAddressInput, joinPortInput;
    [Export]
    Button joinConfirmBtn;
    [Export]
    Label joinAddressLabel, joinPortLabel;
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
        if (joinConfirmBtn is not null) joinConfirmBtn.Pressed += OnJoinConfirmPressed;

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
        isHostLobbyMode = true;
        SetLobbyMode(isHostMode: true);
        ClearError();

    }
    public void OnJoinPressed()
    {
        startBtn.Disabled = true;
        createGameBtn.Disabled = true;
        joinBtn.Disabled = true;
        lobby.Visible = true;
        isHostLobbyMode = false;
        SetLobbyMode(isHostMode: false);
        ClearError();
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
        isHostLobbyMode = false;
        ExitLobby();
        ClearError();
        if (joinConfirmBtn is not null) joinConfirmBtn.Disabled = false;
    }
    void OnJoinConfirmPressed()
    {
        if (Network.Instance.Multiplayer.MultiplayerPeer is not null)
        {
            var status = Network.Instance.Multiplayer.MultiplayerPeer.GetConnectionStatus();
            if (status == MultiplayerPeer.ConnectionStatus.Connected || status == MultiplayerPeer.ConnectionStatus.Connecting)
            {
                HandleNetworkError("Already connected. Disconnect before joining again.");
                return;
            }
        }

        if (isHostLobbyMode)
        {
            var createGameResult = CreateGame();
            if (createGameResult != Error.Ok)
            {
                HandleNetworkError($"Create Game failed: {createGameResult}");
                return;
            }
            isGameCreated = true;
            if (joinConfirmBtn is not null) joinConfirmBtn.Disabled = true;
            ClearError();
            return;
        }

        var address = joinAddressInput is not null ? joinAddressInput.Text : Network.DefaultServerIP;
        if (string.IsNullOrWhiteSpace(address))
        {
            address = Network.DefaultServerIP;
        }

        int port = Network.DefaultPort;
        if (joinPortInput is not null && !string.IsNullOrWhiteSpace(joinPortInput.Text))
        {
            if (!int.TryParse(joinPortInput.Text, out port))
            {
                HandleNetworkError("Invalid port.");
                return;
            }
        }

        var joinGameResult = JoinGame(address, port);
        if (joinGameResult != Error.Ok)
        {
            HandleNetworkError($"Join Game failed: {joinGameResult}");
        }
        else
        {
            if (joinConfirmBtn is not null) joinConfirmBtn.Disabled = true;
            ClearError();
        }
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
        isHostLobbyMode = false;
        ExitLobby();
        ShowError(message);
        if (joinConfirmBtn is not null) joinConfirmBtn.Disabled = false;
    }

    void SetLobbyMode(bool isHostMode)
    {
        if (joinAddressLabel is not null) joinAddressLabel.Text = isHostMode ? "Host Address" : "Remote Address";
        if (joinPortLabel is not null) joinPortLabel.Text = isHostMode ? "Host Port" : "Remote Port";
        if (joinConfirmBtn is not null)
        {
            joinConfirmBtn.Text = isHostMode ? "Host Match" : "Connect";
            joinConfirmBtn.Disabled = false;
        }
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
    public static Error JoinGame(string address = Network.DefaultServerIP, int port = Network.DefaultPort) => Network.Instance.JoinGame(address, port);
    public static Error CreateGame() => Network.Instance.CreateGame();
}
