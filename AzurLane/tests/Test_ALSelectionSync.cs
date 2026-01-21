using System;
using System.Threading.Tasks;
using Godot;

public sealed class ALSelectionSyncTest : TestBase, ISelectionSyncTest
{
    readonly ALGameMatchManager matchManager;
    readonly Network network;
    readonly bool isServer;
    bool isRunning = false;
    TaskCompletionSource<bool> clientSequenceCompletion;
    bool expectedLoaded = false;
    const string ExpectedSelectionsPath = "res://AzurLane/tests/selection_sync_expected.json";

    const string HostPrefix = "Host";
    const string ClientPrefix = "Client";
    const string MessageCommandPrefix = "CMD:";
    const string CommandRunClient = "RunClientSequence";
    const string CommandClientDone = "ClientSequenceComplete";

    public ALSelectionSyncTest(ALGameMatchManager _matchManager, float _stepSeconds) : base(_stepSeconds)
    {
        matchManager = _matchManager ?? throw new InvalidOperationException("[ALSelectionSyncTest] Match manager is required.");
        network = Network.Instance ?? throw new InvalidOperationException("[ALSelectionSyncTest] Network instance is required.");
        isServer = network.Multiplayer.IsServer();
        network.OnSelectionSyncTestMessageEvent -= HandleRemoteMessage;
        network.OnSelectionSyncTestMessageEvent += HandleRemoteMessage;
    }

    public async Task Run()
    {
        if (!isServer)
        {
            GD.Print("[SelectionSyncTest] Listener ready.");
            return;
        }

        if (isRunning)
        {
            GD.Print("[SelectionSyncTest] Already running.");
            return;
        }
        isRunning = true;

        ALPlayer player = matchManager.GetControlledPlayer();
        if (player is null)
        {
            throw new InvalidOperationException("[ALSelectionSyncTest] Controlled player is required.");
        }

        await TestUtils.AwaitMatchReady(player, matchManager);

        GD.Print("[SelectionSyncTest] Main phase reached.");
        await RunTraversal(player, HostPrefix);
        await RunClientSequence();
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
        GD.PrintErr($"[SelectionSyncTest.Remote] Unknown message '{message}' from {peerId}.");
    }

    void HandleRemoteCommand(int peerId, string command)
    {
        GD.Print($"[SelectionSyncTest.Command] From {peerId} command={command}");
        if (command == CommandRunClient)
        {
            _ = RunClientSequenceLocal();
            return;
        }
        if (command == CommandClientDone && clientSequenceCompletion is not null && !clientSequenceCompletion.Task.IsCompleted)
        {
            clientSequenceCompletion.TrySetResult(true);
        }
    }

    async Task RunClientSequence()
    {
        clientSequenceCompletion = new TaskCompletionSource<bool>();
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandRunClient}");
        await AwaitSignal(clientSequenceCompletion, "[SelectionSyncTest] Client sequence timed out.");
    }

    async Task RunClientSequenceLocal()
    {
        if (isRunning)
        {
            throw new InvalidOperationException("[SelectionSyncTest] Client sequence already running.");
        }
        isRunning = true;
        ALPlayer player = matchManager.GetControlledPlayer();
        if (player is null)
        {
            throw new InvalidOperationException("[SelectionSyncTest] Controlled player is required for client run.");
        }
        await TestUtils.AwaitMatchReady(player, matchManager);
        await RunTraversal(player, ClientPrefix);
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandClientDone}");
        isRunning = false;
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
