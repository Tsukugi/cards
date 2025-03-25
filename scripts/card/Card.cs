using Godot;

public partial class Card : CardField
{
    readonly string resourcePath = "res://AzurLane/res/";
    public delegate void OnProvidedCardEvent(Card card);
    protected Node3D cardDisplay, selectedIndicator = null, front = null, back = null, side = null;
    protected Board board;

    [Export]
    bool isFaceDown = false;
    [Export]
    bool isSideWays = false;

    protected CardDTO cardDTO = new();

    public override void _Ready()
    {
        board = this.TryFindParentNodeOfType<Board>();
        board.OnClearSelection -= OnUnselectCardHandler;
        board.OnClearSelection += OnUnselectCardHandler;
        board.OnSelectCardPosition -= OnSelectCardPositionHandler;
        board.OnSelectCardPosition += OnSelectCardPositionHandler;
        cardDisplay = GetNode<Node3D>("CardDisplay");
        selectedIndicator = GetNodeOrNull<Node3D>("CardDisplay/SelectedIndicator");
        front = GetNodeOrNull<Node3D>("CardDisplay/Front");
        back = GetNodeOrNull<Node3D>("CardDisplay/Back");
        side = GetNodeOrNull<Node3D>("CardDisplay/Side");
        SetIsFaceDown(isFaceDown);
        SetIsSideWays(isSideWays);
    }

    void OnSelectCardPositionHandler(Vector2I position, OnProvidedCardEvent cardCallback)
    {
        bool isSelectingThisCard = position == PositionInBoard;
        SetIsSelected(isSelectingThisCard);
        if (isSelectingThisCard)
        {
            // GD.Print($"[OnSelectCardPositionHandler] Card {Name} is active");
            cardCallback(this);
        }
    }
    void OnUnselectCardHandler()
    {
        SetIsSelected(false);
    }

    public override void _Process(double delta)
    {
        OnSelectHandler(isSelected);
        OnFieldStateChangeHandler();
        cardDisplay.Scale = cardDisplay.Scale.WithY(CardStack);
    }


    void OnSelectHandler(bool isSelected)
    {
        if (selectedIndicator is not null) selectedIndicator.Visible = isSelected && board.IsBoardActive;
    }

    void OnFieldStateChangeHandler()
    {
        if (front is not null) front.Visible = !IsEmptyField;
        if (back is not null) back.Visible = !IsEmptyField;
        if (side is not null) side.Visible = !IsEmptyField;
    }

    public void SetIsFaceDown(bool value)
    {
        isFaceDown = value;
        if (isFaceDown) cardDisplay.RotationDegrees = cardDisplay.RotationDegrees.WithZ(180);
        else cardDisplay.RotationDegrees = cardDisplay.RotationDegrees.WithZ(0);
    }

    public void SetIsSideWays(bool value)
    {
        isSideWays = value;
        if (isSideWays) cardDisplay.RotationDegrees = cardDisplay.RotationDegrees.WithY(90);
        else cardDisplay.RotationDegrees = cardDisplay.RotationDegrees.WithY(0);
    }

    public void UpdateAttributes(CardDTO newCardDTO)
    {
        cardDTO = newCardDTO;
        if (cardDTO.imageSrc is not null)
        {
            IsEmptyField = false;
            UpdateImageTexture(
                cardDisplay.GetNode<MeshInstance3D>("Front"),
                newCardDTO.imageSrc);
        }
        if (cardDTO.backImageSrc is not null)
        {
            IsEmptyField = false;
            UpdateImageTexture(
                cardDisplay.GetNode<MeshInstance3D>("Back"),
                newCardDTO.backImageSrc);
        }
    }


    protected void UpdateImageTexture(MeshInstance3D target, string path)
    {
        var material = target.GetActiveMaterial(0).Duplicate(); // I wanna break the reference on the prefab
        if (material is StandardMaterial3D material3D)
        {
            var texture = GD.Load($"{resourcePath}{path}");
            material3D.AlbedoTexture = (CompressedTexture2D)texture;
            target.SetSurfaceOverrideMaterial(0, material3D);
        }
    }

    public CardDTO GetAttributes() => cardDTO;
}