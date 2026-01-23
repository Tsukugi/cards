using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;

public partial class ALPlayerUI : Control
{
    ALPlayer attachedPlayer;
    ALSelectedCardUI selectedCardUI, triggerCardUI, attackerUI, attackedUI;
    [Export]
    Label playStateLabel, phaseLabel, gameOverLabel, peerIdLabel, invalidOperationLabel;
    Panel gameOverPanel;
    TextureRect selectedCardImage;
    [Export]
    MenuButton matchMenuBtn, debugMenuBtn;

    public override void _Ready()
    {
        base._Ready();
        phaseLabel = GetNode<Label>("PhaseLabel");
        playStateLabel = GetNode<Label>("PlayState");
        invalidOperationLabel = GetNode<Label>("InvalidOperationLabel");
        matchMenuBtn = GetNode<MenuButton>("MatchMenuBtn");
        debugMenuBtn = GetNode<MenuButton>("DebugMenuBtn");
        matchMenuBtn.GetPopup().IndexPressed += OnMatchMenuItemSelected;
        debugMenuBtn.GetPopup().IndexPressed += OnDebugMenuItemSelected;
        selectedCardUI = GetNode<ALSelectedCardUI>("SelectedCardUI");
        triggerCardUI = GetNode<ALSelectedCardUI>("TriggerCardUI");
        attackerUI = GetNode<ALSelectedCardUI>("AttackerUI");
        attackedUI = GetNode<ALSelectedCardUI>("AttackedUI");
        gameOverPanel = GetNode<Panel>("GameOverPanel");
        gameOverLabel = gameOverPanel.GetNode<Label>("GameOverLabel");
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        var matchManager = attachedPlayer.GetMatchManager();
        phaseLabel.Text = attachedPlayer.GetCurrentPhaseText();
        var remotePlayer = matchManager.GetRemotePlayer();
        if (matchManager.IsLocalTurn())
        {
            phaseLabel.Modulate = attachedPlayer.GetPlayerColor();
        }
        else if (remotePlayer is not null)
        {
            phaseLabel.Modulate = remotePlayer.GetPlayerColor();
        }
        playStateLabel.Text = remotePlayer is null
            ? $"{attachedPlayer.GetInteractionState()} - {attachedPlayer.GetInputPlayState()}"
            : $"{attachedPlayer.GetInteractionState()} - {attachedPlayer.GetInputPlayState()} --- {remotePlayer.GetRemoteInputPlayState()} - {remotePlayer.GetRemoteInteractionState()}";

        if (attachedPlayer.GetSelectedBoard().GetSelectedCard<Card>(attachedPlayer) is ALCard selectedCard)
        {
            bool CanShowCardDetailsUI = selectedCard.CanShowCardDetailsUI();
            selectedCardUI.UpdateValues(selectedCard);

            selectedCardUI.Visible = CanShowCardDetailsUI;
        }
        else { selectedCardUI.Visible = false; }
        string playingTurn = matchManager.IsLocalTurn() ? "Playing Turn" : "Enemy Turn";
        peerIdLabel.Text = Multiplayer.GetUniqueId().ToString() + " " + playingTurn;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
    }

    void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs args)
    {
        if (args?.Exception is not InvalidOperationException exception)
        {
            return;
        }
        string message = $"{exception.GetType().Name}: {exception.Message}";
        CallDeferred(nameof(UpdateInvalidOperationLabel), message);
    }

    void UpdateInvalidOperationLabel(string message)
    {
        if (invalidOperationLabel is null)
        {
            throw new InvalidOperationException("[UpdateInvalidOperationLabel] InvalidOperationLabel is missing.");
        }
        invalidOperationLabel.Text = message;
        invalidOperationLabel.Visible = true;
    }

    public async Task ShowGameOverUI(bool isVictory)
    {
        gameOverPanel.Visible = true;
        gameOverLabel.Text = isVictory ? "Victory" : "Defeat";
        await this.Wait(2f);
        gameOverPanel.Visible = false;
    }

    public async Task OnSettleBattleUI(ALCard attacker, ALCard attacked, bool isAttackSuccessful)
    {
        attackerUI.UpdateValues(attacker);
        attackerUI.Visible = true;
        attackedUI.UpdateValues(attacked);
        attackedUI.Visible = true;
        await this.Wait(1f);
        if (isAttackSuccessful) await attackedUI.PlayDamagedAnimation();
        attackerUI.UpdateValues(null);
        attackerUI.Visible = false;
        attackedUI.UpdateValues(null);
        attackedUI.Visible = false;
    }

    public async Task OnEffectTriggerUI(ALCard triggeredCard)
    {
        triggerCardUI.UpdateValues(triggeredCard);
        triggerCardUI.Visible = true;
        await triggerCardUI.PlayEffectAnimation();
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
    public async void OnDebugMenuItemSelected(long itemIndex)
    {
        GD.Print($"[OnDebugMenuItemSelected] index: {itemIndex}");
        var debug = attachedPlayer.GetMatchManager().GetDebug();
        switch (itemIndex)
        {
            case 0:
                debug.ToggleIgnoreCosts();
                debugMenuBtn.GetPopup().SetItemChecked((int)itemIndex, !debug.GetIgnoreCosts());
                break;
            case 1:
                await debug.DrawCard();
                break;
            case 2:
                await debug.DrawCubeCard();
                break;
            case 3:
                await debug.InflictDamage();
                break;
            case 4:
                await debug.TestRetaliation();
                break;
            // Add more cases as needed
            default:
                GD.PrintErr("Unknown item selected");
                break;
        }
    }
    public void SetPlayer(ALPlayer _player) => attachedPlayer = _player;

    public void SyncDebugMenuState()
    {
        if (debugMenuBtn is null) return;
        if (attachedPlayer is null) return;
        var debug = attachedPlayer.GetMatchManager().GetDebug();
        if (debug is null) return;
        debugMenuBtn.GetPopup().SetItemChecked(0, !debug.GetIgnoreCosts());
    }
}
