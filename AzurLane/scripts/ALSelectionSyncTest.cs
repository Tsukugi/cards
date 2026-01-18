using System.Threading.Tasks;
using Godot;

public sealed class ALSelectionSyncTest
{
    readonly ALGameMatchManager matchManager;
    readonly float stepSeconds;
    readonly Network network;
    readonly bool isServer;
    bool isRunning = false;
    TaskCompletionSource<bool> clientSequenceCompletion;
    const string HostPrefix = "Host";
    const string ClientPrefix = "Client";
    const string MessageStepPrefix = "STEP:";
    const string MessageCommandPrefix = "CMD:";
    const string CommandRunClient = "RunClientSequence";
    const string CommandClientDone = "ClientSequenceComplete";
    const string CommandRemoteMappingAck = "RemoteMapping.Ack";
    const string CommandCrossBoardStart = "CrossBoardScenario.Start";
    const string CommandCrossBoardReady = "CrossBoardScenario.Ready";
    const string CommandCrossBoardProceed = "CrossBoardScenario.Proceed";
    const string CommandCrossBoardBUpDone = "CrossBoardScenario.BUpDone";
    const string CommandCrossBoardBDone = "CrossBoardScenario.BDownDone";
    TaskCompletionSource<bool> remoteMappingAck;
    TaskCompletionSource<bool> crossBoardReady;
    TaskCompletionSource<bool> crossBoardProceed;
    TaskCompletionSource<bool> crossBoardBUpDone;
    TaskCompletionSource<bool> crossBoardBDone;

    public ALSelectionSyncTest(ALGameMatchManager _matchManager, float _stepSeconds)
    {
        matchManager = _matchManager ?? throw new System.InvalidOperationException("[ALSelectionSyncTest] Match manager is required.");
        stepSeconds = _stepSeconds <= 0 ? 1f : _stepSeconds;
        network = Network.Instance ?? throw new System.InvalidOperationException("[ALSelectionSyncTest] Network instance is required.");
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
            throw new System.InvalidOperationException("[ALSelectionSyncTest] Controlled player is required.");
        }
        await player.GetPlayerAsyncHandler().AwaitForCheck(
            null,
            () => matchManager.GetEnemyPlayer().MultiplayerId != 0,
            -1);

        await player.GetPlayerAsyncHandler().AwaitForCheck(
            null,
            () => matchManager.GetMatchPhase() == EALTurnPhase.Main,
            -1);

