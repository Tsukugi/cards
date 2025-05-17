using System.Collections.Generic;

public class PlayState
{
    public EPlayState state = EPlayState.Wait; // Play state refers to the Player actions and what they can do via input --- Pressing Cancel, OK or Waiting for something
    public string interactionState = "None"; // Interaction state refers to what specifically each player action should be attached to --- Pressing OK is to play a card in the main phase or to select a target for an effect
}

public class PlayStateManager
{
    PlayState currentPlayState = new();
    List<PlayState> history = new();

    public void SetPlayState(PlayState newState)
    {
        if (newState.state == currentPlayState.state && newState.interactionState == currentPlayState.interactionState) return;
        history.Add(currentPlayState);
        currentPlayState = newState;
    }
    public PlayState GetPlayState() => currentPlayState;

    public void GoBackInHistory()
    {
        var last = history[history.Count - 1];
        currentPlayState = last;
        history.Remove(last);
    }
}