using Godot;
using System;

public partial class ALSelectedCardUI : Panel
{
    Panel factionPanel, effectsPanel, supportScopePanel;
    Label powerLabel, selectedCardNameLabel, selectedCardEffectsLabel, selectedCardSupportScopeLabel, selectedCardFactionCountryLabel, selectedCardShipTypeLabel, selectedCardFactionLabel;
    TextureRect selectedCardImage;
    ALCard boundCard = null;

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
        selectedCardShipTypeLabel.Text = attributes.type;
        selectedCardFactionCountryLabel.Text = attributes.factionCountry;
        selectedCardFactionLabel.Text = attributes.faction;

        effectsPanel.Visible = selectedCardEffectsLabel.Text.Length > 0;

        bool isEventCard = attributes.type == ALCardType.Event;
        supportScopePanel.Visible = !isEventCard;
        powerLabel.Visible = !isEventCard;
        if (isEventCard)
        {
            effectsPanel.Position = new Vector2(effectsPanel.Position.X, 315f);
            factionPanel.Position = new Vector2(116f, factionPanel.Position.Y);
        }
        else
        {
            effectsPanel.Position = new Vector2(effectsPanel.Position.X, 387f);
            factionPanel.Position = new Vector2(182f, factionPanel.Position.Y);
            powerLabel.Text = boundCard.GetAttributeWithModifiers<ALCardDTO>("Power").ToString();
        }
    }

    public void UpdateValues(ALCard? card)
    {
        boundCard = card;
        UpdateUINodes(boundCard);
    }

}
