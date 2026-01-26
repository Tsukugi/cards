using System.Threading.Tasks;
using Godot;

public partial class ALGameMatchManager
{
    bool ShouldSkipAutoPhasesForTest()
    {
        return IsTestRunRequested();
    }

    async void StartMatchForPlayer()
    {
        await AssignDeckSet();
        if (ShouldSkipAutoPhasesForTest())
        {
            userPlayer.Phase.SetSkipAutoPhases(true);
        }
        await userPlayer.StartGameForPlayer(userPlayer.GetDeckSet());
        if (Multiplayer.IsServer())
        {
            if (ShouldSkipAutoPhasesForTest())
            {
                await userPlayer.Phase.ForceMainPhase(true);
            }
            else
            {
                StartLocalTurnIfNeeded();
            }
        }
        // if (!GetNextPlayer().GetIsControllerPlayer()) Callable.From(GetNextPlayer().GetPlayerAIController().StartTurn).CallDeferred();
    }

    void TryStartGameplayTest()
    {
        string testPath = GetTestFilter();
        if (string.IsNullOrWhiteSpace(testPath))
        {
            return;
        }
        string className = GetGameplayTestClassName(testPath);
        if (string.IsNullOrWhiteSpace(className))
        {
            throw new System.InvalidOperationException($"[ALGameMatchManager.TryStartGameplayTest] Invalid test path: {testPath}");
        }
        System.Type testType = FindGameplayTestType(className) ?? throw new System.InvalidOperationException($"[ALGameMatchManager.TryStartGameplayTest] No gameplay test type found for {className}.");
        var ctor = testType.GetConstructor([typeof(ALGameMatchManager), typeof(float)]) ?? throw new System.InvalidOperationException($"[ALGameMatchManager.TryStartGameplayTest] Missing required constructor on {className}.");
        object instance = ctor.Invoke([this, debug.GetSelectionSyncStepSeconds()]);
        if (instance is not ISelectionSyncTest test)
        {
            throw new System.InvalidOperationException($"[ALGameMatchManager.TryStartGameplayTest] {className} does not implement ISelectionSyncTest.");
        }
        _ = test.Run();
    }

    static string GetGameplayTestClassName(string testPath)
    {
        string normalized = testPath.Replace('\\', '/').Trim();
        string fileName = System.IO.Path.GetFileNameWithoutExtension(normalized);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "";
        }
        if (fileName.StartsWith("Test_", System.StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName[5..];
        }
        if (fileName.EndsWith("Test", System.StringComparison.OrdinalIgnoreCase))
        {
            return fileName;
        }
        return $"{fileName}Test";
    }

    static System.Type FindGameplayTestType(string className)
    {
        var assembly = typeof(ALGameMatchManager).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (type is null || type.IsAbstract)
            {
                continue;
            }
            if (!typeof(ISelectionSyncTest).IsAssignableFrom(type))
            {
                continue;
            }
            if (string.Equals(type.Name, className, System.StringComparison.Ordinal))
            {
                return type;
            }
        }
        return null;
    }

    bool IsTestRunRequested()
    {
        return IsSingleTestRunRequested();
    }

    static bool IsSingleTestRunRequested()
    {
        string value = GetTestFilter();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return !IsFalseValue(value);
        }
        return HasCommandLineFlag("--test") || HasCommandLineFlag("-test");
    }

    static bool IsFalseValue(string value)
    {
        return string.Equals(value, "0", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "false", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool HasCommandLineFlag(string key)
    {
        return HasCommandLineFlagInArgs(OS.GetCmdlineUserArgs(), key)
            || HasCommandLineFlagInArgs(OS.GetCmdlineArgs(), key);
    }

    static bool HasCommandLineFlagInArgs(string[] args, string key)
    {
        if (args is null || args.Length == 0) return false;
        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            if (arg is null) continue;
            if (arg == key || arg.StartsWith(key + "=", System.StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    static string GetCommandLineValue(string key)
    {
        string value = GetCommandLineValueFromArgs(OS.GetCmdlineUserArgs(), key);
        if (!string.IsNullOrWhiteSpace(value)) return value;
        return GetCommandLineValueFromArgs(OS.GetCmdlineArgs(), key);
    }

    static string GetTestFilter()
    {
        string value = GetCommandLineValue("--test");
        if (string.IsNullOrWhiteSpace(value))
        {
            value = GetCommandLineValue("-test");
        }
        if (value is null)
        {
            return "";
        }
        return value;
    }

    static string GetCommandLineValueFromArgs(string[] args, string key)
    {
        if (args is null || args.Length == 0) return "";
        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            if (arg is null) continue;
            if (arg.StartsWith(key + "=", System.StringComparison.Ordinal))
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

    async Task OnGameOverHandler(Player losingPlayer)
    {
        bool isVictory = !losingPlayer.GetIsControllerPlayer();
        await playerUI.ShowGameOverUI(isVictory);
        await ExitMatch();
    }

    async Task ExitMatch()
    {
        this.ChangeScene($"{ALMain.ALSceneRootPath}/main.tscn");
        await Task.CompletedTask;
    }

    void OnPhaseChangeHandler(EALTurnPhase phase)
    {
        matchCurrentPhase = phase;
    }

    async void OnTurnEndHandler()
    {
        await this.Wait(1f);
        if (!IsLocalTurn())
        {
            GD.PushError("[OnTurnEndHandler] Local player cannot end a remote turn.");
            return;
        }
        GD.Print($"[OnTurnEndHandler] {userPlayer.Name} Turn ended!");

        await userPlayer.TryToTriggerOnAllCards(ALCardEffectTrigger.StartOfTurn);
        await this.Wait(1f);
        ALNetwork.Instance.SendTurnEnd();
        AdvanceTurnOwner();
        // if (!GetPlayerPlayingTurn().GetIsControllerPlayer()) GetPlayerPlayingTurn().GetPlayerAIController().StartTurn();
    }
}
