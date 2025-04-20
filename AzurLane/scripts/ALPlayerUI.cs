using Godot;

public partial class ALPlayerUI : Control
{
    ALPlayer player;
    Panel selectedCardInfo, factionPanel, effectsPanel, supportScopePanel;
    Label playStateLabel, phaseLabel, selectedCardNameLabel, selectedCardEffectsLabel, selectedCardSupportScopeLabel, selectedCardFactionCountryLabel, selectedCardShipTypeLabel, selectedCardFactionLabel;
    TextureRect selectedCardImage;
    [Export]
    MenuButton matchMenuBtn;

    public override void _Ready()
    {
        base._Ready();
        player = this.TryFindParentNodeOfType<ALPlayer>();
        phaseLabel = GetNode<Label>("PhaseLabel");
        selectedCardInfo = GetNode<Panel>("SelectedCardInfo");
        selectedCardImage = GetNode<TextureRect>("SelectedCardInfo/SelectedCardImage");
        selectedCardNameLabel = GetNode<Label>("SelectedCardInfo/NamePanel/NameLabel");
        effectsPanel = GetNode<Panel>("SelectedCardInfo/EffectsPanel");
        selectedCardEffectsLabel = effectsPanel.GetNode<Label>("ScrollContainer/EffectsLabel");
        supportScopePanel = GetNode<Panel>("SelectedCardInfo/SupportScopePanel");
        selectedCardSupportScopeLabel = supportScopePanel.GetNode<Label>("SupportScopeLabel");
        factionPanel = GetNode<Panel>("SelectedCardInfo/FactionPanel");
        selectedCardFactionCountryLabel = factionPanel.GetNode<Label>("FactionCountryLabel");
        selectedCardFactionLabel = factionPanel.GetNode<Label>("FactionLabel");
        selectedCardShipTypeLabel = factionPanel.GetNode<Label>("ShipTypeLabel");
        playStateLabel = GetNode<Label>("PlayState");
        matchMenuBtn = GetNode<MenuButton>("MatchMenuBtn");
        matchMenuBtn.GetPopup().IndexPressed += OnMatchMenuItemSelected;

        //selectedCardInfo.Visible = player.GetIsControllerPlayer(); // Hide it if not the controlled player
        Visible = player.GetIsControllerPlayer();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!player.GetIsControllerPlayer()) return;
        phaseLabel.Text = player.GetCurrentPhaseText();
        phaseLabel.Modulate = player.GetMatchManager().GetPlayerPlayingTurn().GetPlayerColor();
        playStateLabel.Text = $"{player.GetPlayState()} - {player.GetEnemyPlayerBoard<ALBoard>().TryFindParentNodeOfType<ALPlayer>().GetPlayState()}";

        if (player.GetSelectedBoard().GetSelectedCard<ALCard>(player) is ALCard selectedCard)
        {
            bool CanShowCardDetailsUI = selectedCard.CanShowCardDetailsUI();
            selectedCardInfo.Visible = CanShowCardDetailsUI;
            if (CanShowCardDetailsUI)
            {
                ALCardDTO attributes = selectedCard.GetAttributes<ALCardDTO>();
                selectedCardImage.Texture = (Texture2D)selectedCard.GetCardImageResource();
                selectedCardNameLabel.Text = attributes.name;
                selectedCardEffectsLabel.Text = selectedCard.GetFormattedSkills();
                effectsPanel.Visible = selectedCardEffectsLabel.Text.Length > 0;
                selectedCardSupportScopeLabel.Text = attributes.supportScope;
                selectedCardShipTypeLabel.Text = attributes.type;
                selectedCardFactionCountryLabel.Text = attributes.factionCountry;
                selectedCardFactionLabel.Text = attributes.faction;

                if (attributes.type == ALCardType.Event)
                {
                    effectsPanel.Position = new Vector2(effectsPanel.Position.X, 315f);
                    factionPanel.Position = new Vector2(116f, factionPanel.Position.Y);
                    supportScopePanel.Visible = false;
                }
                else
                {
                    supportScopePanel.Visible = true;
                    effectsPanel.Position = new Vector2(effectsPanel.Position.X, 387f);
                    factionPanel.Position = new Vector2(182f, factionPanel.Position.Y);
                }
            }
        }
        else
        {
            selectedCardInfo.Visible = false;
        }
    }

    public void OnMatchMenuItemSelected(long itemIndex)
    {
        GD.Print($"[OnMatchMenuItemSelected] index: {itemIndex}");
        switch (itemIndex)
        {
            case 0:
                this.ChangeScene($"{ALMain.ALSceneRootPath}/main.tscn");
                break;
            // Add more cases as needed
            default:
                GD.PrintErr("Unknown item selected");
                break;
        }
    }
}