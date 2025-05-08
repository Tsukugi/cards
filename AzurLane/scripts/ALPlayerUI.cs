using System.Threading.Tasks;
using Godot;

public partial class ALPlayerUI : Control
{
    ALPlayer attachedPlayer;
    ALSelectedCardUI selectedCardUI, triggerCardUI, attackerUI, attackedUI;
    Label playStateLabel, phaseLabel;
    TextureRect selectedCardImage;
    [Export]
    MenuButton matchMenuBtn, debugMenuBtn;

    public override void _Ready()
    {
        base._Ready();
        phaseLabel = GetNode<Label>("PhaseLabel");
        playStateLabel = GetNode<Label>("PlayState");
        matchMenuBtn = GetNode<MenuButton>("MatchMenuBtn");
        debugMenuBtn = GetNode<MenuButton>("DebugMenuBtn");
        matchMenuBtn.GetPopup().IndexPressed += OnMatchMenuItemSelected;
        debugMenuBtn.GetPopup().IndexPressed += OnDebugMenuItemSelected;
        selectedCardUI = GetNode<ALSelectedCardUI>("SelectedCardUI");
        triggerCardUI = GetNode<ALSelectedCardUI>("TriggerCardUI");
        attackerUI = GetNode<ALSelectedCardUI>("AttackerUI");
        attackedUI = GetNode<ALSelectedCardUI>("AttackedUI");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        phaseLabel.Text = attachedPlayer.GetCurrentPhaseText();
        phaseLabel.Modulate = attachedPlayer.GetMatchManager().GetPlayerPlayingTurn().GetPlayerColor();
        playStateLabel.Text = $"{attachedPlayer.GetPlayState()} - {attachedPlayer.GetEnemyPlayerBoard<ALBoard>().TryFindParentNodeOfType<ALPlayer>().GetPlayState()}";

        if (attachedPlayer.GetSelectedBoard().GetSelectedCard<ALCard>(attachedPlayer) is ALCard selectedCard)
        {
            bool CanShowCardDetailsUI = selectedCard.CanShowCardDetailsUI();
            selectedCardUI.UpdateValues(selectedCard);

            selectedCardUI.Visible = CanShowCardDetailsUI;
        }
        else { selectedCardUI.Visible = false; }
    }

    public async Task OnSettleBattleUI(ALCard attacker, ALCard attacked)
    {
        attackerUI.UpdateValues(attacker);
        attackerUI.Visible = true;
        attackedUI.UpdateValues(attacked);
        attackedUI.Visible = true;
        await this.Wait(2f);
        attackerUI.UpdateValues(null);
        attackerUI.Visible = false;
        attackedUI.UpdateValues(null);
        attackedUI.Visible = false;
    }

    public async Task OnEffectTriggerUI(ALCard triggeredCard)
    {
        triggerCardUI.UpdateValues(triggeredCard);
        triggerCardUI.Visible = true;
        triggerCardUI.PlayEffectAnimation();
        await this.Wait(2f); // TODO make animation awaitable
        triggerCardUI.UpdateValues(null);
        triggerCardUI.Visible = false;
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
    public void OnDebugMenuItemSelected(long itemIndex)
    {
        GD.Print($"[OnDebugMenuItemSelected] index: {itemIndex}");
        switch (itemIndex)
        {
            case 0:
                attachedPlayer.GetMatchManager().GetDebug().ToggleIgnoreCosts();
                break;
            case 1:
                attachedPlayer.GetMatchManager().GetDebug().DrawCard();
                break;
            case 2:
                attachedPlayer.GetMatchManager().GetDebug().DrawCubeCard();
                break;
            case 3:
                attachedPlayer.GetMatchManager().GetDebug().InflictDamage();
                break;
            // Add more cases as needed
            default:
                GD.PrintErr("Unknown item selected");
                break;
        }
    }
    public void SetPlayer(ALPlayer _player) => attachedPlayer = _player;
}