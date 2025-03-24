using Godot;

public partial class Card : CardField
{
    readonly string resourcePath = "res://AzurLane/res/";
    public delegate void OnProvidedCardEvent(Card card);
    protected Node3D cardDisplay, selectedIndicator;
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
        selectedIndicator = GetNode<Node3D>("SelectedIndicator");
        cardDisplay = GetNode<Node3D>("CardDisplay");
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
    }


    void OnSelectHandler(bool isSelected)
    {
        selectedIndicator.Visible = isSelected && board.IsBoardActive;
    }

    void OnFieldStateChangeHandler()
    {
        cardDisplay.Visible = !IsEmptyField;
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
        if (isSideWays) RotationDegrees = RotationDegrees.WithY(90);
        else RotationDegrees = RotationDegrees.WithY(0);
    }

    public void UpdateDTO(CardDTO newCardDTO)
    {
        cardDTO = newCardDTO;
        if (cardDTO.imageSrc is not null)
        {
            UpdateImageTexture(
                cardDisplay.GetNode<MeshInstance3D>("Front"),
                newCardDTO.imageSrc);
        }
        if (cardDTO.backImageSrc is not null)
        {
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