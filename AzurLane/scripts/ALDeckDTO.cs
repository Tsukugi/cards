using System.Collections.Generic;

public class ALDeckDTO : BaseDTO
{
    public string name;
    public string flagship; // Flagship card id
    public Dictionary<string, int> cards; // <card id, number of copies>
    public Dictionary<string, int> cubes; // <card id, number of copies>
}
public class ALDeckSet
{
    public string name;
    public ALCardDTO flagship; // Flagship card id
    public List<ALCardDTO> deck = [];
    public List<ALCardDTO> cubeDeck = [];
    public List<ALCardDTO> retreatDeck = [];
}