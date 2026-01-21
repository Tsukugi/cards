using System;
using System.Threading.Tasks;
using Godot;

public sealed class ALSelectionSyncSimulTest : TestBase, ISelectionSyncTest
{
    readonly ALGameMatchManager matchManager;
    readonly Network network;
    readonly bool isServer;
    bool isRunning = false;

    TaskCompletionSource<bool> traversalSimulReady;
    TaskCompletionSource<bool> traversalSimulDone;
    TaskCompletionSource<bool> phaseSkipReady;
    bool expectedLoaded = false;
    const string ExpectedSelectionsPath = "res://AzurLane/tests/selection_sync_expected.json";

    const string HostPrefix = "Host";
    const string ClientPrefix = "Client";
    const string MessageCommandPrefix = "CMD:";
    const string CommandTraversalSimulPrepare = "TraversalSimul.Prepare";
    const string CommandTraversalSimulReady = "TraversalSimul.Ready";
    const string CommandTraversalSimulStart = "TraversalSimul.Start";
    const string CommandTraversalSimulDone = "TraversalSimul.Done";
    const string CommandSkipToMain = "Phase.SkipToMain";
    const string CommandSkipToMainReady = "Phase.SkipToMain.Ready";

    public ALSelectionSyncSimulTest(ALGameMatchManager _matchManager, float _stepSeconds) : base(_stepSeconds)
    {
        matchManager = _matchManager ?? throw new InvalidOperationException("[ALSelectionSyncSimulTest] Match manager is required.");
        network = Network.Instance ?? throw new InvalidOperationException("[ALSelectionSyncSimulTest] Network instance is required.");
        isServer = network.Multiplayer.IsServer();
        network.OnSelectionSyncTestMessageEvent -= HandleRemoteMessage;
        network.OnSelectionSyncTestMessageEvent += HandleRemoteMessage;
    }

    public async Task Run()
    {
        if (!isServer)
        {
            GD.Print("[SelectionSyncSimulTest] Listener ready.");
            return;
        }

        if (isRunning)
        {
            GD.Print("[SelectionSyncSimulTest] Already running.");
            return;
        }
        isRunning = true;

        ALPlayer player = matchManager.GetControlledPlayer();
        if (player is null)
        {
            throw new InvalidOperationException("[ALSelectionSyncSimulTest] Controlled player is required.");
        }

        await AwaitPlayersConnected(player);
        await SkipToMainPhase();
        GD.Print("[SelectionSyncSimulTest] Forced main phase for test.");
        await RunTraversalSimultaneous(player);
        isRunning = false;
    }

    async Task RunTraversal(ALPlayer player, string prefix)
    {
        if (!expectedLoaded)
        {
            LoadExpectedSelectionsFromFile(ExpectedSelectionsPath, true);
            expectedLoaded = true;
        }

        ALBoard ownBoard = player.GetPlayerBoard<ALBoard>();
        ALBoard enemyBoard = player.GetEnemyPlayerBoard<ALBoard>();
        ALHand ownHand = player.GetPlayerHand<ALHand>();
        ALHand enemyHand = player.GetEnemyPlayerHand<ALHand>();

        await EnsureSelectionAt(player, ownHand, Vector2I.Zero, $"{prefix}.Hand.Start");
        await MoveAxisUntilBoard(player, Vector2I.Up, ownBoard, 2, $"{prefix}.Hand.ToBoard");
        await MoveAxisUntilBoard(player, Vector2I.Up, enemyBoard, 2, $"{prefix}.Board.ToEnemyBoard");
        await MoveAxisUntilBoard(player, Vector2I.Down, ownBoard, 2, $"{prefix}.EnemyBoard.ToBoard");
        await MoveAxisUntilBoard(player, Vector2I.Down, ownHand, 4, $"{prefix}.Board.ToHand");
        await MoveAxisUntilBoard(player, Vector2I.Up, ownBoard, 2, $"{prefix}.Hand.ToBoard2");
        await MoveAxisUntilBoard(player, Vector2I.Up, enemyBoard, 2, $"{prefix}.Board.ToEnemyBoard2");
        await MoveAxisUntilBoard(player, Vector2I.Up, enemyHand, 4, $"{prefix}.EnemyBoard.ToEnemyHand");
        await MoveAxisUntilBoard(player, Vector2I.Down, enemyBoard, 4, $"{prefix}.EnemyHand.ToEnemyBoard");
        await MoveAxisUntilBoard(player, Vector2I.Up, enemyHand, 4, $"{prefix}.EnemyBoard.ToEnemyHand2");
        await MoveAxisUntilBoard(player, Vector2I.Down, enemyBoard, 4, $"{prefix}.EnemyHand.ToEnemyBoard2");
        await MoveAxisUntilBoard(player, Vector2I.Down, ownBoard, 2, $"{prefix}.EnemyBoard.ToBoard2");
        await MoveAxisUntilBoard(player, Vector2I.Up, enemyBoard, 2, $"{prefix}.Board.ToEnemyBoard3");
        await MoveAxisUntilBoard(player, Vector2I.Down, ownBoard, 2, $"{prefix}.EnemyBoard.ToBoard3");
        await MoveAxisUntilBoard(player, Vector2I.Down, ownHand, 4, $"{prefix}.Board.ToHand2");
    }

