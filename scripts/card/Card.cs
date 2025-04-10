using Godot;

public partial class Card : CardField
{
    readonly string resourcePath = "res://AzurLane/res/";
    public delegate void OnProvidedCardEvent(Card card);
    protected Node3D cardDisplay;
    protected MeshInstance3D front = null, back = null, side = null, selectedIndicator = null;
    protected Board board;

    Resource cardImage, cardBackImage;

    [Export]
    Color selectedIndicatorColor = new();
    [Export]
    public Card EdgeUp, EdgeDown, EdgeLeft, EdgeRight;

    [Export]
    bool isFaceDown = false;
    [Export]
    bool isSideWays = false;

    CardDTO attributes = new();

    public override void _Ready()
    {
        board = this.TryFindParentNodeOfType<Board>();
        cardDisplay = GetNode<Node3D>("CardDisplay");
        selectedIndicator = GetNodeOrNull<MeshInstance3D>("CardDisplay/SelectedIndicator");
        front = GetNodeOrNull<MeshInstance3D>("CardDisplay/Front");
        back = GetNodeOrNull<MeshInstance3D>("CardDisplay/Back");
        side = GetNodeOrNull<MeshInstance3D>("CardDisplay/Side");
        SetIsFaceDown(isFaceDown);
        SetIsSideWays(isSideWays);
    }


    public override void _Process(double delta)
    {
        OnSelectHandler(isSelected);
        OnCardUpdateHandler();
        cardDisplay.Scale = cardDisplay.Scale.WithY(CardStack);
    }

    void OnSelectHandler(bool isSelected)
    {
        if (selectedIndicator is not null)
        {
            selectedIndicator.Visible = isSelected;
            if (isSelected) UpdateColor(selectedIndicator, selectedIndicatorColor);
        }
    }

    protected virtual void OnCardUpdateHandler()
    {
        if (front is not null) front.Visible = !IsEmptyField;
        if (back is not null) back.Visible = !IsEmptyField;
        if (side is not null) side.Visible = !IsEmptyField;
    }

    public bool GetIsFaceDown() => isFaceDown;
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

    public T GetAttributes<T>() where T : CardDTO => attributes as T;
    public void UpdateAttributes<T>(T newCardDTO) where T : CardDTO
    {
        attributes = newCardDTO;
        if (attributes.imageSrc is not null)
        {
            IsEmptyField = false;
            cardImage = LoadCardImage(newCardDTO.imageSrc);
            UpdateImageTexture(front, (CompressedTexture2D)cardImage);
        }
        if (attributes.backImageSrc is not null)
        {
            IsEmptyField = false;
            cardBackImage = LoadCardImage(newCardDTO.backImageSrc);
            UpdateImageTexture(back, (CompressedTexture2D)cardBackImage);
        }
        //GD.Print($"[Card.UpdateAttributes] {attributes.name}");
    }

    public void DestroyCard()
    {
        IsEmptyField = true;
    }

    Resource LoadCardImage(string path)
    {
        return GD.Load($"{resourcePath}{path}");
    }

    protected static void UpdateImageTexture(MeshInstance3D target, CompressedTexture2D texture)
    {
        var material = target.GetActiveMaterial(0).Duplicate(); // I wanna break the reference on the prefab
        if (material is StandardMaterial3D material3D)
        {
            material3D.AlbedoTexture = texture;
            target.SetSurfaceOverrideMaterial(0, material3D);
        }
    }
    protected static void UpdateColor(MeshInstance3D target, Color color)
    {
        var material = target.GetActiveMaterial(0).Duplicate(); // I wanna break the reference on the prefab
        if (material is StandardMaterial3D material3D)
        {
            material3D.AlbedoColor = color;
            target.SetSurfaceOverrideMaterial(0, material3D);
        }
    }

    public void UpdatePlayerSelectedColor(Player player)
    {
        selectedIndicatorColor = player.GetPlayerColor();
        UpdateColor(selectedIndicator, selectedIndicatorColor);
    }

    public Resource GetCardImageResource() => isFaceDown ? cardBackImage : cardImage;
    public Board GetBoard() => board;
}