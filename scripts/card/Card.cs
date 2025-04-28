using System.Collections.Generic;
using Godot;

public partial class Card : CardField
{
    readonly string resourcePath = "res://AzurLane/res/";
    public delegate void OnProvidedCardEvent(Card card);
    public event OnProvidedCardFieldEvent OnCardIsSidewaysUpdate;
    protected Node3D cardDisplay;
    protected MeshInstance3D front = null, back = null, side = null, selectedIndicator = null;
    protected Board board;
    EffectManager effect;
    Player ownerPlayer;

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
    protected readonly List<CardEffectDTO> activeStatusEffects = []; // StatusEffectId is effectValue[0]
    readonly List<AttributeModifier> activeModifiers = []; // <attributeName, value>
    public override void _Ready()
    {
        ownerPlayer = this.TryFindParentNodeOfType<Player>();
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
        bool isEmptyField = GetIsEmptyField();
        if (front is not null) front.Visible = !isEmptyField;
        if (back is not null) back.Visible = !isEmptyField;
        if (side is not null) side.Visible = !isEmptyField;
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
        if (material is ShaderMaterial material3D)
        {
            material3D.SetShaderParameter("color", color);
            target.SetSurfaceOverrideMaterial(0, material3D);
        }
    }

    // --- API ---

    public void AddModifier(AttributeModifier modifier)
    {
        GD.Print($"[AddModifier] {Name} {modifier.AttributeName} {modifier.Amount} {modifier.Duration}");
        activeModifiers.Add(modifier);
    }
    public void RemoveModifier(AttributeModifier modifier)
    {
        GD.Print($"[RemoveModifier] {Name} {modifier.AttributeName} {modifier.Amount} {modifier.Duration}");
        activeModifiers.Remove(modifier);
    }
    public virtual void TryToExpireEffectOrModifier(string duration)
    {
        GD.Print($"[TryToExpireModifier] {duration}");
        var modifiers = activeModifiers.FindAll(modifier => modifier.Duration.ToString() == duration);
        modifiers.ForEach(RemoveModifier);
        var statusEffects = activeStatusEffects.FindAll(modifier => modifier.duration.ToString() == duration);
        statusEffects.ForEach(effect => activeStatusEffects.Remove(effect));
    }
    public virtual void TryToTriggerCardEffect(string triggerEvent)
    {
        if (effect is null) { GD.PrintErr($"[TryToTriggerCardEffect] Cannot trigger effects with a card that doesn't have an Effect instance"); return; }
        // GD.Print($"[TryToTriggerCardEffect] {triggerEvent}");
        _ = effect.TryToApplyEffects(triggerEvent);
    }

    public T GetEffectManager<T>() where T : EffectManager => effect as T;

    public int GetAttributeWithModifiers<T>(string attributeName) where T : CardDTO
    {
        T attrs = GetAttributes<T>();

        int attribute = attrs.GetPropertyValue<int>(attributeName);
        foreach (AttributeModifier modifier in activeModifiers)
        {
            attribute += modifier.Amount;
            // GD.Print($"[GetAttributeWithModifiers] {attribute} {modifier.Amount}");
        }
        return attribute;
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
        if (OnCardIsSidewaysUpdate is not null) OnCardIsSidewaysUpdate(this);
    }

    public T GetAttributes<T>() where T : CardDTO
    {
        if (attributes is null) GD.PushError($"[GetAttributes] No attributes are set");
        return attributes as T;
    }
    public virtual void UpdateAttributes<T>(T newCardDTO) where T : CardDTO
    {
        attributes = newCardDTO;
        if (attributes.imageSrc is not null)
        {
            SetIsEmptyField(false);
            cardImage = LoadCardImage(newCardDTO.imageSrc);
            UpdateImageTexture(front, (CompressedTexture2D)cardImage);
        }
        if (attributes.backImageSrc is not null)
        {
            SetIsEmptyField(false);
            cardBackImage = LoadCardImage(newCardDTO.backImageSrc);
            UpdateImageTexture(back, (CompressedTexture2D)cardBackImage);
        }
        effect = new(this, activeStatusEffects, ownerPlayer);
        //GD.Print($"[Card.UpdateAttributes] {attributes.name}");
    }

    public void SetEffectManager(EffectManager newEffect)
    {
        GD.Print($"[SetEffectManager] {GetAttributes<CardDTO>().name}");
        effect = newEffect;
    }

    public void DestroyCard()
    {
        SetIsEmptyField(true);
    }

    public void UpdatePlayerSelectedColor(Player player)
    {
        selectedIndicatorColor = player.GetPlayerColor();
        UpdateColor(selectedIndicator, selectedIndicatorColor);
    }

    public Resource GetCardImageResource() => isFaceDown ? cardBackImage : cardImage;
    public Board GetBoard() => board;
    public List<AttributeModifier> GetModifiers() => activeModifiers;
    public void SetOwnerPlayer(Player player) => ownerPlayer = player;
    public T GetOwnerPlayer<T>() where T : Player => ownerPlayer as T;
}