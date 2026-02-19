using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    [Header("Deck Configuration")]
    [SerializeField] private int lavaLine3Count = 5;
    [SerializeField] private int lavaSquare2x2Count = 5;
    [SerializeField] private int waterLine3Count = 5;
    [SerializeField] private int waterSquare2x2Count = 5;
    [SerializeField] private int blockTwoAdjacentCount = 5;
    [SerializeField] private int blockOneSpaceOneCount = 5;
    [SerializeField] private int windNorthCount = 2;
    [SerializeField] private int windSouthCount = 2;
    [SerializeField] private int windEastCount = 2;
    [SerializeField] private int windWestCount = 2;

    private List<Card> deck = new List<Card>();
    private List<Card> discardPile = new List<Card>();

    void Start()
    {
        InitializeDeck();
        ShuffleDeck();
    }

    void InitializeDeck()
    {
        deck.Clear();

        // Cartes Lave
        for (int i = 0; i < lavaLine3Count; i++)
            deck.Add(new Card(CardType.Lava, CardPattern.Line3));
        for (int i = 0; i < lavaSquare2x2Count; i++)
            deck.Add(new Card(CardType.Lava, CardPattern.Square2x2));

        // Cartes Eau
        for (int i = 0; i < waterLine3Count; i++)
            deck.Add(new Card(CardType.Water, CardPattern.Line3));
        for (int i = 0; i < waterSquare2x2Count; i++)
            deck.Add(new Card(CardType.Water, CardPattern.Square2x2));

        // Cartes Bloque
        for (int i = 0; i < blockTwoAdjacentCount; i++)
            deck.Add(new Card(CardType.Block, CardPattern.TwoAdjacent));
        for (int i = 0; i < blockOneSpaceOneCount; i++)
            deck.Add(new Card(CardType.Block, CardPattern.OneSpaceOne));

        // Cartes Direction du Vent
        for (int i = 0; i < windNorthCount; i++)
            deck.Add(new Card(CardType.WindDirection, CardPattern.North));
        for (int i = 0; i < windSouthCount; i++)
            deck.Add(new Card(CardType.WindDirection, CardPattern.South));
        for (int i = 0; i < windEastCount; i++)
            deck.Add(new Card(CardType.WindDirection, CardPattern.East));
        for (int i = 0; i < windWestCount; i++)
            deck.Add(new Card(CardType.WindDirection, CardPattern.West));

        Debug.Log($"Deck initialisé avec {deck.Count} cartes");
    }

    public void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            Card temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }

        Debug.Log("Deck mélangé");
    }

    public Card DrawCard()
    {
        if (deck.Count == 0)
        {
            Debug.Log("Deck vide, mélange de la défausse...");
            ReshuffleDiscardPile();
        }

        if (deck.Count == 0)
        {
            Debug.LogWarning("Plus de cartes disponibles!");
            return null;
        }

        Card drawnCard = deck[0];
        deck.RemoveAt(0);
        Debug.Log($"Carte piochée: {drawnCard.cardName}");
        return drawnCard;
    }

    public void DiscardCard(Card card)
    {
        discardPile.Add(card);
    }

    void ReshuffleDiscardPile()
    {
        deck.AddRange(discardPile);
        discardPile.Clear();
        ShuffleDeck();
    }

    public int GetDeckCount()
    {
        return deck.Count;
    }
}