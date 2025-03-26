using Godot;

public partial class ALCard : Card
{
    Label3D powerLabel;
    [Export]
    // Useful to differentiate from playable cards - ALTCG Gameplay: Cubes and Durability are resources
    bool isResource = false;

    // AzurLane TCG - Active: active units can attack, inactive units are horizontally placed
    bool isInActiveState = true;

    public override void _Ready()
    {
        base._Ready();
        powerLabel = GetNodeOrNull<Label3D>("CardDisplay/PowerLabel");
        UpdateAttributes<ALCardDTO>(new()); // ! HACK, I Do this to force a ALCardDTO attributes
    }

    protected override void OnFieldStateChangeHandler()
    {
        base.OnFieldStateChangeHandler();
        if (powerLabel is not null) powerLabel.Visible = CanShowPowerLabel();
    }

    bool CanShowPowerLabel() => !IsEmptyField && !isResource && !isDeck;

    public void SetIsInActiveState(bool isActive)
    {
        if (IsEmptyField) return;
        isInActiveState = isActive;
        SetIsSideWays(!isActive);
    }

    public bool GetIsInActiveState() => !IsEmptyField && isInActiveState;

}