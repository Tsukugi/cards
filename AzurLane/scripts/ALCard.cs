using Godot;

public partial class ALCard : Card
{
    protected new ALCardDTO cardDTO = new();

    // AzurLane TCG - Active: active units can attack, inactive units are horizontally placed
    bool isInActiveState = true;
    public new ALCardDTO GetAttributes() => cardDTO;

    public void UpdateDTO(ALCardDTO newCardDTO)
    {
        base.UpdateDTO(newCardDTO);
        cardDTO = newCardDTO;
    }

    public void SetIsInActiveState(bool isActive)
    {
        if (IsEmptyField) return;
        isInActiveState = isActive;
        SetIsSideWays(!isActive);
    }

    public bool GetIsInActiveState() => !IsEmptyField && isInActiveState;

}