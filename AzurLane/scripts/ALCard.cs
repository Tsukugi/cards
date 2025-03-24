using Godot;

public partial class ALCard : Card
{
    protected new ALCardDTO cardDTO = new();

    // AzurLane TCG - Active: active units can attack, inactive units are horizontally placed
    bool isInActiveState = true;
    public new ALCardDTO GetAttributes() => cardDTO;

    public void UpdateDTO(ALCardDTO newCardDTO)
    {
        cardDTO = newCardDTO;
        if (cardDTO.imageSrc is not null)
        {
            UpdateImageTexture(
                cardDisplay.GetNode<MeshInstance3D>("Front"),
                newCardDTO.imageSrc);
        }
        if (cardDTO.backImageSrc is not null)
        {
            UpdateImageTexture(
                cardDisplay.GetNode<MeshInstance3D>("Back"),
                newCardDTO.backImageSrc);
        }
    }

    public void SetIsInActiveState(bool isActive)
    {
        if (IsEmptyField) return;
        isInActiveState = isActive;
        SetIsSideWays(!isActive);
    }

    public bool GetIsInActiveState() => !IsEmptyField && isInActiveState;

}