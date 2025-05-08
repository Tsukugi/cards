using Godot;
using System;
using System.Text;

public partial class ALSelectedCardUI : Panel
{
    Panel factionPanel, effectsPanel, supportScopePanel;
    Label powerLabel, selectedCardNameLabel, selectedCardEffectsLabel, selectedCardSupportScopeLabel, selectedCardFactionCountryLabel, selectedCardShipTypeLabel, selectedCardFactionLabel;
    TextureRect selectedCardImage;
    ALCard boundCard = null;
    AnimationPlayer animationPlayer;

    Vector2 effectOriginalPosition, factionOriginalPosition;

    public override void _Ready()
    {
        base._Ready();
        powerLabel = GetNode<Label>("PowerLabel");
        selectedCardImage = GetNode<TextureRect>("SelectedCardImage");
        selectedCardNameLabel = GetNode<Label>("NamePanel/NameLabel");
        effectsPanel = GetNode<Panel>("EffectsPanel");
        selectedCardEffectsLabel = effectsPanel.GetNode<Label>("ScrollContainer/EffectsLabel");
        supportScopePanel = GetNode<Panel>("SupportScopePanel");
        selectedCardSupportScopeLabel = supportScopePanel.GetNode<Label>("SupportScopeLabel");
        factionPanel = GetNode<Panel>("FactionPanel");
        selectedCardFactionCountryLabel = factionPanel.GetNode<Label>("FactionCountryLabel");
        selectedCardFactionLabel = factionPanel.GetNode<Label>("FactionLabel");
        selectedCardShipTypeLabel = factionPanel.GetNode<Label>("ShipTypeLabel");
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        effectOriginalPosition = effectsPanel.Position;
        factionOriginalPosition = factionPanel.Position;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        UpdateUINodes(boundCard);
    }

    public void UpdateUINodes(ALCard boundCard)
    {
        if (boundCard is null) return;
        // GD.Print($"[UpdateUINodes] {boundCard.Name}");
        ALCardDTO attributes = boundCard.GetAttributes<ALCardDTO>();
        selectedCardImage.Texture = (Texture2D)boundCard.GetCardImageResource();
        selectedCardNameLabel.Text = attributes.name;
        selectedCardEffectsLabel.Text = boundCard.GetFormattedEffect();
        selectedCardSupportScopeLabel.Text = attributes.supportScope;
        selectedCardShipTypeLabel.Text = attributes.shipType;
        selectedCardFactionCountryLabel.Text = LoggingUtils.ArrayToString(attributes.factionCountry, "-", false);
        selectedCardFactionLabel.Text = attributes.faction;

        effectsPanel.Visible = selectedCardEffectsLabel.Text.Length > 0;

        bool isNotShipCard = attributes.cardType == ALCardType.Event || attributes.cardType == ALCardType.Cube;
        supportScopePanel.Visible = !isNotShipCard;
        powerLabel.Visible = !isNotShipCard;
        if (isNotShipCard)
        {
            effectsPanel.Position = new Vector2(effectOriginalPosition.X, effectOriginalPosition.Y - 75f);
            factionPanel.Position = new Vector2(factionOriginalPosition.X - 80f, factionOriginalPosition.Y);
        }
        else
        {
            effectsPanel.Position = effectOriginalPosition;
            factionPanel.Position = factionOriginalPosition;
            powerLabel.Text = boundCard.GetAttributeWithModifiers<ALCardDTO>("Power").ToString();
        }
    }

    public void PlayEffectAnimation()
    {
        animationPlayer.Play("Show");
    }

    public void UpdateValues(ALCard? card)
    {
        boundCard = card;
        UpdateUINodes(boundCard);
    }

}
