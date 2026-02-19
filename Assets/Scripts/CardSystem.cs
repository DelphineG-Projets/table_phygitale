using UnityEngine;
using System.Collections.Generic;

public enum CardType
{
    Lava,
    Water,
    Block,
    WindDirection
}

public enum CardPattern
{
    // Pour Lave et Eau
    Line3,      // Ligne de 3
    Square2x2,  // Carré 2x2

    // Pour Bloque
    TwoAdjacent,    // 2 collés
    OneSpaceOne,    // 1 vide 1

    // Pour Direction
    North,
    South,
    East,
    West
}

public enum WindDirection
{
    North,
    South,
    East,
    West
}

[System.Serializable]
public class Card
{
    public CardType type;
    public CardPattern pattern;
    public string cardName;
    public Sprite cardImage; // Optionnel : pour l'UI

    public Card(CardType cardType, CardPattern cardPattern)
    {
        type = cardType;
        pattern = cardPattern;
        cardName = $"{cardType} - {cardPattern}";
    }
}