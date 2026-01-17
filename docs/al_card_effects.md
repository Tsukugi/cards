# Azur Lane Card Effects (Logic Summary)

This document summarizes how card effects are defined, triggered, and resolved in the Azur Lane (AL) gameplay code.

## Effect Data Model
- Card data carries `effects` entries (see `CardDTO` and `ALCardDTO`).
- Each `CardEffectDTO` includes:
  - `triggerEvent[]`: string triggers that cause the effect to run.
  - `condition[]`: condition entries checked before the effect can run.
  - `duration`: how long effect-driven modifiers or status effects last.
  - `effectId`: method name to execute (dynamic call).
  - `effectLabel`: the display text shown in UI.
  - `effectValue[]`: parameters passed to the effect logic.
  - `stackableEffect`: whether a status effect can be applied multiple times.

## Core Flow
- Cards hold an `EffectManager` instance; AL cards override this with `ALEffectManager`.
- `Card.TryToTriggerCardEffect(trigger)` calls into the effect manager for matching effects.
- `ALEffectManager.GetEffectsByTrigger` filters effects whose `triggerEvent` includes the trigger string.
- `ALEffectManager.CheckCanTriggerEffect` evaluates all conditions; all must pass to proceed.
- `ApplyEffect` uses reflection (`ClassUtils.CallMethodAsync`) to invoke the method named by `effectId`.

## Triggers (Where They Fire)
AL-specific triggers are centralized in `ALCardEffectTrigger` and invoked by game flow:
- Start of attack: `ALGameMatchManager.OnAttackStartHandler` -> `StartsAttack`.
- When attacked (after guard phase): `ALGameMatchManager.OnAttackGuardEndHandler` -> `IsAttacked`.
- On battle support (guard provided): `ALGameMatchManager.OnGuardProvidedHandler` -> `IsBattleSupported`.
- Retaliation (flagship durability damage): `ALGameMatchManager.OnRetaliationHandler` -> `Retaliation`.
- Start of turn: `ALGameMatchManager.OnTurnEndHandler` triggers `StartOfTurn` on both players’ cards.
- End of turn: `ALPlayer.EndTurn` -> `EndOfTurn`.
- On damage received: `ALPlayer.ApplyDurabilityDamage` -> `OnDamageReceived` on all cards.
- On card destroyed: `ALPlayer.DestroyUnitCard` -> `OnCardDestroyed`.
- On max cube count change: `ALPlayer.PlaceCubeToBoard` -> `OnMaxCubeCountChanged`.
- General triggers in `CardEffectTrigger` are used for event cards and visibility (`WhenPlayedFromHand`, `WhenPlayedIntoBoard`, `OnVisible`).

## Conditions (AL-Specific)
Condition methods live in `ALEffectManager` and are invoked by name:
- `CheckShipsInBoard`: find ship units on board matching attribute comparison.
- `CheckCardsInHand`: find cards in hand matching attribute comparison.
- `IsSpecificCardOnField`: checks for a specific card ID on the owner’s board.
- `IsPlacedOnBackRow`: true if the card is in the back row.
- `CheckFlagshipDurability`: compares remaining durability cards.
- `HasCubes`: compares active cube count.
- `CurrentPlayerPlayingTurn`: validates whose turn it is.
- Base condition: `CheckCardCount` (from `EffectManager`) checks counts on hand/board.

All conditions must pass; any failure cancels the effect for that trigger.

## Effect Implementations (AL)
Effects are methods on `ALEffectManager` and are invoked by `effectId`:
- `GetPower`: adds a power modifier for the current battle.
- `SelectAndGivePower`: prompts target selection, adds a power modifier for `effectDTO.duration`.
- `AddStatusEffect`: adds a status effect (non-stackable unless `stackableEffect` is true).
- `ReturnEnemyToHand`: select enemy ship card that matches cost/attribute comparator; return to hand.
- `Awakening`: swaps the card’s attributes to the awakened variant (by ID + "覺醒").
- `ReactivateCube`: re-activates an inactive cube in the owner’s cost area.
- `Reactivate`: sets the card’s active state to true.
- `Retaliation`: adds a cost modifier making the card effectively free for the interaction.
- `DiscardAndDrawCubeDown`: discard a selected hand card, then draw a cube to the cost area.
- `RestEnemy`: select enemy unit and set to resting if currently active.
- `RestOrDestroyEnemy`: rest active enemy or destroy if already resting.
- `Rush`: removes the `BattlefieldDelay` status effect.

## Status Effects and Modifiers
- Status effects are stored in `Card.activeStatusEffects` and referenced by `effectValue[0]`.
- Key AL status effects:
  - `BattlefieldDelay`: unit cannot attack the turn it is played.
  - `LimitBattleSupport`: restricts battle support to specific field types.
  - `RangedAttack`: allows attacking back row.
- Attribute modifiers are applied via `Card.AddModifier` and read through `GetAttributeWithModifiers`.
- Modifier expiration is handled by `TryToExpireEffectOrModifier(duration)` on cards.
- Expiration points:
  - Phase changes (`ALPhase`): expire `MainPhase` and `BattlePhase` durations.
  - Battle end (`ALGameMatchManager.OnAttackEndHandler`): expire `CurrentBattle`.
  - Turn end (`ALPlayer.EndTurn`): expire `UntilEndOfTurn`.
  - Face-down/visibility changes: expire `WhileFaceDown` or `WhileVisible` on flip/destroy.

## Selection-Driven Effects
Several effects pause the game and require user selection:
- `SelectAndGivePower`, `ReturnEnemyToHand`, `DiscardAndDrawCubeDown`, `RestEnemy`,
  and `RestOrDestroyEnemy` all call `ApplySelectEffectTargetAction`.
- This sets `PlayState` to `SelectTarget` and waits for `OnCardEffectTargetSelected`.
- After a valid selection, the player’s state returns to the previous play state.

## Event Cards and Guard Interaction
- Event cards are played via `TryToPlayEventCard` (hand only).
- Event effects use `WhenPlayedFromHand` or `Counter` triggers; conditions must pass.
- Guard interactions add a `Guard` power modifier to the attacked card and can trigger
  `IsBattleSupported` effects on the defended unit.

## UI Surface
- Card effect text is rendered in `ALCard.GetFormattedEffect` and `GetFormattedEffectMini`,
  using `effectLabel` and optional metadata (trigger, duration, conditions, effectId).
