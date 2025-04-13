using Godot;

public partial class ALPlayerUI : Control
{
    ALPlayer player;
    Panel selectedCardInfo;
    Label playStateLabel, phaseLabel, selectedCardNameLabel, selectedCardSkillsLabel, selectedCardSupportScopeLabel, selectedCardFactionCountryLabel, selectedCardShipTypeLabel, selectedCardFactionLabel;
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
        selectedCardSkillsLabel = GetNode<Label>("SelectedCardInfo/SkillsPanel/SkillsLabel");
        selectedCardSupportScopeLabel = GetNode<Label>("SelectedCardInfo/SupportScopePanel/SupportScopeLabel");
        selectedCardFactionCountryLabel = GetNode<Label>("SelectedCardInfo/FactionCountryPanel/FactionCountryLabel");
        selectedCardFactionLabel = GetNode<Label>("SelectedCardInfo/FactionPanel/FactionLabel");
        selectedCardShipTypeLabel = GetNode<Label>("SelectedCardInfo/ShipTypePanel/ShipTypeLabel");
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
                selectedCardSkillsLabel.Text = selectedCard.GetFormattedSkills();
                selectedCardSupportScopeLabel.Text = attributes.supportScope;
                selectedCardShipTypeLabel.Text = attributes.type;
                selectedCardFactionCountryLabel.Text = attributes.factionCountry;
                selectedCardFactionLabel.Text = attributes.faction;
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