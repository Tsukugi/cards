using Godot;

public partial class ALPhaseButton : Card
{
    protected new ALBoard board;
    public override void _Ready()
    {
        base._Ready();
        board = this.TryFindParentNodeOfType<ALBoard>();
        board.OnCardTrigger -= OnCardTriggerHandler;
        board.OnCardTrigger += OnCardTriggerHandler;
    }

    void OnCardTriggerHandler(Card card)
    {
        if (card != this) return;
        GD.Print($"[ALPhaseButton.OnCardTriggerHandler] {card.Name} triggered!");
    }
}