using Godot;

public partial class ALCard : Card
{
    public event OnProvidedCardEvent OnDurabilityDamage;
    Node3D UI;
    Label3D powerLabel, stackCount;
    [Export]
    // Useful to differentiate from playable cards - ALTCG Gameplay: Cubes and Durability are resources
    bool isResource = false;

    // AzurLane TCG - Active: active units can attack, inactive units are horizontally placed
    bool isInActiveState = true;

    [Export]
    // AzurLane TCG: Which type of field this card is considered when attacked
    EAttackFieldType attackFieldType = EAttackFieldType.CantAttackHere;
    [Export]
    // AzurLane TCG: If this field/card is a flagship
    bool isFlagship = false;

    public override void _Ready()
    {
        base._Ready();
        UI = GetNodeOrNull<Node3D>("UI");
        powerLabel = GetNodeOrNull<Label3D>("UI/PowerLabel");
        stackCount = GetNodeOrNull<Label3D>("UI/StackCount");
        UpdateAttributes<ALCardDTO>(new()); // ! HACK, I Do this to force a ALCardDTO attributes
    }

    protected override void OnCardUpdateHandler()
    {
        base.OnCardUpdateHandler();
        if (UI is not null)
        {
            // This rotates the Card UI to be seen from an inverted board (enemyBoard)
            if (!board.GetIsEnemyBoard()) UI.RotationDegrees = UI.RotationDegrees.WithY(0);
            else UI.RotationDegrees = UI.RotationDegrees.WithY(180);
        }

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

    // --- API ---

    public bool CanShowStackCount() => !IsEmptyField && CardStack > 1;
    public bool CanShowCardDetailsUI() => !IsEmptyField && !isDeck && !(isFlagship && GetIsFaceDown()) && !GetIsFaceDown();
    public bool CanShowPowerLabel() => !IsEmptyField && !isResource && !isDeck;

    public void SetIsInActiveState(bool isActive)
    {
        if (IsEmptyField) return;
        isInActiveState = isActive;
        SetIsSideWays(!isActive);
    }

    public bool GetIsInActiveState() => !IsEmptyField && isInActiveState;
    public bool CanBeAttacked(EAttackFieldType attackerType)
    {
        switch (attackerType)
        {
            case EAttackFieldType.CantAttackHere: GD.PushError("[CanBeAttacked] A non attacker card is trying to start an attack"); return false;
            case EAttackFieldType.BackRow: return attackFieldType == EAttackFieldType.FrontRow; // A backAttacker only can attack front row
            case EAttackFieldType.FrontRow: return true; // A frontAttacker can attack everyone
            default: return false;
        }
    }
    public EAttackFieldType GetAttackFieldType() => attackFieldType;
    public bool GetIsAFlagship() => isFlagship;

    public void TakeDurabilityDamage()
    {
        if (OnDurabilityDamage is not null) OnDurabilityDamage(this);
    }
}

public enum EAttackFieldType
{
    CantAttackHere,
    BackRow,
    FrontRow
}