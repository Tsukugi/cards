# Card Effects Implementation

This document describes how card effects are wired, triggered, and executed in the
Azur Lane (AL) gameplay code. It focuses on the runtime implementation details.

## Core Data Types
- `CardDTO` and `CardEffectDTO` define the effect payload stored in the card data.
  - `CardEffectDTO.effectId` is the method name that will be invoked by reflection.
  - `CardEffectDTO.triggerEvent[]` lists trigger strings that fire the effect.
  - `CardEffectDTO.condition[]` lists condition entries that must all pass.
  - `CardEffectDTO.effectValue[]` stores parameters for the effect method.
  - `CardEffectDTO.duration` drives effect expiration rules.
- AL-specific constants live in `ALCardDTO` (triggers, durations, status effects).

## Effect Managers
- `Card` owns an `EffectManager` instance; AL cards replace it with `ALEffectManager`.
- `EffectManager.ApplyEffect` calls `ClassUtils.CallMethodAsync` with `effectId`.
- `EffectManager.CheckCondition` calls `ClassUtils.CallMethod` with `conditionId`.
- Status effects are stored in `Card.activeStatusEffects` and read by name.

## Trigger Flow
- `Card.TryToTriggerCardEffect(triggerEvent)` forwards to the effect manager.
- `ALEffectManager.GetEffectsByTrigger` filters `CardEffectDTO.triggerEvent`.
- `ALEffectManager.CheckCanTriggerEffect` runs all condition methods.
- If all conditions pass, `ApplyEffect` invokes the effect method by name.

## Trigger Sources (AL)
- Attack start: `ALGameMatchManager.OnAttackStartHandler` triggers `StartsAttack`.
- Guard end: `ALGameMatchManager.OnAttackGuardEndHandler` triggers `IsAttacked`.
- Guard support: `ALGameMatchManager.OnGuardProvidedHandler` triggers `IsBattleSupported`.
- Retaliation window: `ALGameMatchManager.OnRetaliationHandler` triggers `Retaliation`.
- Turn start: `ALGameMatchManager.OnTurnEndHandler` triggers `StartOfTurn` on all cards.
- Turn end: `ALPlayer.EndTurn` triggers `EndOfTurn` on all cards.
- Durability damage: `ALPlayer.ApplyDurabilityDamage` triggers `OnDamageReceived`.
- Card destroyed: `ALPlayer.DestroyUnitCard` triggers `OnCardDestroyed`.
- Cube count change: `ALPlayer.PlaceCubeToBoard` triggers `OnMaxCubeCountChanged`.
- Hand visibility: `ALHand.AddCardToHand` triggers `OnVisible`.
- Event card play: `ALPlayer.TryToPlayEventCard` uses `WhenPlayedFromHand` or `Counter`.

## Conditions (AL)
Condition handlers live in `ALEffectManager` and are called by name:
- `CheckShipsInBoard`
- `CheckCardsInHand`
- `IsSpecificCardOnField`
- `IsPlacedOnBackRow`
- `CheckFlagshipDurability`
- `HasCubes`
- `CurrentPlayerPlayingTurn`
Base condition `CheckCardCount` is implemented in `EffectManager`.

## Effect Methods (AL)
`ALEffectManager` hosts the effect methods invoked by `effectId`:
- `GetPower` and `SelectAndGivePower` add `AttributeModifier` entries.
- `AddStatusEffect` appends to `activeStatusEffects`.
- `ReturnEnemyToHand` returns a target to the enemy hand (ship-only).
- `Awakening` swaps card attributes to the awakened variant.
- `ReactivateCube`, `Reactivate`, `Retaliation`, `DiscardAndDrawCubeDown`.
- `RestEnemy`, `RestOrDestroyEnemy`, `Rush`.

## Selection-Driven Effects
Effects that require a target selection call `ApplySelectEffectTargetAction`:
- Moves the player into `EPlayState.SelectTarget` and `ALInteractionState.SelectEffectTarget`.
- Waits for `Board.OnCardEffectTargetSelected` or `ALHand.OnCardEffectTargetSelected`.
- Restores the previous play state after the effect finishes or cancels.

## Status Effects and Modifiers
- Status effects are stored in `Card.activeStatusEffects`.
- `ALCardStatusEffects` defines the shared keys (e.g., `BattlefieldDelay`).
- Modifiers are applied via `Card.AddModifier` and read by `GetAttributeWithModifiers`.
- Expiration is driven by `Card.TryToExpireEffectOrModifier(duration)` and called from:
  - `ALPhase` (phase transitions),
  - `ALGameMatchManager.OnAttackEndHandler` (current battle),
  - `ALPlayer.EndTurn` (until end of turn),
  - `Card.SetIsFaceDown` and `Card.DestroyCard` (visibility/face-down).
