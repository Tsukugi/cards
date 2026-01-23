
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ALPlayer : Player
{
    // --- Params ---
    float DrawDelay = 0.2f;

    // --- Events ---
    public event ProvideCardInteractionEvent OnAttackStart;
    public event ProvideCardInteractionEvent OnAttackTargetAdquired;
    public event InteractionEvent OnAttackEnd;
    public event EnemyInteractionRequestEvent OnAttackGuardStart;
    public event InteractionEvent OnAttackGuardEnd;
    public event ProvideCardInteractionEvent OnGuardProvided;
    public event ProvideCardInteractionEvent OnRetaliation;
    public event InteractionEvent OnRetaliationCancel;
    public event InteractionEvent OnGameOver;
    public event Action OnTurnEnd;

    // --- Refs ---
    ALDeckSet deckSet = null;
    ALDeckSet enemyDeckSet = null;
    readonly List<ALCardDTO> enemyHandCards = [];
    ALGameMatchManager matchManager;
    // --- AI ---
    ALBasicAI ai;

    // --- Nodes --- 
    Node3D unitsArea;
    [Export]
    ALCard deckField, cubeDeckField, flagshipField, retreatField;
    [Export]
    ALPhaseButton phaseButtonField;
    ALBoardArea costArea, durabilityArea;
    Node3D enemyUnitsArea;
    ALBoardArea enemyCostArea, enemyDurabilityArea;
    ALCard enemyDeckField, enemyCubeDeckField, enemyFlagshipField, enemyRetreatField;
    ALPhaseButton enemyPhaseButtonField;

    // --- Phase ---
    AsyncHandler playerAsyncHandler;
    ALPhase phaseManager;

    // --- Actions ---

    public override void _Ready()
    {
        base._Ready(); // Required for board/hand refs
        matchManager = this.TryFindParentNodeOfType<ALGameMatchManager>();

        playerAsyncHandler = new(this);
        ai = new(this);
        phaseManager = new(this);

        ALBoard board = GetPlayerBoard<ALBoard>();
        ALHand hand = GetPlayerHand<ALHand>();
        SetBoardExitMapping(hand, board, _ => new Vector2I(1, 1));
        SetBoardExitMapping(board, hand, position =>
        {
            int maxX = hand.GetCardsInTree().Count - 1;
            if (maxX < 0)
            {
                throw new InvalidOperationException("[ALPlayer.SetBoardExitMapping] Hand has no cards for exit mapping.");
            }
            int clampedX = Math.Clamp(position.X, 0, maxX);
            return new Vector2I(clampedX, 0);
        });
        ALHand enemyHand = GetEnemyPlayerHand<ALHand>();
        if (enemyHand is not null)
        {
            SetBoardExitMapping(enemyHand, board, _ => new Vector2I(1, -2));
        }
        costArea = board.GetCostArea(ALBoardSide.Player);
        unitsArea = board.GetUnitsRoot(ALBoardSide.Player);
        durabilityArea = board.GetDurabilityArea(ALBoardSide.Player);
        deckField = board.GetDeckField(ALBoardSide.Player);
        cubeDeckField = board.GetCubeDeckField(ALBoardSide.Player);
        flagshipField = board.GetFlagship(ALBoardSide.Player);
        retreatField = board.GetRetreatField(ALBoardSide.Player);
        phaseButtonField = board.GetPhaseButton(ALBoardSide.Player);

        enemyUnitsArea = board.GetUnitsRoot(ALBoardSide.Enemy);
        enemyCostArea = board.GetCostArea(ALBoardSide.Enemy);
        enemyDurabilityArea = board.GetDurabilityArea(ALBoardSide.Enemy);
        enemyDeckField = board.GetDeckField(ALBoardSide.Enemy);
        enemyCubeDeckField = board.GetCubeDeckField(ALBoardSide.Enemy);
        enemyFlagshipField = board.GetFlagship(ALBoardSide.Enemy);
        enemyRetreatField = board.GetRetreatField(ALBoardSide.Enemy);
        enemyPhaseButtonField = board.GetPhaseButton(ALBoardSide.Enemy);

        Callable.From(InitializeEvents).CallDeferred();
    }

    protected void InitializeEvents()
    {
        ALHand hand = GetPlayerHand<ALHand>();
        ALBoard board = GetPlayerBoard<ALBoard>();
        List<ALCard> units = GetUnitsInBoard();
        units.ForEach(unit =>
        {
            unit.OnDurabilityDamage -= ApplyDurabilityDamage;
            unit.OnDurabilityDamage += ApplyDurabilityDamage;
        });
        GD.Print("[InitializeEvents] ALPlayer events initialized");
    }

    public void AssignDeck(ALDeckSet newDeckSet)
    {
        deckSet = newDeckSet;
        GD.Print($"[AssignDeck] Name {newDeckSet.name} - Deck size: {newDeckSet.deck.Count}");
    }

    public void AssignEnemyDeck(ALDeckSet newDeckSet)
    {
        enemyDeckSet = newDeckSet;
        GD.Print($"[AssignEnemyDeck] Name {newDeckSet.name} - Deck size: {newDeckSet.deck.Count}");
        enemyDeckField.CardStack = enemyDeckSet.deck.Count;
        enemyDeckField.UpdateAttributes(enemyDeckSet.deck[^1]);
        enemyCubeDeckField.CardStack = enemyDeckSet.cubeDeck.Count;
        enemyCubeDeckField.UpdateAttributes(enemyDeckSet.cubeDeck[^1]);
        UpdateEnemyFlagship(enemyDeckSet.flagship);
    }

    public bool HasValidEnemyDeck() => enemyDeckSet is not null;

    public void UpdateEnemyFlagship(ALCardDTO card) => enemyFlagshipField.UpdateAttributes(card);

    public async Task StartGameForPlayer(ALDeckSet deckSet)
    {
        // Deck setup
        deckField.CardStack = deckSet.deck.Count; // Set it as deck size
        deckField.UpdateAttributes(deckSet.deck[^1]); // Use last card as template
        // CubeDeck setup
        cubeDeckField.CardStack = deckSet.cubeDeck.Count; // Set it as deck size
        cubeDeckField.UpdateAttributes(deckSet.cubeDeck[^1]); // Use last card as template
        // Flagship setup
        UpdateFlagship(deckSet.flagship);
        ALNetwork.Instance.SyncFlagship(deckSet.flagship.id);

        // Player preparation
        ALHand hand = GetPlayerHand<ALHand>();
        for (int i = 0; i < 5; i++)
        {
            await DrawCardToHand();
        }
        hand.SelectCardField(this, Vector2I.Zero);
        SelectBoard(this, hand);
        await ApplyFlagshipDurability(); // Manual says that this step is after drawing hand cards
    }

    // Turn and Phases
    public void StartTurn()
    {
        GD.Print($"[StartTurn] Start turn for player {Name}");
        phaseManager.StartTurn();
    }

    public async Task EndTurn()
    {
        await TryToTriggerOnAllCards(ALCardEffectTrigger.EndOfTurn);
        await TryToExpireCardsModifierDuration(CardEffectDuration.UntilEndOfTurn);
        if (OnTurnEnd is not null) OnTurnEnd();
    }

    bool TryToSpendCubes(int cost)
    {
        List<ALCard> activeCubes = GetActiveCubesInBoard();

        GD.Print($"[SpendCubes] Cost {cost} - Active cubes {activeCubes.Count}");

        bool debugIgnoreCosts = matchManager.GetDebug().GetIgnoreCosts();
        if (debugIgnoreCosts)
        {
            GD.PrintErr($"[SpendCubes] Debug --- Skipping cost {cost}");
            return true;
        }

        if (activeCubes.Count < cost)
        {
            GD.PrintErr("[SpendCubes] Player doesn't have enough cubes to play this card");
            return false;
        }

        for (int i = 0; i < cost; i++)
        {
            activeCubes[activeCubes.Count - 1 - i].SetIsInActiveState(false); // Last match
        }
        return true;
    }

    public async Task OnCostPlayCardStartHandler(Card cardToPlay)
    {
        ALCard card = cardToPlay.CastToALCard();
        ALCardDTO attributes = card.GetAttributes<ALCardDTO>();
        int cost = card.GetAttributeWithModifiers<ALCardDTO>("Cost");

        GD.Print($"[OnCostPlayCardStartHandler] {card.Name}");
        bool cubesSpent = TryToSpendCubes(cost);
        if (!cubesSpent)
        {
            GD.PrintErr($"[OnCostPlayCardStartHandler] Not enough cubes {cost}");
            return;
        }

        if (attributes.cardType == ALCardType.Event)
        {
            await TryToPlayEventCard(card, (ALHand)card.GetBoard(), CardEffectTrigger.WhenPlayedFromHand);
            return;
        }

        if (attributes.cardType == ALCardType.Ship)
        {
            await OnPlayCardStartHandler(card);
            return;
        }
    }

    public async Task OnCostPlaceCardCancelHandler(Card cardToRestore)
    {
        ALCard card = cardToRestore.CastToALCard();

        ALCardDTO attributes = card.GetAttributes<ALCardDTO>();
        List<ALCard> cubes = GetCubesInBoard();
        GD.Print($"[OnCostPlaceCardCancelHandler] Reverting cubes spent for {attributes.name}");

        for (int i = 0; i < attributes.cost; i++)
        {
            var lastInactiveCubeIndex = cubes.Count - 1 - i;
            if (!lastInactiveCubeIndex.IsInsideBounds(cubes.Count)) continue; // This can happen if CardCost > drawnCubes : Mainly on debug mode 
            cubes[lastInactiveCubeIndex].SetIsInActiveState(true); // Last match
        }
        await OnPlaceCardCancelHandler(card);
    }

    public async Task OnALPlaceCardStartHandler(Card fieldToPlace)
    {
        ALCard cardToPlace = fieldToPlace.CastToALCard();
        ALBoard board = GetPlayerBoard<ALBoard>();
        ALCard fieldBeingPlaced = board.GetSelectedCard<ALCard>(this);
        if (!fieldBeingPlaced.CanPlayerPlaceInThisField()) { GD.PrintErr("[PlaceCardInBoardFromHand] This card place is not placeable!"); return; }
        if (fieldBeingPlaced.GetIsAFlagship())
        {
            GD.PrintErr($"[OnALPlaceCardStartHandler] You cannot place cards in a flagship field");
            return;
        }
        if (!fieldBeingPlaced.GetIsEmptyField())
        {
            ALCardDTO existingCard = fieldBeingPlaced.GetAttributes<ALCardDTO>();
            GD.Print($"[OnALPlaceCardStartHandler] Sending {existingCard} to retreat area");
            AddToRetreatAreaOnTop(existingCard);
        }
        await OnPlaceCardStartHandler(cardToPlace);
        await fieldBeingPlaced.GetEffectManager<ALEffectManager>().AddStatusEffect(ALCardStatusEffects.BattlefieldDelayImpl);
        await OnPlaceCardEndHandler(cardToPlace);
    }

    public void EndGuardPhase()
    {
        GD.PrintErr($"[EndGuardPhase]");
        if (OnAttackGuardEnd is not null) OnAttackGuardEnd(this);
    }
    // Retaliation
    public async Task PlayCardInRetaliationPhase(ALCard selectedCard)
    {
        GD.PrintErr($"[PlayCardInRetaliationPhase]");
        var attrs = selectedCard.GetAttributes<ALCardDTO>();
        if (!attrs.HasEffectWithTrigger(ALCardEffectTrigger.Retaliation))
        {
            GD.PrintErr($"[PlayCardInRetaliationPhase] This card is not playable on a Retaliation Phase");
            return;
        }
        await OnCostPlayCardStartHandler(selectedCard);
    }

    // You can guard cards from field and hand
    public async Task PlayCardAsGuard(ALCard cardToGuard)
    {
        var selectedGuard = cardToGuard.GetBoard();
        if (selectedGuard != GetPlayerBoard<ALBoard>() && selectedGuard != GetPlayerHand<ALHand>())
        {
            GD.PrintErr($"[PlayCardAsGuard] Guard cards can only be played from your hand or board - cardToGuard: {cardToGuard.GetAttributes<ALCardDTO>().name}");
            return;
        }
        if (!cardToGuard.GetIsInActiveState())
        {
            GD.PrintErr($"[PlayCardAsGuard] Select an active card, selected: {cardToGuard.Name}");
            return;
        }
        ;
        if (cardToGuard.GetIsAFlagship())
        {
            GD.PrintErr($"[PlayCardAsGuard] You cannot guard with a flagship, selected: {cardToGuard.Name}");
            return;
        }

        ALCardDTO attrs = cardToGuard.GetAttributes<ALCardDTO>();

        if (selectedGuard is ALBoard guardingBoard)
        {
            if (attrs.supportScope == ALSupportScope.Hand)
            {
                GD.PrintErr($"[PlayCardAsGuard] This card cannot be played as Guard from your board, selected: {attrs.name} {attrs.supportScope}");
                return;
            }
            // Supporting cards are into resting when used

            ALCard attackedCard = GetMatchManager().GetAttackedCard();
            bool canSupport = cardToGuard.CanBattleSupportCard(attackedCard);
            if (!canSupport)
            {
                GD.PrintErr($"[PlayCardAsGuard] {cardToGuard.Name} cannot support {attackedCard.Name}");
                return;
            }
            cardToGuard.SetIsInActiveState(false);
        }

        if (selectedGuard is ALHand guardingHand)
        {
            int cost = cardToGuard.GetAttributeWithModifiers<ALCardDTO>("Cost");

            bool cubesSpent = TryToSpendCubes(cost);
            if (!cubesSpent)
            {
                GD.PrintErr($"[TryToPlayEventCard] Not enough cubes {cost}");
                return;
            }
            if (!cardToGuard.IsCardUnit())
            {
                if (attrs.cardType == ALCardType.Event) // Events are a special card that has its own process
                {
                    await TryToPlayEventCard(cardToGuard, guardingHand, ALCardEffectTrigger.Counter.ToString());
                    return;
                }
                GD.PrintErr($"[PlayCardAsGuard] You can only play ships as units, selected: {attrs.name} {attrs.supportScope}");
                return;
            }
            if (attrs.supportScope == ALSupportScope.Battlefield)
            {
                GD.PrintErr($"[PlayCardAsGuard] This card cannot be played as Guard from your hand, selected: {attrs.name} {attrs.supportScope}");
                return;
            }
            // Supporting cards are discarded when used
            AddToRetreatAreaOnTop(cardToGuard.GetAttributes<ALCardDTO>());
            guardingHand.RemoveCardFromHand(this, cardToGuard);
        }
        if (OnGuardProvided is not null) await OnGuardProvided(this, cardToGuard);
    }

    async Task TryToPlayEventCard(ALCard eventCard, ALHand hand, string trigger)
    {
        var attrs = eventCard.GetAttributes<ALCardDTO>();

        ALEffectManager effectManager = eventCard.GetEffectManager<ALEffectManager>();
        CardEffectDTO[] effects = effectManager.GetEffectsByTrigger(trigger);

        bool canTrigger = effects.Length == 0 || Array.Find(effects, effectManager.CheckCanTriggerEffect) is not null;
        if (!canTrigger) { GD.PrintErr($"[TryToPlayEventCard] Effect condittions not fullfilled. Effects with {trigger}: {effects.Length}"); return; }

        GD.Print("[TryToPlayEventCard] After effect");
        eventCard.SetIsEmptyField(true);
        await eventCard.TryToTriggerCardEffect(trigger);
        AddToRetreatAreaOnTop(eventCard.GetAttributes<ALCardDTO>());
        hand.RemoveCardFromHand(this, eventCard);
    }

    public async Task ApplyDurabilityDamage(Card card)
    {
        List<ALCard> durabilityCards = GetDurabilityCards();
        ALCard durabilityCardInBoard = durabilityCards.FindLast(durabilityCard => durabilityCard.GetIsFaceDown());
        if (durabilityCardInBoard is null)
        {
            GD.PrintErr($"[OnDurabilityDamageHandler] Game over for {Name}");
            if (OnGameOver is not null) await OnGameOver(this);
            return;
        }
        // "Draw" the card to hand 
        ALHand hand = GetPlayerHand<ALHand>();
        ALCard durabilityCardInHand = await hand.AddCardToHand(durabilityCardInBoard.GetAttributes<ALCardDTO>());
        if (GetIsControllerPlayer())
        {
            ALNetwork.Instance.SyncDurabilityDamage(durabilityCardInHand.GetAttributes<ALCardDTO>().id);
        }
        durabilityCardInBoard.DestroyCard(); // Destroy card from board
        await TryToTriggerOnAllCards(ALCardEffectTrigger.OnDamageReceived);
        GD.Print($"[OnDurabilityDamageHandler] {Name} takes damage, durability is {durabilityCards.FindAll(durabilityCard => durabilityCard.GetIsFaceDown()).Count}/{durabilityCards.Count}");
        bool canTriggerRetaliationPhase = durabilityCardInHand.GetEffectManager<ALEffectManager>().GetEffectsByTrigger(ALCardEffectTrigger.Retaliation).Length > 0;
        if (OnRetaliation is not null && canTriggerRetaliationPhase) await OnRetaliation(this, durabilityCardInHand);
    }

    // Actions 

    public async Task StartBattle(ALCard attacker)
    {
        var attrs = attacker.GetAttributes<ALCardDTO>();
        ALBoard board = GetPlayerBoard<ALBoard>();
        if (board.IsEnemyCard(attacker) || !attacker.GetIsInActiveState())
        {
            GD.PrintErr($"[StartBattle] You must start an attack from your board and with an active card - Attacker: {attrs.name}");
            return;
        }
        if (attacker.GetEffectManager<ALEffectManager>().HasActiveEffect(ALCardStatusEffects.BattlefieldDelay))
        {
            GD.PrintErr($"[StartBattle] This card cannot attack the turn that joins the battlefield - Attacker: {attrs.name}");
            return;
        }

        if (OnAttackStart is not null) await OnAttackStart(this, attacker);
    }

    // Limitations on attack
    // BackRow ship -> FrontRow Ships
    // FrontRow Ship -> All ships 
    // Flagship -> All Ships
    public void AttackCard(ALCard attacker, ALCard target)
    {
        ALBoard board = GetPlayerBoard<ALBoard>();
        bool attackerEnemy = board.IsEnemyCard(attacker);
        bool targetEnemy = board.IsEnemyCard(target);
        if (attackerEnemy == targetEnemy || !target.IsCardUnit())
        {
            GD.PrintErr($"[AttackCard] Target must be a placed unit card from the opposite side - Target: {target.GetAttributes<ALCardDTO>().name}");
            return;
        }
        //  if Flagship, they can attack everyone
        bool canBeAttacked =
        attacker.GetIsAFlagship()
        || attacker.GetEffectManager<ALEffectManager>().HasActiveEffect(ALCardStatusEffects.RangedAttack)
        || target.CanBeAttacked(attacker.GetAttackFieldType());
        if (!canBeAttacked)
        {
            GD.PrintErr($"[AttackCard] {attacker.Name} cannot attack {target.Name}");
            return;
        }

        if (OnAttackTargetAdquired is not null) OnAttackTargetAdquired(this, target);

        // Start guard phase    
        if (OnAttackGuardStart is not null) OnAttackGuardStart(this, target.GetOwnerPlayer<ALPlayer>());
    }

    public async Task SettleBattle(ALPlayerUI playerUI)
    {
        await SetPlayState(EPlayState.Wait);
        ALCard attackerCard = matchManager.GetAttackerCard();
        ALCard attackedCard = matchManager.GetAttackedCard();
        ALCardDTO attackerAttrs = attackerCard.GetAttributes<ALCardDTO>();
        ALCardDTO attackedAttrs = attackedCard.GetAttributes<ALCardDTO>();
        float attackerPower = attackerCard.GetAttributeWithModifiers<ALCardDTO>("Power");
        float attackedPower = attackedCard.GetAttributeWithModifiers<ALCardDTO>("Power");

        bool isAttackSuccessful = attackerPower >= attackedPower;
        GD.Print($"[SettleBattle] {attackerAttrs.name} Power: has base {attackerAttrs.power} with modifiers {attackerPower}");
        GD.Print($"[SettleBattle] {attackedAttrs.name} Power: has base {attackedAttrs.power} with modifiers {attackedPower}");
        GD.Print($"[SettleBattle] Is attack succesful: {isAttackSuccessful}");

        await playerUI.OnSettleBattleUI(attackerCard, attackedCard, isAttackSuccessful);

        if (isAttackSuccessful)
        {

            if (attackedCard.GetIsAFlagship())
            {
                GD.Print($"[SettleBattle] {attackedAttrs.name} Takes durability damage!");
                attackedCard.TakeDurabilityDamage();
            }
            else
            {
                GD.Print($"[SettleBattle] {attackedAttrs.name} destroyed!");
                await DestroyUnitCard(attackedCard);
            }
        }
        else
        {
            GD.PrintErr($"[SettleBattle] Attack from {attackerAttrs.name} did not go through");
        }

        // Clean
        await SetPlayState(EPlayState.SelectTarget, ALInteractionState.SelectAttackerUnit);
        if (OnAttackEnd is not null) await OnAttackEnd(this);
        await phaseManager.EndBattlePhaseIfNoActiveCards();
    }


    static async Task DestroyUnitCard(ALCard card)
    {

        card.GetOwnerPlayer<ALPlayer>().AddToRetreatAreaOnTop(card.GetAttributes<ALCardDTO>());
        await card.TryToTriggerCardEffect(ALCardEffectTrigger.OnCardDestroyed);
        card.GetBoard().RetireCard(card);
        card.DestroyCard();
    }

    // Nodes

    static ALCardDTO DrawCard(List<ALCardDTO> deck, ALCard relatedField)
    {
        ALCardDTO drawnCard = Player.DrawCard(deck);
        UpdateDeckStackSize(relatedField, deck.Count);
        return drawnCard;
    }

    static void AddCardToDeck(ALCardDTO cardToAdd, List<ALCardDTO> deck, ALCard boardField, bool top = true)
    {
        if (top)
        {
            deck.Insert(0, cardToAdd);
        }
        else
        {
            deck.Add(cardToAdd);
            boardField.UpdateAttributes(cardToAdd); // Update image src to show bottom card 
        }
        UpdateDeckStackSize(boardField, deck.Count);
    }

    static void UpdateDeckStackSize(ALCard deck, int size)
    {
        //GD.Print($"[UpdateDeckStackSize] {deck.CardStack} -> {size}");
        deck.CardStack = size;
        if (deck.CardStack == 0) deck.SetIsEmptyField(true);
    }


    async Task ApplyFlagshipDurability()
    {
        int durability = flagshipField.GetAttributes<ALCardDTO>().durability;
        GD.Print($"[{Name}.ApplyFlagshipDurability] {durability}");
        for (int i = 0; i < durability; i++)
        {
            await AddDurabilityCard();
        }
    }

    // Public Player Actions 
    public void AddToRetreatAreaOnTop(ALCardDTO cardToAdd)
    {
        // We add to the bottom as the deck works flipped down
        AddCardToDeck(cardToAdd, deckSet.retreatDeck, retreatField, false);
    }
    public async Task CancelAttack(Player player)
    {
        GD.Print($"[CancelAttack]");
        await player.GoBackInHistoryState();
    }

    public virtual async Task CancelSelectEffectState(Player player)
    {
        GD.Print($"[CancelSelectEffectState]");
        await player.GoBackInHistoryState();
    }

    public async Task CancelRetaliation(Player player)
    {
        GD.Print($"[CancelRetaliation]");
        if (OnRetaliationCancel is not null) await OnRetaliationCancel(player);
    }

    public async Task AddDurabilityCard(ALCardDTO card = null)
    {
        ALCard emptyDurability = durabilityArea.TryGetAllChildOfType<ALCard>().Find(card => card.GetIsEmptyField());
        if (emptyDurability is null)
        {
            GD.PrintErr("[AddDurabilityCard] Cannot add more durability to the board");
            return;
        }
        ALCardDTO cardToDraw = card is null ? DrawCard(deckSet.deck, deckField) : card;
        ALNetwork.Instance.ALDrawCard(cardToDraw.id, ALDrawType.Durability);
        await PlaceDurabilityCard(cardToDraw);
    }

    public async Task PlaceDurabilityCard(ALCardDTO card)
    {
        ALCard emptyDurability = durabilityArea.TryGetAllChildOfType<ALCard>().Find(card => card.GetIsEmptyField());
        emptyDurability.UpdateAttributes(card);
        emptyDurability.IsInputSelectable = true;
        await this.Wait(DrawDelay);
    }

    public ALPhase Phase => phaseManager;
    public AsyncHandler GetPlayerAsyncHandler()
    {
        if (playerAsyncHandler is null) GD.PushError("[GetPlayerAsyncHandler] No asyncHandler is set");
        return playerAsyncHandler;
    }

    public List<ALCard> GetUnitsInBoard() => unitsArea.TryGetAllChildOfType<ALCard>();
    public List<ALCard> GetEnemyUnitsInBoard() => enemyUnitsArea.TryGetAllChildOfType<ALCard>();
    public List<ALCard> GetCardsInHand() => GetPlayerHand<ALHand>().TryGetAllChildOfType<ALCard>();
    public List<ALCard> GetActiveUnitsInBoard() => GetUnitsInBoard().FindAll(card => card.GetIsInActiveState());
    public List<ALCard> GetActiveCubesInBoard() => costArea.TryGetAllChildOfType<ALCard>().FindAll(card => card.GetIsInActiveState());
    public List<ALCard> GetEnemyActiveCubesInBoard() => enemyCostArea.TryGetAllChildOfType<ALCard>().FindAll(card => card.GetIsInActiveState());
    public List<ALCard> GetDurabilityCards() => durabilityArea.TryGetAllChildOfType<ALCard>().FindAll(card => !card.GetIsEmptyField());
    public List<ALCard> GetCubesInBoard() => costArea.TryGetAllChildOfType<ALCard>().FindAll(card => !card.GetIsEmptyField());
    public List<ALCard> GetEnemyDurabilityCards() => enemyDurabilityArea.TryGetAllChildOfType<ALCard>().FindAll(card => !card.GetIsEmptyField());
    public List<ALCard> GetEnemyCubesInBoard() => enemyCostArea.TryGetAllChildOfType<ALCard>().FindAll(card => !card.GetIsEmptyField());
    public ALDeckSet GetDeckSet() => deckSet;
    public ALDeckSet GetEnemyDeckSet() => enemyDeckSet;
    public ALCardDTO DrawFromDeck() => DrawCard(deckSet.deck, deckField);
    public ALCardDTO DrawFromCubeDeck() => DrawCard(deckSet.cubeDeck, cubeDeckField);
    public ALCardDTO DrawFromEnemyDeck() => DrawCard(enemyDeckSet.deck, enemyDeckField);
    public ALCardDTO DrawFromEnemyCubeDeck() => DrawCard(enemyDeckSet.cubeDeck, enemyCubeDeckField);
    public bool HasValidDeck() => deckSet is not null;
    public int GetEnemyHandCount() => enemyHandCards.Count;
    public void UpdateFlagship(ALCardDTO card) => flagshipField.UpdateAttributes(card);

    public bool IsAwaitingBattleGuard() =>
        phaseManager.GetCurrentPhase() == EALTurnPhase.Battle
        && GetInputPlayState() == EPlayState.Wait
        && GetInteractionState() == ALInteractionState.AwaitOtherPlayerInteraction
        && matchManager.IsAttackInProgress();

    public ALCard FindAvailableEmptyFieldInRow(bool frontRow = false)
    {
        List<ALCard> fields = GetUnitsInBoard();
        if (frontRow) return fields.Find(field => field.GetAttackFieldType() == EAttackFieldType.FrontRow && field.GetIsEmptyField());
        else return fields.Find(field => field.GetAttackFieldType() == EAttackFieldType.BackRow && field.GetIsEmptyField());
    }

    public string GetCurrentPhaseText() => phaseManager.GetPhaseByIndex((int)matchManager.GetMatchPhase());
    public EALTurnPhase GetCurrentPhase() => phaseManager.GetCurrentPhase();
    public ALGameMatchManager GetMatchManager() => matchManager;

    // Play actions
    public void TriggerPhaseButton(Player player)
    {
        ALBoard board = player.GetPlayerBoard<ALBoard>();
        board.SelectCardField(player, phaseButtonField.PositionInBoard);
        SelectBoard(player, board);
        GD.Print($"[TriggerPhaseButton] {board.GetSelectedCard<Card>(player)}");
        TriggerAction(player, InputAction.Ok);
    }

    public async Task DrawCardToHand()
    {
        ALCardDTO cardToDraw = DrawCard(deckSet.deck, deckField);
        ALNetwork.Instance.ALDrawCard(cardToDraw.id, ALDrawType.Deck);
        await AddCardToHand(cardToDraw);
    }

    public async Task AddCardToHand(ALCardDTO card)
    {
        ALHand hand = GetPlayerHand<ALHand>();
        await hand.AddCardToHand(card);
        await this.Wait(DrawDelay);
    }

    public void AddEnemyCardToHand(ALCardDTO card)
    {
        if (card is null)
        {
            throw new InvalidOperationException("[AddEnemyCardToHand] Card is required.");
        }
        enemyHandCards.Add(card);
        PlayerHand enemyPlayerHand = GetEnemyPlayerHand<PlayerHand>();
        if (enemyPlayerHand is not ALHand enemyHandBoard)
        {
            throw new InvalidOperationException("[AddEnemyCardToHand] Enemy hand board is missing or invalid.");
        }
        enemyHandBoard.AddEnemyCardToHand(card);
    }

    public void RemoveEnemyCardFromHand(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new InvalidOperationException("[RemoveEnemyCardFromHand] Card id is required.");
        }
        int index = enemyHandCards.FindIndex(card => card.id == cardId);
        if (index < 0)
        {
            throw new InvalidOperationException($"[RemoveEnemyCardFromHand] Card id not found in enemy hand: {cardId}");
        }
        enemyHandCards.RemoveAt(index);
        PlayerHand enemyPlayerHand = GetEnemyPlayerHand<PlayerHand>();
        if (enemyPlayerHand is not ALHand enemyHandBoard)
        {
            throw new InvalidOperationException("[RemoveEnemyCardFromHand] Enemy hand board is missing or invalid.");
        }
        enemyHandBoard.RemoveEnemyCardFromHand(cardId);
    }

    public void SetBoardCardsAsActive()
    {
        // We wanna only reset units and cost area cards in AzurLane TCG
        costArea.TryGetAllChildOfType<ALCard>().ForEach(card => card.SetIsInActiveState(true));
        GetUnitsInBoard().ForEach(card => card.SetIsInActiveState(true));
    }

    public async Task TryDrawCubeToBoard()
    {

        ALCardDTO cardToDraw = DrawCard(deckSet.cubeDeck, cubeDeckField);
        ALNetwork.Instance.ALDrawCard(cardToDraw.id, ALDrawType.Cube);
        await PlaceCubeToBoard(cardToDraw);
    }

    public async Task PlaceCubeToBoard(ALCardDTO cube)
    {
        try
        {
            Card cubeField = PlayerBoard.FindLastEmptyFieldInRow(
                   costArea.TryGetAllChildOfType<Card>()
               );
            cubeField.IsInputSelectable = true;
            GetPlayerBoard<ALBoard>()
                .GetCardInPosition<ALCard>(this, cubeField.PositionInBoard)
                .UpdateAttributes(cube);

            await TryToTriggerOnAllCards(ALCardEffectTrigger.OnMaxCubeCountChanged);
        }
        catch (Exception e)
        {
            GD.Print(e); // Expected when cube deck is empty
            return;
        }
    }

    public async Task PlaceEnemyCubeToBoard(ALCardDTO cube)
    {
        try
        {
            Card cubeField = PlayerBoard.FindLastEmptyFieldInRow(
                   enemyCostArea.TryGetAllChildOfType<Card>()
               );
            if (cubeField is not ALCard enemyCube)
            {
                throw new InvalidOperationException("[PlaceEnemyCubeToBoard] No cube field available.");
            }
            enemyCube.IsInputSelectable = true;
            enemyCube.UpdateAttributes(cube);
            await Task.CompletedTask;
        }
        catch (Exception e)
        {
            GD.Print(e); // Expected when cube deck is empty
            return;
        }
    }

    public async Task PlaceEnemyDurabilityCard(ALCardDTO card)
    {
        ALCard emptyDurability = enemyDurabilityArea.TryGetAllChildOfType<ALCard>().Find(durability => durability.GetIsEmptyField());
        if (emptyDurability is null)
        {
            GD.PrintErr("[PlaceEnemyDurabilityCard] Cannot add more durability to the enemy board");
            return;
        }
        emptyDurability.UpdateAttributes(card);
        emptyDurability.IsInputSelectable = true;
        await this.Wait(DrawDelay);
    }

    public void ApplyEnemyDurabilityDamage(ALCardDTO card)
    {
        if (card is null)
        {
            throw new InvalidOperationException("[ApplyEnemyDurabilityDamage] Card is required.");
        }
        ALCard durabilityCardInBoard = GetEnemyDurabilityCards().FindLast(durabilityCard => durabilityCard.GetIsFaceDown());
        if (durabilityCardInBoard is null)
        {
            throw new InvalidOperationException("[ApplyEnemyDurabilityDamage] No enemy durability card available.");
        }
        durabilityCardInBoard.DestroyCard();
        AddEnemyCardToHand(card);
    }

    public async Task PlaceEnemyCardToBoard(ALCardDTO card, string boardName, string fieldPath)
    {
        if (card is null)
        {
            throw new InvalidOperationException("[PlaceEnemyCardToBoard] Card is required.");
        }
        if (string.IsNullOrWhiteSpace(boardName))
        {
            throw new InvalidOperationException("[PlaceEnemyCardToBoard] Board name is required.");
        }
        if (string.IsNullOrWhiteSpace(fieldPath))
        {
            throw new InvalidOperationException("[PlaceEnemyCardToBoard] Field path is required.");
        }
        if (boardName != "Board")
        {
            throw new InvalidOperationException($"[PlaceEnemyCardToBoard] Unsupported board name: {boardName}");
        }
        ALBoard board = GetPlayerBoard<ALBoard>() ?? throw new InvalidOperationException("[PlaceEnemyCardToBoard] Player board is missing.");
        string enemyFieldPath = fieldPath.Replace("Player/", "EnemyPlayer/");
        Node enemyNode = board.GetNodeOrNull(enemyFieldPath);
        if (enemyNode is not ALCard targetField)
        {
            throw new InvalidOperationException($"[PlaceEnemyCardToBoard] Enemy field not found for path {enemyFieldPath}.");
        }
        if (!targetField.GetIsEmptyField())
        {
            AddEnemyToRetreatAreaOnTop(targetField.GetAttributes<ALCardDTO>());
        }
        targetField.UpdateAttributes(card);
        await targetField.GetEffectManager<ALEffectManager>().AddStatusEffect(ALCardStatusEffects.BattlefieldDelayImpl);
        RemoveEnemyCardFromHand(card.id);
    }

    public ALBasicAI GetPlayerAIController() => ai;

    void AddEnemyToRetreatAreaOnTop(ALCardDTO cardToAdd)
    {
        if (cardToAdd is null)
        {
            throw new InvalidOperationException("[AddEnemyToRetreatAreaOnTop] Card is required.");
        }
        if (enemyDeckSet is null)
        {
            throw new InvalidOperationException("[AddEnemyToRetreatAreaOnTop] Enemy deck is required.");
        }
        AddCardToDeck(cardToAdd, enemyDeckSet.retreatDeck, enemyRetreatField, false);
    }

}

public enum EALTurnPhase
{
    Reset,
    Preparation,
    Main,
    Battle,
    End
}
