using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;

public abstract class TestBase
{
    protected readonly float StepSeconds;
    Dictionary<string, ExpectedSelection> expectedSelections = new();
    bool requireExpectedSelections = false;

    protected TestBase(float stepSeconds)
    {
        StepSeconds = stepSeconds <= 0 ? 1f : stepSeconds;
    }

    protected void LoadExpectedSelectionsFromFile(string path, bool requireMatches = false)
    {
        requireExpectedSelections = requireMatches;
        if (string.IsNullOrWhiteSpace(path))
        {
            expectedSelections = new Dictionary<string, ExpectedSelection>();
            return;
        }
        if (!FileAccess.FileExists(path))
        {
            throw new InvalidOperationException($"[TestBase] Expected selections file not found: {path}");
        }
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            var error = FileAccess.GetOpenError();
            throw new InvalidOperationException($"[TestBase] Failed to open expected selections file: {path}. Error: {error}");
        }
        string json = file.GetAsText();
        var selections = JsonConvert.DeserializeObject<Dictionary<string, ExpectedSelection>>(json);
        expectedSelections = selections ?? new Dictionary<string, ExpectedSelection>();
    }

    protected async Task EnsureSelectionAt(ALPlayer player, Board board, Vector2I position, string label)
    {
        board.SelectCardField(player, position);
        player.SelectBoard(player, board);
        await player.Wait(StepSeconds);
        AssertHasSelection(player, board, label);
        AssertExpectedSelection(label, player);
        LogSelection(label, player, board);
    }

    protected async Task MoveAxisUntilBoard(ALPlayer player, Vector2I axis, Board targetBoard, int maxSteps, string label)
    {
        if (maxSteps <= 0)
        {
            throw new InvalidOperationException("[TestBase.MoveAxisUntilBoard] maxSteps must be positive.");
        }

        for (int step = 0; step < maxSteps; step++)
        {
            await MoveAxis(player, axis, $"{label}.Step{step + 1}");
            Board selectedBoard = player.GetSelectedBoard();
            string selectedName = selectedBoard?.Name ?? "null";
            bool selectedEnemy = selectedBoard?.GetIsEnemyBoard() ?? false;
            Card selectedCard = selectedBoard?.GetSelectedCard<Card>(player);
            string selectedCardName = selectedCard is null ? "null" : selectedCard.Name;
            Vector2I? selectedPos = selectedCard?.PositionInBoard;
            string targetName = targetBoard.Name;
            bool targetEnemy = targetBoard.GetIsEnemyBoard();
            GD.Print($"[TestBase.Step] {label} step={step + 1} selected={selectedName} enemy={selectedEnemy} card={selectedCardName} pos={selectedPos} target={targetName} targetEnemy={targetEnemy}");
            if (selectedBoard == targetBoard)
            {
                AssertHasSelection(player, targetBoard, label);
                AssertExpectedSelection(label, player);
                LogSelection(label, player, targetBoard);
                return;
            }
        }

        Fail($"[TestBase] {label} expected board {targetBoard.Name}, got {player.GetSelectedBoard()?.Name ?? "null"} after {maxSteps} steps.");
    }

    protected async Task MoveAxis(ALPlayer player, Vector2I axis, string label)
    {
        Vector2I appliedAxis = player.SimulateAxisInput(axis);
        GD.Print($"[SelectionSyncTest.Step] {label} axis={axis} applied={appliedAxis}");
        await player.Wait(StepSeconds);
        AssertExpectedSelection(label, player);
    }

    protected static void AssertHasSelection(ALPlayer player, Board board, string label)
    {
        Card selected = board.GetSelectedCard<Card>(player);
        if (selected is null)
        {
            Fail($"[TestBase] {label} missing selection on {board.Name}.");
        }
    }

    protected static void LogSelection(string label, ALPlayer player, Board board)
    {
        Card selected = board.GetSelectedCard<Card>(player);
        Vector2I? pos = selected?.PositionInBoard;
        string cardName = selected is null ? "null" : selected.Name;
        GD.Print($"[SelectionSyncTest.State] {label} player={player.Name} board={board.Name} card={cardName} pos={pos}");
    }

    void AssertExpectedSelection(string label, ALPlayer player)
    {
        if (expectedSelections.Count == 0)
        {
            return;
        }

        string normalizedLabel = NormalizeLabel(label);
        if (!expectedSelections.TryGetValue(normalizedLabel, out ExpectedSelection expected))
        {
            if (requireExpectedSelections)
            {
                Fail($"[TestBase] Missing expected selection for '{normalizedLabel}'.");
            }
            return;
        }

        Board selectedBoard = player.GetSelectedBoard();
        if (selectedBoard is null)
        {
            Fail($"[TestBase] {label} expected board {expected.Board}, got null.");
        }
        if (!string.Equals(selectedBoard.Name, expected.Board, StringComparison.Ordinal))
        {
            Fail($"[TestBase] {label} expected board {expected.Board}, got {selectedBoard.Name}.");
        }
        if (selectedBoard.GetIsEnemyBoard() != expected.Enemy)
        {
            Fail($"[TestBase] {label} expected enemy={expected.Enemy}, got enemy={selectedBoard.GetIsEnemyBoard()}.");
        }
        Card selected = selectedBoard.GetSelectedCard<Card>(player);
        if (selected is null)
        {
            Fail($"[TestBase] {label} expected selected card on {selectedBoard.Name}.");
        }
        Vector2I expectedPos = expected.GetPosition();
        if (selected.PositionInBoard != expectedPos)
        {
            Fail($"[TestBase] {label} expected pos={expectedPos}, got {selected.PositionInBoard}.");
        }
    }

    static string NormalizeLabel(string label)
    {
        if (label.StartsWith("Host.", StringComparison.Ordinal))
        {
            label = label[5..];
        }
        else if (label.StartsWith("Client.", StringComparison.Ordinal))
        {
            label = label[7..];
        }
        if (label.StartsWith("Simul.", StringComparison.Ordinal))
        {
            label = label[6..];
        }
        return label;
    }

    protected static void Fail(string message)
    {
        GD.PrintErr(message);
        throw new InvalidOperationException(message);
    }

    sealed class ExpectedSelection
    {
        public string Board { get; set; } = "";
        public bool Enemy { get; set; }
        public int[] Pos { get; set; } = new int[2];

        public Vector2I GetPosition()
        {
            if (Pos.Length != 2)
            {
                throw new InvalidOperationException("[TestBase.ExpectedSelection] Pos must have exactly 2 items.");
            }
            return new Vector2I(Pos[0], Pos[1]);
        }
    }
}
