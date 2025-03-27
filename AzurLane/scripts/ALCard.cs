using Godot;

public partial class ALCard : Card
{
    Label3D powerLabel, stackCount;
    [Export]
    // Useful to differentiate from playable cards - ALTCG Gameplay: Cubes and Durability are resources
    bool isResource = false;

    // AzurLane TCG - Active: active units can attack, inactive units are horizontally placed
    bool isInActiveState = true;

    public override void _Ready()
    {
        base._Ready();
        powerLabel = GetNodeOrNull<Label3D>("UI/PowerLabel");
        stackCount = GetNodeOrNull<Label3D>("UI/StackCount");
        UpdateAttributes<ALCardDTO>(new()); // ! HACK, I Do this to force a ALCardDTO attributes
    }

    protected override void OnFieldStateChangeHandler()
    {
        base.OnFieldStateChangeHandler();
        if (powerLabel is not null)
        {
            bool isShown = CanShowPowerLabel();
            powerLabel.Visible = isShown;
            if (isShown)
            {
                var attrs = GetAttributes<ALCardDTO>();
                powerLabel.Text = attrs.power.ToString();
            }
        }

        if (stackCount is not null)
        {
            bool isShown = CanShowStackCount();
            stackCount.Visible = isShown;
            if (isShown) stackCount.Text = CardStack.ToString();
        }
    }

    public bool CanShowStackCount() => !IsEmptyField && CardStack > 1;
    public bool CanShowCardDetailsUI() => !IsEmptyField && !isDeck && !(GetAttributes<ALCardDTO>().type != "Flagship" && GetIsFaceDown());
    public bool CanShowPowerLabel() => !IsEmptyField && !isResource && !isDeck;

    public void SetIsInActiveState(bool isActive)
    {
        if (IsEmptyField) return;
        isInActiveState = isActive;
        SetIsSideWays(!isActive);
    }

    public bool GetIsInActiveState() => !IsEmptyField && isInActiveState;

}