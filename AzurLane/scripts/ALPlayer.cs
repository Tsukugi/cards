
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ALPlayer : Player
{
    public event ProvideCardInteractionEvent OnAttackStart;
    public event ProvideCardInteractionEvent OnAttackTargetAdquired;
    public event InteractionEvent OnAttackEnd;
    public event EnemyInteractionRequestEvent OnAttackGuardStart;
    public event InteractionEvent OnAttackGuardEnd;
    public event ProvideCardInteractionEvent OnGuardProvided;

    // --- Events ---
    public event Action OnTurnEnd;

    // --- Refs ---
    ALDeckSet deckSet = new();
    ALGameMatchManager matchManager;
    // --- AI ---
    ALBasicAI ai;

    // --- Nodes --- 
    Node3D costArea, durabilityArea, unitsArea;
    [Export]
    ALCard deckField, cubeDeckField, flagshipField, retreatField;
    [Export]
    ALPhaseButton phaseButtonField;

    // --- Phase ---
    AsyncHandler playerAsyncHandler;
    ALPhase phaseManager;

    public override void _Ready()
    {
        base._Ready(); // Required for board/hand refs
        matchManager = this.TryFindParentNodeOfType<ALGameMatchManager>();

        playerAsyncHandler = new(this);
        ai = new(this);
        phaseManager = new(this);

        ALBoard board = GetPlayerBoard<ALBoard>();
        costArea = board.GetNode<Node3D>("CostArea");
        unitsArea = board.GetNode<Node3D>("Units");
        durabilityArea = board.GetNode<Node3D>("FlagshipDurability");

        Callable.From(StartGameForPlayer).CallDeferred();
    }

    protected override void InitializeEvents()
    {
        base.InitializeEvents();
        ALHand hand = GetPlayerHand<ALHand>();
        ALBoard board = GetPlayerBoard<ALBoard>();
        hand.OnPlayCardStart -= OnPlayCardStartHandler; // Unload default event
        hand.OnPlayCardStart -= OnCostPlayCardStartHandler;
        hand.OnPlayCardStart += OnCostPlayCardStartHandler;
        board.OnPlaceCardCancel -= OnPlaceCardCancelHandler; // Unload default event
        board.OnPlaceCardCancel -= OnCostPlaceCardCancelHandler;
        board.OnPlaceCardCancel += OnCostPlaceCardCancelHandler;
        board.OnPlaceCardStart -= OnPlaceCardStartHandler; // Unload default event
        board.OnPlaceCardStart -= OnALPlaceCardStartHandler;
        board.OnPlaceCardStart += OnALPlaceCardStartHandler;
        List<ALCard> units = GetUnitsInBoard();
        units.ForEach(unit =>
        {
            unit.OnDurabilityDamage -= OnDurabilityDamageHandler;
            unit.OnDurabilityDamage += OnDurabilityDamageHandler;
        });

        GD.Print("[InitializeEvents] ALPlayer events initialized");
    }

    protected override void UnassignBoardEvents(Board board)
    {
        base.UnassignBoardEvents(board);
        ALBoard enemyBoard = GetEnemyPlayerBoard<ALBoard>();
        if (enemyBoard is not null) enemyBoard.OnCardTrigger -= OnCardTriggerHandler;
    }
    protected override void AssignBoardEvents(Board board)
    {
        UnassignBoardEvents(board);
        base.AssignBoardEvents(board);
        ALBoard enemyBoard = GetEnemyPlayerBoard<ALBoard>();
        if (enemyBoard is not null) enemyBoard.OnCardTrigger += OnCardTriggerHandler;
    }

    public void AssignDeck(ALDeckSet newDeckSet)
    {
        deckSet = newDeckSet;
        GD.Print($"[AssignDeck] Deck size: {newDeckSet.deck.Count}");
    }

    void StartGameForPlayer()
    {
        // Deck setup
        deckField.CardStack = deckSet.deck.Count; // Set it as deck size
        deckField.UpdateAttributes(deckSet.deck[^1]); // Use last card as template
        // CubeDeck setup
        cubeDeckField.CardStack = deckSet.cubeDeck.Count; // Set it as deck size
        cubeDeckField.UpdateAttributes(deckSet.cubeDeck[^1]); // Use last card as template
        // Flagship setup
        flagshipField.UpdateAttributes(deckSet.flagship);

        // Player preparation
        ALHand hand = GetPlayerHand<ALHand>();
        DrawCardToHand(5);
        SelectBoard(hand);
        hand.SelectCardField(this, Vector2I.Zero);
        ApplyFlagshipDurability(); // Manual says that this step is after drawing hand cards
    }

    // Turn and Phases
    public void StartTurn()
    {
        GD.Print($"[StartTurn] Start turn for player {Name}");
        if (!GetIsControllerPlayer()) _ = ai.StartTurn();
        phaseManager.StartTurn();
    }

    public void EndTurn()
    {
        if (OnTurnEnd is not null) OnTurnEnd();
    }

    // Event handlers
    public void OnCostPlayCardStartHandler(Card cardToPlay)
    {
        var currentPhase = phaseManager.GetCurrentPhase();
        if (currentPhase != EALTurnPhase.Main)
        {
            GD.PrintErr($"[OnCostPlayCardStartHandler] Cannot place cards outside main phase");
            return;
        }

        ALCard card = cardToPlay.CastToALCard();
        ALCardDTO attributes = card.GetAttributes<ALCardDTO>();

        List<ALCard> activeCubes = GetActiveCubesInBoard();

        GD.Print($"[OnCostPlayCardStartHandler] Cost {attributes.cost} - Active cubes {activeCubes.Count}");

        if (attributes.cost > activeCubes.Count)
        {
            GD.PrintErr("[OnCostPlayCardStartHandler] Player doesn't have enough cubes to play this card");
            return;
        }

        for (int i = 0; i < attributes.cost; i++)
        {
            activeCubes[activeCubes.Count - 1 - i].SetIsInActiveState(false); // Last match
        }

        OnPlayCardStartHandler(card);
    }

    protected void OnCostPlaceCardCancelHandler(Card cardToRestore)
    {
        ALCard card = cardToRestore.CastToALCard();

        ALCardDTO attributes = card.GetAttributes<ALCardDTO>();
        List<ALCard> cubes = GetCubesInBoard();
        GD.Print($"[OnCostPlaceCardCancelHandler] Reverting cubes spent for {attributes.name}");

        for (int i = 0; i < attributes.cost; i++)
        {
            cubes[cubes.Count - 1 - i].SetIsInActiveState(true); // Last match
        }

        OnPlaceCardCancelHandler(card);
    }

    protected void OnALPlaceCardStartHandler(Card fieldToPlace)
    {
        ALCard cardToPlace = fieldToPlace.CastToALCard();
        ALBoard board = GetPlayerBoard<ALBoard>();
        ALCard fieldBeingPlaced = board.GetSelectedCard<ALCard>(this);
        if (fieldBeingPlaced.GetIsAFlagship())
        {
            GD.PrintErr($"[OnALPlaceCardStartHandler] You cannot place cards in a flagship field");
            return;
        }
        if (!fieldBeingPlaced.IsEmptyField)
        {
            ALCardDTO existingCard = fieldBeingPlaced.GetAttributes<ALCardDTO>();
            GD.Print($"[OnALPlaceCardStartHandler] Sending {existingCard} to retreat area");
            AddToRetreatAreaOnTop(existingCard);
        }
        OnPlaceCardStartHandler(cardToPlace);
    }

    protected override void OnCardTriggerHandler(Card field)
    {
        playerAsyncHandler.Debounce(() =>
        {
            base.OnCardTriggerHandler(field);
            EALTurnPhase currentPhase = phaseManager.GetCurrentPhase();
            EALTurnPhase matchPhase = matchManager.GetMatchPhase(); // I want the synched match phase so both player can interact
            GD.Print($"{currentPhase} {matchPhase}");
            if (field is ALPhaseButton)
            {
                if (currentPhase == EALTurnPhase.Main) phaseManager.PlayNextPhase();
                if (matchPhase == EALTurnPhase.Battle)
                {
                    if (GetPlayState() == EPlayState.Select) phaseManager.PlayNextPhase();
                    if (GetPlayState() == EPlayState.EnemyInteraction) EndGuardPhase();
                }
            }
            if (field is ALCard card)
            {
                if (currentPhase == EALTurnPhase.Battle)
                {
                    if (GetPlayState() == EPlayState.Select) StartBattle(card);
                    if (GetPlayState() == EPlayState.SelectTarget) AttackCard(matchManager.GetAttackerCard(), card);

                }
                if (matchPhase == EALTurnPhase.Battle)
                {
                    if (GetPlayState() == EPlayState.EnemyInteraction) PlayCardAsGuard(card);
                }
            }
        }, 1f);
    }

    void EndGuardPhase()
    {
        GD.PrintErr($"[EndGuardPhase]");
        if (OnAttackGuardEnd is not null) OnAttackGuardEnd(this);
    }

    // You can guard cards from field and hand
    void PlayCardAsGuard(ALCard cardToGuard)
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
        };
        if (cardToGuard.GetIsAFlagship())
        {
            GD.PrintErr($"[PlayCardAsGuard] You cannot guard with a flagship, selected: {cardToGuard.Name}");
            return;
        }

        ALCardDTO attrs = cardToGuard.GetAttributes<ALCardDTO>();
        if (selectedGuard is ALBoard guardingBoard)
        {
            if (attrs.supportScope == EALSupportScope.Hand.ToString())
            {
                GD.PrintErr($"[PlayCardAsGuard] This card cannot be played as Guard from your board, selected: {attrs.name} {attrs.supportScope}");
                return;
            }
            // Supporting cards are into resting when used
            cardToGuard.SetIsInActiveState(false);
        }
        if (selectedGuard is ALHand guardingHand)
        {
            if (attrs.supportScope == EALSupportScope.Battlefield.ToString())
            {
                GD.PrintErr($"[PlayCardAsGuard] This card cannot be played as Guard from your hand, selected: {attrs.name} {attrs.supportScope}");
                return;
            }
            // Supporting cards are discarded when used
            AddToRetreatAreaOnTop(cardToGuard.GetAttributes<ALCardDTO>());
            guardingHand.RemoveCardFromHand(cardToGuard);
        }
        if (OnGuardProvided is not null) OnGuardProvided(this, cardToGuard);
    }

    void OnDurabilityDamageHandler(Card card)
    {
        List<ALCard> durabilityCards = GetDurabilityCards();
        ALCard durabilityCard = durabilityCards.FindLast(durabilityCard => durabilityCard.GetIsFaceDown());
        if (durabilityCard is null)
        {
            GD.PrintErr($"[OnDurabilityDamageHandler] Game over for {Name}");
            return;
        }
        // "Draw" the card to hand 
        durabilityCard.DestroyCard(); // Destroy card from board
        ALHand hand = GetPlayerHand<ALHand>();
        hand.AddCardToHand(durabilityCard.GetAttributes<ALCardDTO>());
        GD.Print($"[OnDurabilityDamageHandler] {Name} takes damage, durability is {durabilityCards.FindAll(durabilityCard => durabilityCard.GetIsFaceDown()).Count}/{durabilityCards.Count}");
    }

    // Actions 

    void StartBattle(ALCard attacker)
    {
        if (attacker.GetBoard() != GetPlayerBoard<ALBoard>() || !attacker.GetIsInActiveState())
        {
            GD.PrintErr($"[StartBattle] You must start an attack from your board and with an active card - Attacker: {attacker.GetAttributes<ALCardDTO>().name}");
            return;
        }

        if (OnAttackStart is not null) OnAttackStart(this, attacker);
        SetPlayState(EPlayState.SelectTarget);
    }

    // Limitations on attack
    // BackRow ship -> FrontRow Ships
    // FrontRow Ship -> All ships 
    // Flagship -> All Ships
    void AttackCard(ALCard attacker, ALCard target)
    {
        if (target.GetBoard() != GetEnemyPlayerBoard<ALBoard>() || !target.IsCardUnit())
        {
            GD.PrintErr($"[AttackCard] Target must be from the enemy board and has to be a placed unit card - Target: {target.GetBoard().Name}.{target.GetAttributes<ALCardDTO>().name}");
            return;
        }
        if (attacker.GetBoard().GetIsEnemyBoard() == target.GetBoard().GetIsEnemyBoard())
        {
            GD.PrintErr($"[AttackCard] {attacker.Name} cannot attack {target.Name} as they are allies!");
            return;
        }
        //  if Flagship, they can attack everyone
        bool canBeAttacked = attacker.GetIsAFlagship() || target.CanBeAttacked(attacker.GetAttackFieldType());
        if (!canBeAttacked)
        {
            GD.PrintErr($"[AttackCard] {attacker.Name} cannot attack {target.Name}");
            return;
        }

        if (OnAttackTargetAdquired is not null) OnAttackTargetAdquired(this, target);

        // Start guard phase    
        if (OnAttackGuardStart is not null) OnAttackGuardStart(this, target.GetOwnerPlayer<ALPlayer>());
    }

    public async Task SettleBattle()
    {
        SetPlayState(EPlayState.Wait);
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

        if (isAttackSuccessful)
        {
            if (matchManager.GetAttackedCard().GetIsAFlagship())
            {
                GD.Print($"[SettleBattle] {attackedAttrs.name} Takes durability damage!");
                matchManager.GetAttackedCard().TakeDurabilityDamage();
            }
            else
            {
                GD.Print($"[SettleBattle] {attackedAttrs.name} destroyed!");
                matchManager.GetAttackedCard().DestroyCard();
            }
        }
        else
        {
            GD.PrintErr($"[SettleBattle] Attack from {attackerAttrs.name} did not go through");
        }

        // Clean
        SetPlayState(EPlayState.Select);
        if (OnAttackEnd is not null) OnAttackEnd(this);
        await phaseManager.EndBattlePhaseIfNoActiveCards();
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
        GD.Print($"[UpdateDeckStackSize] {deck.CardStack} -> {size}");
        deck.CardStack = size;
        if (deck.CardStack == 0) deck.IsEmptyField = true;
    }

    void AddToRetreatAreaOnTop(ALCardDTO cardToAdd)
    {
        // We add to the bottom as the deck works flipped down
        AddCardToDeck(cardToAdd, deckSet.retreatDeck, retreatField, false);
    }

    void ApplyFlagshipDurability()
    {
        int durability = flagshipField.GetAttributes<ALCardDTO>().durability;
        List<ALCard> durabilityList = durabilityArea.TryGetAllChildOfType<ALCard>();
        for (int i = 0; i < durability; i++)
        {
            ALCardDTO cardToDraw = DrawCard(deckSet.deck, deckField);
            durabilityList[i].UpdateAttributes(cardToDraw);
            durabilityList[i].IsInputSelectable = true;
        }
    }
    void TryToExpireCardsModifierDuration(EALCardSkillDuration duration)
    {
        GetPlayerBoard<ALBoard>().GetCardsInTree().ForEach(card => card.TryToExpireModifier(duration.ToString()));
        GetPlayerHand<ALHand>().GetCardsInTree().ForEach(card => card.TryToExpireModifier(duration.ToString()));
    }

    // Public Player Actions for AI 
    public ALPhase Phase => phaseManager;
    public AsyncHandler GetPlayerAsyncHandler()
    {
        if (playerAsyncHandler is null) GD.PushError("[GetPlayerAsyncHandler] No asyncHandler is set");
        return playerAsyncHandler;
    }

    public List<ALCard> GetUnitsInBoard() => unitsArea.TryGetAllChildOfType<ALCard>();
    public List<ALCard> GetActiveUnitsInBoard() => GetUnitsInBoard().FindAll(card => card.GetIsInActiveState());
    public List<ALCard> GetActiveCubesInBoard() => costArea.TryGetAllChildOfType<ALCard>().FindAll(card => card.GetIsInActiveState());
    public List<ALCard> GetDurabilityCards() => durabilityArea.TryGetAllChildOfType<ALCard>().FindAll(card => !card.IsEmptyField);
    public List<ALCard> GetCubesInBoard() => costArea.TryGetAllChildOfType<ALCard>().FindAll(card => !card.IsEmptyField);

    public bool IsAwaitingBattleGuard() =>
        phaseManager.GetCurrentPhase() == EALTurnPhase.Battle
        && GetPlayState() == EPlayState.AwaitEnemyInteraction
        && matchManager.IsAttackInProgress();

    public ALCard FindAvailableEmptyFieldInRow(bool frontRow = false)
    {
        List<ALCard> fields = GetUnitsInBoard();
        if (frontRow) return fields.Find(field => field.GetAttackFieldType() == EAttackFieldType.FrontRow && field.IsEmptyField);
        else return fields.Find(field => field.GetAttackFieldType() == EAttackFieldType.BackRow && field.IsEmptyField);
    }

    public string GetCurrentPhaseText() => phaseManager.GetPhaseByIndex((int)matchManager.GetMatchPhase());
    public EALTurnPhase GetCurrentPhase() => phaseManager.GetCurrentPhase();
    public ALGameMatchManager GetMatchManager() => matchManager;

    // Play actions
    public void TriggerPhaseButton()
    {
        ALBoard board = GetPlayerBoard<ALBoard>();
        SelectBoard(board);
        // Select phase button
        board.SelectCardField(this, phaseButtonField.PositionInBoard);
        TriggerAction(InputAction.Ok);
    }

    public void DrawCardToHand(int num = 1)
    {
        for (int i = 0; i < num; i++)
        {
            ALCardDTO cardToDraw = DrawCard(deckSet.deck, deckField);
            ALHand hand = GetPlayerHand<ALHand>();
            hand.AddCardToHand(cardToDraw);
        }
    }

    public void SetBoardCardsAsActive()
    {
        // We wanna only reset units and cost area cards in AzurLane TCG
        costArea.TryGetAllChildOfType<ALCard>().ForEach(card => card.SetIsInActiveState(true));
        GetUnitsInBoard().ForEach(card => card.SetIsInActiveState(true));
    }

    public void TryDrawCubeToBoard()
    {
        try
        {
            ALCardDTO cardToDraw = DrawCard(deckSet.cubeDeck, cubeDeckField);
            Card cubeField = PlayerBoard.FindLastEmptyFieldInRow(
                costArea.TryGetAllChildOfType<Card>()
            );
            cubeField.IsInputSelectable = true;
            GetPlayerBoard<ALBoard>()
                .GetCardInPosition<ALCard>(this, cubeField.PositionInBoard)
                .UpdateAttributes(cardToDraw);
        }
        catch (Exception e)
        {
            GD.Print(e); // Expected when cube deck is empty
            return;
        }
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