    async void HandleRemoteMessage(int peerId, string message)
    {
        if (message.StartsWith(MessageCommandPrefix, StringComparison.Ordinal))
        {
            string command = message[MessageCommandPrefix.Length..];
            HandleRemoteCommand(peerId, command);
            return;
        }
        GD.PrintErr($"[SelectionSyncSimulTest.Remote] Unknown message '{message}' from {peerId}.");
    }

    void HandleRemoteCommand(int peerId, string command)
    {
        GD.Print($"[SelectionSyncSimulTest.Command] From {peerId} command={command}");
        if (command == CommandTraversalSimulPrepare)
        {
            _ = PrepareTraversalSimultaneousLocal();
            return;
        }
        if (command == CommandSkipToMain)
        {
            _ = SkipToMainPhaseLocal();
            return;
        }
        if (command == CommandSkipToMainReady && phaseSkipReady is not null && !phaseSkipReady.Task.IsCompleted)
        {
            phaseSkipReady.TrySetResult(true);
            return;
        }
        if (command == CommandTraversalSimulReady && traversalSimulReady is not null && !traversalSimulReady.Task.IsCompleted)
        {
            traversalSimulReady.TrySetResult(true);
            return;
        }
        if (command == CommandTraversalSimulStart)
        {
            _ = RunTraversalSimultaneousLocal();
            return;
        }
        if (command == CommandTraversalSimulDone && traversalSimulDone is not null && !traversalSimulDone.Task.IsCompleted)
        {
            traversalSimulDone.TrySetResult(true);
        }
    }

    async Task RunTraversalSimultaneous(ALPlayer player)
    {
        traversalSimulReady = new TaskCompletionSource<bool>();
        traversalSimulDone = new TaskCompletionSource<bool>();
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandTraversalSimulPrepare}");
        await AwaitSignal(traversalSimulReady, "[SelectionSyncSimulTest] Traversal simultaneous ready timed out.");

        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandTraversalSimulStart}");
        Task hostTraversal = RunTraversal(player, $"{HostPrefix}.Simul");
        Task waitDone = AwaitSignal(traversalSimulDone, "[SelectionSyncSimulTest] Traversal simultaneous done timed out.");
        await Task.WhenAll(hostTraversal, waitDone);
    }

    async Task SkipToMainPhase()
    {
        phaseSkipReady = new TaskCompletionSource<bool>();
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandSkipToMain}");
        await SkipToMainPhaseLocal();
        await AwaitSignal(phaseSkipReady, "[SelectionSyncSimulTest] Skip-to-main timed out.");
    }

    async Task SkipToMainPhaseLocal()
    {
        ALPlayer player = matchManager.GetControlledPlayer();
        if (player is null)
        {
            throw new InvalidOperationException("[ALSelectionSyncSimulTest] Controlled player is required for skip-to-main.");
        }

        foreach (ALPlayer target in matchManager.GetOrderedPlayers())
        {
            target.Phase.SetSkipAutoPhases(true);
            foreach (ALCard cube in target.GetCubesInBoard())
            {
                cube.DestroyCard();
            }
        }

        await player.Phase.ForceMainPhase(true);
        foreach (ALPlayer target in matchManager.GetOrderedPlayers())
        {
            if (target == player)
            {
                continue;
            }
            await target.Phase.ForceMainPhase(false);
        }

        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandSkipToMainReady}");
    }

    async Task PrepareTraversalSimultaneousLocal()
    {
        ALPlayer player = matchManager.GetControlledPlayer();
        if (player is null)
        {
            throw new InvalidOperationException("[ALSelectionSyncSimulTest] Controlled player is required for traversal prep.");
        }
        await AwaitPlayersConnected(player);
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandTraversalSimulReady}");
    }

    async Task RunTraversalSimultaneousLocal()
    {
        if (isRunning)
        {
            throw new InvalidOperationException("[ALSelectionSyncSimulTest] Traversal simultaneous already running.");
        }
        isRunning = true;
        ALPlayer player = matchManager.GetControlledPlayer();
        if (player is null)
        {
            throw new InvalidOperationException("[ALSelectionSyncSimulTest] Controlled player is required for traversal run.");
        }
        await AwaitPlayersConnected(player);
        await RunTraversal(player, $"{ClientPrefix}.Simul");
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandTraversalSimulDone}");
        isRunning = false;
    }

    async Task AwaitPlayersConnected(ALPlayer player)
    {
        await player.GetPlayerAsyncHandler().AwaitForCheck(
            null,
            () => matchManager.GetEnemyPlayer().MultiplayerId != 0,
            -1);
    }

    async Task AwaitSignal(TaskCompletionSource<bool> signal, string errorMessage)
    {
        var timeoutTask = Task.Delay(30000);
        Task completed = await Task.WhenAny(signal.Task, timeoutTask);
        if (completed == timeoutTask)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }
}