        GD.Print("[SelectionSyncTest] Main phase reached.");
        await RunSequence(player, HostPrefix);
        await RunClientSequence();
        isRunning = false;
    }

    async Task RunSequence(ALPlayer player, string prefix)
    {
        await RunSelectionSteps(player, prefix);
        await TestRemoteEnemyBoardMapping(player, prefix);
        await TestCrossBoardSharedSelection(player, prefix);
        await TestBoardSwitchRoundTrip(player, prefix);
        await TestMultiSelectorSameBoard(player, prefix);
    }

    async Task RunSelectionSteps(ALPlayer player, string prefix)
    {
        ALBoard board = player.GetPlayerBoard<ALBoard>();
        await EnsureSelectionAt(player, board, Vector2I.Zero);
        LogSelectionState($"{prefix}.OwnBoard.Start");
        SendStepLabel($"{prefix}.OwnBoard.Start");

        await MoveAxis(player, Vector2I.Right, $"{prefix}.OwnBoard.Right");
        await MoveAxis(player, Vector2I.Right, $"{prefix}.OwnBoard.Right");
        await MoveAxis(player, Vector2I.Left, $"{prefix}.OwnBoard.Left");

        board.SelectCardField(player, Vector2I.Zero);
        await player.Wait(stepSeconds);

        await MoveAxis(player, Vector2I.Up, $"{prefix}.OwnBoard.UpToEnemy");
        await MoveAxis(player, Vector2I.Right, $"{prefix}.EnemyBoard.Right");
        await MoveAxis(player, Vector2I.Left, $"{prefix}.EnemyBoard.Left");

        await EnsureSelectionAt(player, board, Vector2I.One);
        LogSelectionState($"{prefix}.OwnBoard.Row1");
        SendStepLabel($"{prefix}.OwnBoard.Row1");

        await MoveAxis(player, Vector2I.Up, $"{prefix}.OwnBoard.UpToEnemy2");
        await MoveAxis(player, Vector2I.Left, $"{prefix}.EnemyBoard.Left2");
        await MoveAxis(player, Vector2I.Right, $"{prefix}.EnemyBoard.Right2");
    }

    async Task TestRemoteEnemyBoardMapping(ALPlayer player, string prefix)
    {
        if (!isServer)
        {
            return;
        }

        ALBoard enemyBoard = player.GetEnemyPlayerBoard<ALBoard>();
        await EnsureSelectionAt(player, enemyBoard, Vector2I.Zero);
        LogSelectionState($"{prefix}.EnemyBoard.ZeroZero");
        remoteMappingAck = new TaskCompletionSource<bool>();
        SendStepLabel($"{prefix}.AssertRemoteEnemyBoardOwnBoard00");
        await AwaitCrossBoardTask(remoteMappingAck, "[SelectionSyncTest] Remote mapping ack timed out.");
    }

    async Task TestCrossBoardSharedSelection(ALPlayer player, string prefix)
    {
        if (!isServer)
        {
            return;
        }

        await EnsureSelectionAt(player, player.GetPlayerBoard<ALBoard>(), Vector2I.Zero);
        await RequestCrossBoardReady();

        await MoveAxis(player, Vector2I.Up, $"{prefix}.CrossBoard.A.UpToEnemy");
        AssertCrossBoardStateAfterAUp(player);

        await RequestCrossBoardProceed();
        await WaitForCrossBoardBUp();
        AssertCrossBoardStateAfterBUp(player);

        await WaitForCrossBoardBDone();
        AssertCrossBoardStateAfterBDown(player);
    }

    async Task EnsureSelectionAt(ALPlayer player, Board board, Vector2I position)
    {
        board.SelectCardField(player, position);
        player.SelectBoard(player, board);
        await player.Wait(stepSeconds);
    }

    async Task RequestCrossBoardReady()
    {
        crossBoardReady = new TaskCompletionSource<bool>();
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandCrossBoardStart}");
        await AwaitCrossBoardTask(crossBoardReady, "[SelectionSyncTest] Cross-board ready timed out.");
    }

    async Task RequestCrossBoardProceed()
    {
        crossBoardProceed = new TaskCompletionSource<bool>();
        crossBoardBUpDone = new TaskCompletionSource<bool>();
        crossBoardBDone = new TaskCompletionSource<bool>();
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandCrossBoardProceed}");
        await AwaitCrossBoardTask(crossBoardProceed, "[SelectionSyncTest] Cross-board proceed timed out.");
    }

    async Task WaitForCrossBoardBUp()
    {
        await AwaitCrossBoardTask(crossBoardBUpDone, "[SelectionSyncTest] Cross-board B up timed out.");
    }

    async Task WaitForCrossBoardBDone()
    {
        await AwaitCrossBoardTask(crossBoardBDone, "[SelectionSyncTest] Cross-board B down timed out.");
    }

    async Task AwaitCrossBoardTask(TaskCompletionSource<bool> taskSource, string errorMessage)
    {
        var timeoutTask = Task.Delay(15000);
        Task completed = await Task.WhenAny(taskSource.Task, timeoutTask);
        if (completed == timeoutTask)
        {
            throw new System.InvalidOperationException(errorMessage);
        }
    }

    void AssertCrossBoardStateAfterAUp(ALPlayer player)
    {
        ALPlayer enemyPlayer = matchManager.GetEnemyPlayer();
        if (enemyPlayer is null)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Enemy player required for cross-board assertion.");
        }

        Board expectedLocalBoard = player.GetEnemyPlayerBoard<ALBoard>();
        Board expectedEnemyBoard = enemyPlayer.GetPlayerBoard<ALBoard>();
        AssertPlayerSelection(player, expectedLocalBoard, new Vector2I(2, 0), "CrossBoard.AUp.Local");
        AssertPlayerSelection(enemyPlayer, expectedEnemyBoard, Vector2I.Zero, "CrossBoard.AUp.Enemy");
        AssertOnlyBoardHasSelection(player, expectedLocalBoard, "CrossBoard.AUp.LocalOnly");
        AssertOnlyBoardHasSelection(enemyPlayer, expectedEnemyBoard, "CrossBoard.AUp.EnemyOnly");
    }

    void AssertCrossBoardStateAfterBUp(ALPlayer player)
    {
        ALPlayer enemyPlayer = matchManager.GetEnemyPlayer();
        if (enemyPlayer is null)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Enemy player required for cross-board assertion.");
        }

        Board expectedLocalBoard = player.GetEnemyPlayerBoard<ALBoard>();
        Board expectedEnemyBoard = enemyPlayer.GetEnemyPlayerBoard<ALBoard>();
        AssertPlayerSelection(player, expectedLocalBoard, new Vector2I(2, 0), "CrossBoard.BUp.Local");
        AssertPlayerSelection(enemyPlayer, expectedEnemyBoard, new Vector2I(2, 0), "CrossBoard.BUp.Enemy");
        AssertOnlyBoardHasSelection(player, expectedLocalBoard, "CrossBoard.BUp.LocalOnly");
        AssertOnlyBoardHasSelection(enemyPlayer, expectedEnemyBoard, "CrossBoard.BUp.EnemyOnly");
    }

    void AssertCrossBoardStateAfterBDown(ALPlayer player)
    {
        ALPlayer enemyPlayer = matchManager.GetEnemyPlayer();
        if (enemyPlayer is null)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Enemy player required for cross-board assertion.");
        }

        Board expectedLocalBoard = player.GetEnemyPlayerBoard<ALBoard>();
        Board expectedEnemyBoard = enemyPlayer.GetPlayerBoard<ALBoard>();
        AssertPlayerSelection(player, expectedLocalBoard, new Vector2I(2, 0), "CrossBoard.BDown.Local");
        AssertPlayerSelection(enemyPlayer, expectedEnemyBoard, Vector2I.Zero, "CrossBoard.BDown.Enemy");
        AssertOnlyBoardHasSelection(player, expectedLocalBoard, "CrossBoard.BDown.LocalOnly");
        AssertOnlyBoardHasSelection(enemyPlayer, expectedEnemyBoard, "CrossBoard.BDown.EnemyOnly");
    }

    void AssertPlayerSelection(ALPlayer player, Board expectedBoard, Vector2I expectedPos, string label)
    {
        Card selected = expectedBoard.GetSelectedCard<Card>(player);
        if (selected is null)
        {
            throw new System.InvalidOperationException($"[SelectionSyncTest] {label} missing selection on {expectedBoard.Name}.");
        }
        if (selected.PositionInBoard != expectedPos)
        {
            throw new System.InvalidOperationException($"[SelectionSyncTest] {label} expected {expectedPos}, got {selected.PositionInBoard} on {expectedBoard.Name}.");
        }
    }

    void AssertOnlyBoardHasSelection(ALPlayer player, Board expectedBoard, string label)
    {
        Board playerHand = player.GetPlayerHand<PlayerHand>();
        Board playerBoard = player.GetPlayerBoard<PlayerBoard>();
        Board enemyBoard = player.GetEnemyPlayerBoard<PlayerBoard>();
        Board enemyHand = player.GetEnemyPlayerHand<PlayerHand>();
        Board[] boards = [playerHand, playerBoard, enemyBoard, enemyHand];
        foreach (Board board in boards)
        {
            Card selected = board.GetSelectedCard<Card>(player);
            if (board == expectedBoard)
            {
                if (selected is null)
                {
                    throw new System.InvalidOperationException($"[SelectionSyncTest] {label} expected selection on {board.Name}.");
                }
                continue;
            }
            if (selected is not null)
            {
                throw new System.InvalidOperationException($"[SelectionSyncTest] {label} unexpected selection on {board.Name}.");
            }
        }
    }

    async Task MoveAxis(ALPlayer player, Vector2I axis, string label)
    {
        GD.Print($"[SelectionSyncTest.Step] {label} axis={axis}");
        player.GetSelectedBoard().OnInputAxisChange(player, axis);
        await player.Wait(stepSeconds);
        LogSelectionState(label);
        SendStepLabel(label);
    }

    async Task TestBoardSwitchRoundTrip(ALPlayer player, string prefix)
    {
        ALBoard board = player.GetPlayerBoard<ALBoard>();
        ALBoard enemyBoard = player.GetEnemyPlayerBoard<ALBoard>();
        var candidates = board.GetCardsInTree().FindAll(card =>
            card.IsInputSelectable
            && card.EdgeUp is not null
            && card.PositionInBoard.Y == 0
            && card.PositionInBoard.X >= 0
            && card.PositionInBoard.X <= 2);
        candidates.Sort((left, right) => left.PositionInBoard.X.CompareTo(right.PositionInBoard.X));

        if (candidates.Count == 0)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] No edge-linked cards available for board switching.");
        }

        foreach (Card card in candidates)
        {
            GD.Print($"[SelectionSyncTest.RoundTrip] {prefix} Start {card.Name} pos={card.PositionInBoard}");
            await EnsureSelectionAt(player, board, card.PositionInBoard);
            LogSelectionState($"{prefix}.RoundTrip.Start.{card.Name}");
            SendStepLabel($"{prefix}.RoundTrip.Start.{card.Name}");

            await MoveAxis(player, Vector2I.Up, $"{prefix}.RoundTrip.UpToEnemy");
            if (player.GetSelectedBoard() != enemyBoard)
            {
                throw new System.InvalidOperationException($"[SelectionSyncTest] Expected enemy board after Up from {card.Name}.");
            }
            Card enemySelected = enemyBoard.GetSelectedCard<Card>(player);
            if (enemySelected is null)
            {
                throw new System.InvalidOperationException("[SelectionSyncTest] No selection on enemy board after switch.");
            }
            if (enemySelected != card.EdgeUp)
            {
                throw new System.InvalidOperationException($"[SelectionSyncTest] Enemy selection mismatch for {card.Name}. Expected {card.EdgeUp.Name}, got {enemySelected.Name}.");
            }

            await MoveAxis(player, Vector2I.Down, $"{prefix}.RoundTrip.DownToOwn");
            if (player.GetSelectedBoard() != board)
            {
                throw new System.InvalidOperationException($"[SelectionSyncTest] Expected own board after Down from {enemySelected.Name}.");
            }
            Card ownSelected = board.GetSelectedCard<Card>(player);
            if (ownSelected is null)
            {
                throw new System.InvalidOperationException("[SelectionSyncTest] No selection on own board after return.");
            }
            if (ownSelected.PositionInBoard != card.PositionInBoard)
            {
                throw new System.InvalidOperationException($"[SelectionSyncTest] Return mismatch for {card.Name}. Expected {card.PositionInBoard}, got {ownSelected.PositionInBoard}.");
            }
        }
    }

    async Task TestMultiSelectorSameBoard(ALPlayer player, string prefix)
    {
        ALPlayer enemyPlayer = matchManager.GetEnemyPlayer();
        if (enemyPlayer is null)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Enemy player is required for multi-selector test.");
        }

        ALBoard board = player.GetPlayerBoard<ALBoard>();
        Card? left = board.FindCardInTree(new Vector2I(0, 0));
        Card? right = board.FindCardInTree(new Vector2I(2, 0));
        Card? middle = board.FindCardInTree(new Vector2I(1, 0));

        if (left is null || right is null || middle is null)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Missing board positions for multi-selector test.");
        }
        if (!left.IsInputSelectable || !right.IsInputSelectable || !middle.IsInputSelectable)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Board positions are not selectable for multi-selector test.");
        }

        GD.Print($"[SelectionSyncTest.MultiSelector] {prefix} Start");
        board.SelectCardField(player, left.PositionInBoard, false);
        board.SelectCardField(enemyPlayer, right.PositionInBoard, false);
        await player.Wait(stepSeconds);
        LogSelectionState($"{prefix}.MultiSelector.Initial");
        SendStepLabel($"{prefix}.MultiSelector.Initial");

        Card ownSelected = board.GetSelectedCard<Card>(player);
        Card enemySelected = board.GetSelectedCard<Card>(enemyPlayer);
        if (ownSelected != left)
        {
            throw new System.InvalidOperationException($"[SelectionSyncTest] Expected own selection {left.Name}, got {ownSelected?.Name ?? "null"}.");
        }
        if (enemySelected != right)
        {
            throw new System.InvalidOperationException($"[SelectionSyncTest] Expected enemy selection {right.Name}, got {enemySelected?.Name ?? "null"}.");
        }

        board.SelectCardField(player, middle.PositionInBoard, false);
        await player.Wait(stepSeconds);
        LogSelectionState($"{prefix}.MultiSelector.MoveOwn");
        SendStepLabel($"{prefix}.MultiSelector.MoveOwn");

        ownSelected = board.GetSelectedCard<Card>(player);
        enemySelected = board.GetSelectedCard<Card>(enemyPlayer);
        if (ownSelected != middle)
        {
            throw new System.InvalidOperationException($"[SelectionSyncTest] Expected own selection {middle.Name}, got {ownSelected?.Name ?? "null"}.");
        }
        if (enemySelected != right)
        {
            throw new System.InvalidOperationException($"[SelectionSyncTest] Expected enemy selection {right.Name}, got {enemySelected?.Name ?? "null"}.");
        }
    }

    void LogSelectionState(string label)
    {
        ALPlayer localPlayer = matchManager.GetControlledPlayer();
        ALPlayer enemyPlayer = matchManager.GetEnemyPlayer();
        if (localPlayer is null || enemyPlayer is null)
        {
            GD.PrintErr($"[SelectionSyncTest.State] {label} missing players");
            return;
        }

        Board localBoard = localPlayer.GetSelectedBoard();
        Board enemyBoard = enemyPlayer.GetSelectedBoard();
        Card localSelected = localBoard?.GetSelectedCard<Card>(localPlayer);
        Card enemySelected = enemyBoard?.GetSelectedCard<Card>(enemyPlayer);

        string localBoardName = localBoard is null ? "null" : localBoard.Name;
        string enemyBoardName = enemyBoard is null ? "null" : enemyBoard.Name;
        string localCardName = localSelected is null ? "null" : localSelected.Name;
        string enemyCardName = enemySelected is null ? "null" : enemySelected.Name;
        Vector2I? localPos = localSelected?.PositionInBoard;
        Vector2I? enemyPos = enemySelected?.PositionInBoard;

        GD.Print($"[SelectionSyncTest.State] {label} local={localPlayer.Name}({localPlayer.MultiplayerId}):{localBoardName}:{localCardName}:{localPos} enemy={enemyPlayer.Name}({enemyPlayer.MultiplayerId}):{enemyBoardName}:{enemyCardName}:{enemyPos}");
    }

    void SendStepLabel(string label)
    {
        if (!isServer)
        {
            return;
        }
        network.SendSelectionSyncTestMessage($"{MessageStepPrefix}{label}");
    }

    async void HandleRemoteMessage(int peerId, string message)
    {
        if (message.StartsWith(MessageStepPrefix))
        {
            string label = message[MessageStepPrefix.Length..];
            await matchManager.Wait(0.2f);
            GD.Print($"[SelectionSyncTest.Remote] From {peerId} label={label}");
            LogSelectionState($"Remote.{label}");
            AssertRemoteEnemyBoardMapping(label);
            return;
        }
        if (message.StartsWith(MessageCommandPrefix))
        {
            string command = message[MessageCommandPrefix.Length..];
            HandleRemoteCommand(peerId, command);
            return;
        }
        GD.PrintErr($"[SelectionSyncTest.Remote] Unknown message '{message}' from {peerId}.");
    }

    void AssertRemoteEnemyBoardMapping(string label)
    {
        if (isServer || label != $"{HostPrefix}.AssertRemoteEnemyBoardOwnBoard00")
        {
            return;
        }

        ALPlayer enemyPlayer = matchManager.GetEnemyPlayer();
        if (enemyPlayer is null)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Enemy player is required for remote mapping assertion.");
        }

        Board enemySelectedBoard = enemyPlayer.GetSelectedBoard();
        Card enemySelected = enemySelectedBoard?.GetSelectedCard<Card>(enemyPlayer);
        if (enemySelectedBoard is null || enemySelected is null)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Missing enemy selection for remote mapping assertion.");
        }
        if (enemySelectedBoard.GetIsEnemyBoard())
        {
            throw new System.InvalidOperationException($"[SelectionSyncTest] Expected enemy selection on own board, got enemyBoard={enemySelectedBoard.GetIsEnemyBoard()}.");
        }
        if (enemySelected.PositionInBoard != Vector2I.Zero)
        {
            throw new System.InvalidOperationException($"[SelectionSyncTest] Expected enemy selection at (0, 0), got {enemySelected.PositionInBoard}.");
        }
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandRemoteMappingAck}");
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
            return;
        }
        if (command == CommandRemoteMappingAck && remoteMappingAck is not null && !remoteMappingAck.Task.IsCompleted)
        {
            remoteMappingAck.TrySetResult(true);
            return;
        }
        if (command == CommandCrossBoardStart)
        {
            _ = RunCrossBoardScenarioLocal();
            return;
        }
        if (command == CommandCrossBoardReady && crossBoardReady is not null && !crossBoardReady.Task.IsCompleted)
        {
            crossBoardReady.TrySetResult(true);
            return;
        }
        if (command == CommandCrossBoardProceed && crossBoardProceed is not null && !crossBoardProceed.Task.IsCompleted)
        {
            crossBoardProceed.TrySetResult(true);
            return;
        }
        if (command == CommandCrossBoardProceed && crossBoardProceed is null)
        {
            crossBoardProceed = new TaskCompletionSource<bool>();
            crossBoardProceed.TrySetResult(true);
            return;
        }
        if (command == CommandCrossBoardBUpDone && crossBoardBUpDone is not null && !crossBoardBUpDone.Task.IsCompleted)
        {
            crossBoardBUpDone.TrySetResult(true);
            return;
        }
        if (command == CommandCrossBoardBDone && crossBoardBDone is not null && !crossBoardBDone.Task.IsCompleted)
        {
            crossBoardBDone.TrySetResult(true);
        }
    }

    async Task RunClientSequence()
    {
        clientSequenceCompletion = new TaskCompletionSource<bool>();
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandRunClient}");
        var timeoutTask = Task.Delay(30000);
        Task completed = await Task.WhenAny(clientSequenceCompletion.Task, timeoutTask);
        if (completed == timeoutTask)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Client sequence timed out.");
        }
    }

    async Task RunClientSequenceLocal()
    {
        if (isRunning)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Client sequence already running.");
        }
        isRunning = true;
        ALPlayer player = matchManager.GetControlledPlayer();
        if (player is null)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Controlled player is required for client run.");
        }
        await player.GetPlayerAsyncHandler().AwaitForCheck(
            null,
            () => matchManager.GetMatchPhase() == EALTurnPhase.Main,
            -1);
        await RunSequence(player, ClientPrefix);
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandClientDone}");
        isRunning = false;
    }

    async Task RunCrossBoardScenarioLocal()
    {
        if (isRunning)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Cross-board scenario already running.");
        }
        isRunning = true;

        ALPlayer player = matchManager.GetControlledPlayer();
        if (player is null)
        {
            throw new System.InvalidOperationException("[SelectionSyncTest] Controlled player is required for cross-board scenario.");
        }
        await EnsureSelectionAt(player, player.GetPlayerBoard<ALBoard>(), Vector2I.Zero);
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandCrossBoardReady}");

        crossBoardProceed ??= new TaskCompletionSource<bool>();
        await AwaitCrossBoardTask(crossBoardProceed, "[SelectionSyncTest] Cross-board scenario proceed wait timed out.");

        await MoveAxis(player, Vector2I.Up, $"{ClientPrefix}.CrossBoard.B.UpToEnemy");
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandCrossBoardBUpDone}");

        await MoveAxis(player, Vector2I.Down, $"{ClientPrefix}.CrossBoard.B.DownToOwn");
        network.SendSelectionSyncTestMessage($"{MessageCommandPrefix}{CommandCrossBoardBDone}");

        isRunning = false;
    }
}
