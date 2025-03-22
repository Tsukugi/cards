
using Godot;

public partial class ALPlayer : Player
{
    public override void _Ready()
    {
        base._Ready();
        Callable.From(StartGameForPlayer).CallDeferred();
    }

    void StartGameForPlayer()
    {
        SelectBoard(hand);
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.AddCardToHand();
        hand.SelectCard(Vector2I.Zero);
        board.SelectCard(new Vector2I(1, 1));
        SetPlayState(EPlayState.Select);
    }
}
