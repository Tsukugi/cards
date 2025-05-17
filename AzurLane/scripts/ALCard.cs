using System.Text;
using Godot;

public partial class ALCard : Card
{
    public event OnProvidedCardEvent OnCardActiveStateUpdate;
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
    bool isFlagshipField = false;
    public override void _Ready()
    {
        base._Ready();
        UI = GetNodeOrNull<Node3D>("UI");
        powerLabel = GetNodeOrNull<Label3D>("UI/PowerLabel");
        stackCount = GetNodeOrNull<Label3D>("UI/StackCount");
        nameLabel = GetNodeOrNull<Label3D>("UI/Name");
        skillsLabel = GetNodeOrNull<Label3D>("UI/Skills");
        skillsBackdrop = GetNodeOrNull<Node3D>("UI/SkillsBackdrop");
        UpdateAttributes<ALCardDTO>(new()); // I Do this to force a ALCardDTO attributes
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
                var shipPower = GetAttributeWithModifiers<ALCardDTO>("Power");
                float sizeMod = 1f + ((shipPower - attrs.power) / 300f); // if pow 400 - modPow 600 => 1 + 200/300 => scale is 1.6
                powerLabel.FontSize = (int)(120 * sizeMod);
                powerLabel.Text = shipPower.ToString();
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
            if (isShown) skillsLabel.Text = GetFormattedEffectMini();
        }
    }

    // --- API ---
    public bool CanShowStackCount() => !GetIsEmptyField() && CardStack > 1;
    public bool CanShowCardDetailsUI() => !GetIsEmptyField() && !isDeck && !(GetIsAFlagship() && GetIsFaceDown()) && !GetIsFaceDown();
    public bool CanShowPowerLabel() => IsCardUnit();
    public bool IsCardUnit()
    {
        var attrs = GetAttributes<ALCardDTO>();
        return !GetIsEmptyField() && !isResource && !isDeck
           && (attrs.cardType == ALCardType.Ship || GetIsAFlagship()); // Refers to a placed card that is a ship or flagship
    }
    public string GetFormattedEffectMini()
    // TODO : Add colors for duration and condition
    {
        CardEffectDTO[] effects = GetAttributes<ALCardDTO>().effects;
        StringBuilder stringBuilder = new();
        foreach (var effect in effects)
        {
            string formattedEffects = $"■ ";
            stringBuilder.AppendLine($"{formattedEffects}{effect.effectLabel}");
        }
        return stringBuilder.ToString();
    }
    public string GetFormattedEffect()
    // TODO : Add colors for duration and condition
    {
        CardEffectDTO[] effects = GetAttributes<ALCardDTO>().effects;
        StringBuilder stringBuilder = new();
        foreach (var effect in effects)
        {

            string formattedEffects = $"■";
            if (effect.triggerEvent.Length > 0) formattedEffects += $"({LoggingUtils.ArrayToString(effect.triggerEvent)}) - ";
            if (effect.duration is not null) formattedEffects += $"[{effect.duration}] - ";
            if (effect.condition.Length > 0)
            {
                formattedEffects += "[";
                for (int i = 0; i < effect.condition.Length; i++)
                {
                    formattedEffects += $"{effect.condition[i].conditionId}";
                    if (effect.condition[i].conditionArgs.Length > 0)
                    {
                        formattedEffects += $" ({LoggingUtils.ArrayToString(effect.condition[i].conditionArgs)})";
                    }
                }
                formattedEffects += "] - ";
            }
            if (effect.effectId is not null)
            {
                formattedEffects += $"[{effect.effectId}";
                if (effect.effectValue.Length > 0) formattedEffects += $" ({LoggingUtils.ArrayToString(effect.effectValue)})";
                formattedEffects += $"] - ";
            }
            stringBuilder.AppendLine($"{formattedEffects}{effect.effectLabel}");
        }
        return stringBuilder.ToString();
    }
    public void SetIsInActiveState(bool isActive)
    {
        isInActiveState = isActive;
        SetIsSideWays(!isActive);
    }

    public bool GetIsInActiveState() => !GetIsEmptyField() && isInActiveState;
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
    public bool GetIsAFlagship() => isFlagshipField && (GetAttributes<ALCardDTO>().cardType == ALCardType.Flagship || GetAttributes<ALCardDTO>().cardType == ALCardType.FlagshipAwakened);
    public void TakeDurabilityDamage()
    {
        if (OnDurabilityDamage is not null) OnDurabilityDamage(this);
    }
    public override void UpdateAttributes<T>(T newCardDTO)
    {
        var player = GetOwnerPlayer<ALPlayer>();
        base.UpdateAttributes<T>(newCardDTO);
        SetEffectManager(new ALEffectManager(this, activeStatusEffects, player, player.GetMatchManager()));
        //GD.Print($"[Card.UpdateAttributes] {attributes.name}");
    }
    public bool CanBattleSupportCard(ALCard target)
    {
        // This checks for a limitation in some attack areas 
        // Structure in the database is ["LimitBattleSupport", "BackRow"] 
        var statusEffect = GetEffectManager<ALEffectManager>().TryGetStatusEffect(ALCardStatusEffects.LimitBattleSupport);
        bool isLimitedToSupport =
            statusEffect is CardEffectDTO matchingEffect
            && matchingEffect.effectValue[1] == target.GetAttackFieldType().ToString();
        GD.Print($"[CanBattleSupportCard] {isLimitedToSupport}");
        return !isLimitedToSupport;
    }
}

public enum EAttackFieldType
{
    CantAttackHere,
    BackRow,
    FrontRow
}