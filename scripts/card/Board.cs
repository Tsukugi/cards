using Godot;

public partial class Board : Node3D
{
    public delegate void BoardEventHandler();
    public delegate void PositionEventHandler(Vector2I position);
    public event PositionEventHandler OnSelectCardPosition;
    public event BoardEventHandler OnClearSelection;


    protected bool isBoardActive = false; // If false, the board should not use any Input

    protected PackedScene cardTemplate = GD.Load<PackedScene>("scenes/card.tscn");
    protected readonly AxisInputHandler axisInputHandler = new();
    protected readonly ActionInputHandler actionInputHandler = new();
    public PlayState playState = PlayState.Select;
    public Card SelectedCard = null;
    public Vector2I SelectCardPosition = Vector2I.Zero;

    public void DeselectAllCards()
    {
        if (OnClearSelection is null) return;
        OnClearSelection();
    }

    public void SelectCard(Vector2I position)
    {
        DeselectAllCards();
        SelectCardPosition = position;

        if (OnSelectCardPosition is not null) OnSelectCardPosition(position);
    }

    public void SetIsGroupActive(bool value)
    {
        isBoardActive = value;
        if (isBoardActive) SelectCard(SelectCardPosition);
        else DeselectAllCards();
    }
}