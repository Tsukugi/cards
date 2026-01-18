using Godot;
using System;

public partial class ALMain : Control, IALMainAutoMatchHost
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
    CheckButton autoHostMatchToggle, autoJoinMatchToggle;
    [Export]
    LineEdit playerNameInput;
    [Export]
    LineEdit joinAddressInput, joinPortInput;
    [Export]
    Button joinConfirmBtn;
    [Export]
    Label joinAddressLabel, joinPortLabel;
    ALLobbyUI lobby = null;

    ALMainDebug debug;
    bool isAutoMatchInProgress = false;
    int playerNameChangeSerial = 0;

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
        if (autoHostMatchToggle is not null) autoHostMatchToggle.Toggled += OnAutoHostMatchToggled;
        if (autoJoinMatchToggle is not null) autoJoinMatchToggle.Toggled += OnAutoJoinMatchToggled;
        if (playerNameInput is not null) playerNameInput.TextChanged += OnPlayerNameChanged;

        LoadConnectionSettings();
        LoadPlayerNameFromArgs();
        LoadPlayerSettings();
        LoadDebugSettings();
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
        var result = TryConfirmJoinOrHost(out string message);
        if (result != Error.Ok)
        {
            HandleNetworkError(message);
        }
    }

    void OnServerDisconnectedHandler()
    {
        if (isAutoMatchInProgress) return;
        HandleNetworkError("Disconnected from server.");
    }

    void OnConnectionFailedHandler()
    {
        if (isAutoMatchInProgress) return;
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

    void LoadConnectionSettings()
    {
        var settings = ALLocalStorage.LoadConnectionSettings();
        if (settings is null) return;
        if (joinAddressInput is not null) joinAddressInput.Text = settings.Address;
        if (joinPortInput is not null) joinPortInput.Text = settings.Port.ToString();
    }

    void LoadDebugSettings()
    {
        string profileName = GetDebugProfileName();
        GD.Print($"[ALMain.LoadDebugSettings] Profile {profileName}");
        var settings = ALLocalStorage.LoadMatchDebugSettings(profileName);
        if (settings is null) return;
        if (autoHostMatchToggle is not null) autoHostMatchToggle.ButtonPressed = settings.EnableAutoHostMatch;
        if (autoJoinMatchToggle is not null) autoJoinMatchToggle.ButtonPressed = settings.EnableAutoJoinMatch;
    }

    void OnAutoHostMatchToggled(bool enabled)
    {
        if (enabled && autoJoinMatchToggle is not null)
        {
            autoJoinMatchToggle.ButtonPressed = false;
        }
        SaveDebugSettings();
    }

    void OnAutoJoinMatchToggled(bool enabled)
    {
        if (enabled && autoHostMatchToggle is not null)
        {
            autoHostMatchToggle.ButtonPressed = false;
        }
        SaveDebugSettings();
    }

    void SaveDebugSettings()
    {
        string profileName = GetDebugProfileName();
        var settings = ALLocalStorage.LoadMatchDebugSettings(profileName) ?? new ALMatchDebugSettings();
        settings.EnableAutoHostMatch = autoHostMatchToggle is not null && autoHostMatchToggle.ButtonPressed;
        settings.EnableAutoJoinMatch = autoJoinMatchToggle is not null && autoJoinMatchToggle.ButtonPressed;
        ALLocalStorage.SaveMatchDebugSettings(settings, profileName);
        GD.Print($"[ALMain.SaveDebugSettings] Profile {profileName} Host {settings.EnableAutoHostMatch} Join {settings.EnableAutoJoinMatch}");
    }

    void LoadPlayerSettings()
    {
        if (playerNameInput is null) return;
        if (string.IsNullOrWhiteSpace(playerNameInput.Text)) return;
        var settings = ALLocalStorage.LoadPlayerSettings(playerNameInput.Text);
        if (settings is not null) playerNameInput.Text = settings.Name;
        if (Network.Instance is null) throw new InvalidOperationException("[ALMain.LoadPlayerSettings] Network instance is required.");
        Network.Instance.SetPlayerName(playerNameInput.Text);
        GD.Print($"[ALMain.LoadPlayerSettings] Player {playerNameInput.Text}");
    }

    void LoadPlayerNameFromArgs()
    {
        if (playerNameInput is null) return;
        string playerName = GetCommandLineValue("--player-name");
        if (string.IsNullOrWhiteSpace(playerName)) return;
        playerNameInput.Text = playerName;
        GD.Print($"[ALMain.LoadPlayerNameFromArgs] Player {playerName}");
    }

    static string GetCommandLineValue(string key)
    {
        string value = GetCommandLineValueFromArgs(OS.GetCmdlineUserArgs(), key);
        if (!string.IsNullOrWhiteSpace(value)) return value;
        return GetCommandLineValueFromArgs(OS.GetCmdlineArgs(), key);
    }

    static string GetCommandLineValueFromArgs(string[] args, string key)
    {
        if (args is null || args.Length == 0) return "";
        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            if (arg is null) continue;
            if (arg.StartsWith(key + "=", StringComparison.Ordinal))
            {
                return arg[(key.Length + 1)..];
            }
            if (arg == key && index + 1 < args.Length)
            {
                return args[index + 1];
            }
        }
        return "";
    }

    void OnPlayerNameChanged(string newName)
    {
        playerNameChangeSerial++;
        _ = ApplyPlayerNameAfterDelay(newName, playerNameChangeSerial);
    }

    async System.Threading.Tasks.Task ApplyPlayerNameAfterDelay(string newName, int changeSerial)
    {
        await Wait(0.5f);
        if (changeSerial != playerNameChangeSerial) return;
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new InvalidOperationException("[ALMain.ApplyPlayerNameAfterDelay] Player name is required.");
        }
        var settings = ALLocalStorage.LoadPlayerSettings(newName) ?? new ALPlayerSettings();
        settings.Name = newName;
        ALLocalStorage.SavePlayerSettings(settings, newName);
        if (Network.Instance is null) throw new InvalidOperationException("[ALMain.ApplyPlayerNameAfterDelay] Network instance is required.");
        Network.Instance.SetPlayerName(newName);
        GD.Print($"[ALMain.ApplyPlayerNameAfterDelay] Player {newName}");
        LoadDebugSettings();
    }

    public bool IsAutoHostMatchEnabled()
    {
        if (autoHostMatchToggle is not null) return autoHostMatchToggle.ButtonPressed;
        var settings = ALLocalStorage.LoadMatchDebugSettings(GetDebugProfileName());
        return settings is not null && settings.EnableAutoHostMatch;
    }

    public bool IsAutoJoinMatchEnabled()
    {
        if (autoJoinMatchToggle is not null) return autoJoinMatchToggle.ButtonPressed;
        var settings = ALLocalStorage.LoadMatchDebugSettings(GetDebugProfileName());
        return settings is not null && settings.EnableAutoJoinMatch;
    }

    string GetDebugProfileName()
    {
        if (playerNameInput is null)
        {
            throw new InvalidOperationException("[ALMain.GetDebugProfileName] Player name input is required.");
        }
        if (string.IsNullOrWhiteSpace(playerNameInput.Text))
        {
            throw new InvalidOperationException("[ALMain.GetDebugProfileName] Player name is required.");
        }
        return playerNameInput.Text;
    }

    public void SetAutoMatchInProgress(bool enabled) => isAutoMatchInProgress = enabled;

    public void UpdateJoinInputs(string address, int port)
    {
        if (joinAddressInput is not null) joinAddressInput.Text = address;
        if (joinPortInput is not null) joinPortInput.Text = port.ToString();
    }

    public ALConnectionSettings GetJoinConnectionSettings()
    {
        var address = joinAddressInput is not null ? joinAddressInput.Text : Network.DefaultServerIP;
        if (string.IsNullOrWhiteSpace(address)) address = Network.DefaultServerIP;

        int port = Network.DefaultPort;
        if (joinPortInput is not null && !string.IsNullOrWhiteSpace(joinPortInput.Text))
        {
            if (!int.TryParse(joinPortInput.Text, out port))
            {
                throw new InvalidOperationException("[ALMain.GetJoinConnectionSettings] Invalid port.");
            }
        }

        return new ALConnectionSettings
        {
            Address = address,
            Port = port
        };
    }

    public Error TryConfirmJoinOrHost(out string message)
    {
        message = "";
        if (IsActiveMultiplayerPeer())
        {
            message = "Already connected. Disconnect before joining again.";
            return Error.AlreadyInUse;
        }

        if (isHostLobbyMode)
        {
            var createGameResult = CreateGame();
            if (createGameResult != Error.Ok)
            {
                message = $"Create Game failed: {createGameResult}";
                return createGameResult;
            }
            isGameCreated = true;
            if (joinConfirmBtn is not null) joinConfirmBtn.Disabled = true;
            ClearError();
            return Error.Ok;
        }

        string address = Network.DefaultServerIP;
        int port = Network.DefaultPort;
        try
        {
            var connectionSettings = GetJoinConnectionSettings();
            address = connectionSettings.Address;
            port = connectionSettings.Port;
        }
        catch (InvalidOperationException)
        {
            message = "Invalid port.";
            return Error.InvalidParameter;
        }

        ALLocalStorage.SaveConnectionSettings(new ALConnectionSettings
        {
            Address = address,
            Port = port
        });

        var joinGameResult = JoinGame(address, port);
        if (joinGameResult != Error.Ok)
        {
            message = $"Join Game failed: {joinGameResult}";
            return joinGameResult;
        }

        if (joinConfirmBtn is not null) joinConfirmBtn.Disabled = true;
        ClearError();
        return Error.Ok;
    }

    bool IsActiveMultiplayerPeer()
    {
        if (Network.Instance is null)
        {
            throw new InvalidOperationException("[ALMain.IsActiveMultiplayerPeer] Network instance is required.");
        }
        var peer = Network.Instance.Multiplayer.MultiplayerPeer;
        if (peer is null) return false;
        if (peer is OfflineMultiplayerPeer) return false;
        var status = peer.GetConnectionStatus();
        return status == MultiplayerPeer.ConnectionStatus.Connected || status == MultiplayerPeer.ConnectionStatus.Connecting;
    }

    public System.Threading.Tasks.Task Wait(float seconds) => NodeUtils.Wait(this, seconds);

    public static void ExitLobby() => Network.Instance.CloseConnection();
    public static void StartMatch(string path) => Network.Instance.RequestStartMatch(path);
    public static void CheckConnection() => Network.Instance.CheckConnection();
    public static Error JoinGame(string address = Network.DefaultServerIP, int port = Network.DefaultPort) => Network.Instance.JoinGame(address, port);
    public static Error CreateGame() => Network.Instance.CreateGame();

    public void OpenJoinLobby() => OnJoinPressed();
    public void OpenHostLobby() => OnCreateGamePressed();
    public void StartMatch() => OnStartPressed();
}
