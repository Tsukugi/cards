# Card Selector Indicator

The selected-card indicator is a `MeshInstance3D` named `SelectedIndicator` under
`CardDisplay` in `AzurLane/AzurLaneCard.tscn`. It starts invisible and uses
`AzurLane/shader/AzurLaneCardSelect.tres` for the glow/pulse look.

## Selection Flow

- `Board.SelectCardField` clears the previous selection for the player, sets the new card,
  updates the indicator color from the player, and marks the card as selected
  (`scripts/card/Board.cs`).
- Each frame `Card._Process` calls `OnSelectHandler`, which toggles
  `SelectedIndicator.Visible` and applies the color when `isSelected` is true
  (`scripts/card/Card.cs`).
- The color comes from `Player.GetPlayerColor` via `Card.UpdatePlayerSelectedColor`, which
  updates the shader parameter named `color` on the indicator material
  (`scripts/card/Card.cs`).
- Each board tracks selection per player in its `selectedCard` dictionary, so each player
  can have one selected card per board (`scripts/card/Board.cs`).

## Tuning the Look

Edit `AzurLane/shader/AzurLaneCardSelect.tres` or the default color in
`AzurLane/AzurLaneCard.tscn`.
