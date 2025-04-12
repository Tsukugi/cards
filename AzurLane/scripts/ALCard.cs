using System.Text;
using Godot;

public partial class ALCard : Card
{
    public event OnProvidedCardEvent OnDurabilityDamage;
    Node3D UI, skillsBackdrop;
    Label3D powerLabel, stackCount, nameLabel, skillsLabel;
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
        nameLabel = GetNodeOrNull<Label3D>("UI/Name");
        skillsLabel = GetNodeOrNull<Label3D>("UI/Skills");
        skillsBackdrop = GetNodeOrNull<Node3D>("UI/SkillsBackdrop");
        UpdateAttributes<ALCardDTO>(new()); // ! HACK, I Do this to force a ALCardDTO attributes
    }

    protected override void OnCardUpdateHandler()
    {
        base.OnCardUpdateHandler();
        ALCardDTO attrs = GetAttributes<ALCardDTO>();
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
                powerLabel.Text = attrs.power.ToString();
            }
        }

        if (stackCount is not null)
        {
            bool isShown = CanShowStackCount();
            stackCount.Visible = isShown;
            if (isShown) stackCount.Text = CardStack.ToString();
        }

        if (nameLabel is not null)
        {
            bool isShown = CanShowCardDetailsUI() && GetBoard().GetType() == typeof(ALHand);
            nameLabel.Visible = isShown;
            if (isShown) nameLabel.Text = attrs.name;
        }

        if (skillsLabel is not null)
        {
            bool isShown = CanShowCardDetailsUI() && GetBoard().GetType() == typeof(ALHand);
            skillsLabel.Visible = isShown;
            skillsBackdrop.Visible = isShown;
            if (isShown) skillsLabel.Text = GetFormattedSkills();
        }
    }

    // --- API ---
    public bool CanShowStackCount() => !IsEmptyField && CardStack > 1;
    public bool CanShowCardDetailsUI() => !IsEmptyField && !isDeck && !(isFlagship && GetIsFaceDown()) && !GetIsFaceDown();
    public bool CanShowPowerLabel() => !IsEmptyField && !isResource && !isDeck;

    public string GetFormattedSkills()
    //TODO : Add colors for duration and condition
    {
        ALCardSkillDTO[] attributes = GetAttributes<ALCardDTO>().skills;
        StringBuilder stringBuilder = new();
        foreach (var item in attributes)
        {
            var formattedSkils = "";
            formattedSkils += $"[{item.duration}] - ";
            if (item.effectId is not null) formattedSkils += $"[{item.effectId}] - ";
            if (item.condition is ALCardSkillConditionDTO[] conditions && conditions.Length > 0)
            {
                formattedSkils += "[";
                for (int i = 0; i < conditions.Length; i++)
                {
                    formattedSkils += $"{conditions[i].conditionId}";
                    if (conditions[i].conditionAmount is string amount) formattedSkils += $" ({amount})";
                    if (conditions[i].conditionCard is string card) formattedSkils += $" ({card})";
                    if (i != conditions.Length - 1) formattedSkils += " - ";
                }
                formattedSkils += "] - ";
            }
            stringBuilder.AppendLine($"{formattedSkils}{item.effectLabel}");
        }
        return stringBuilder.ToString();
    }
    public void SetIsInActiveState(bool isActive)
    {
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
    public EAttackFieldType SetAttackFieldType(EAttackFieldType value) => attackFieldType = value;
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