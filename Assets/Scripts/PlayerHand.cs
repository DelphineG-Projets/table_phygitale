using UnityEngine;
using System.Collections.Generic;

public class PlayerHand : MonoBehaviour
{
    public int playerNumber;
    public List<Card> cardsInHand = new List<Card>();

    // NOUVELLE méthode - celle qu'on utilise maintenant
    public void InitializeAllCards()
    {
        cardsInHand.Clear();

        // Cartes Lave
        cardsInHand.Add(new Card(CardType.Lava, CardPattern.Line3));
        cardsInHand.Add(new Card(CardType.Lava, CardPattern.Square2x2));

        // Cartes Eau
        cardsInHand.Add(new Card(CardType.Water, CardPattern.Line3));
        cardsInHand.Add(new Card(CardType.Water, CardPattern.Square2x2));

        // Cartes Bloque
        cardsInHand.Add(new Card(CardType.Block, CardPattern.TwoAdjacent));
        cardsInHand.Add(new Card(CardType.Block, CardPattern.OneSpaceOne));

        // Cartes Direction du Vent
        cardsInHand.Add(new Card(CardType.WindDirection, CardPattern.North));
        cardsInHand.Add(new Card(CardType.WindDirection, CardPattern.South));
        cardsInHand.Add(new Card(CardType.WindDirection, CardPattern.East));
        cardsInHand.Add(new Card(CardType.WindDirection, CardPattern.West));

        Debug.Log($"Joueur {playerNumber} a accès à {cardsInHand.Count} cartes (liste complète)");
    }

    // ANCIENNE méthode - garde-la au cas où, mais on ne l'utilise plus
    public void DrawInitialHand(DeckManager deckManager)
    {
        cardsInHand.Clear();

        for (int i = 0; i < 5; i++)
        {
            Card card = deckManager.DrawCard();
            if (card != null)
            {
                cardsInHand.Add(card);
            }
        }

        Debug.Log($"Joueur {playerNumber} a pioché {cardsInHand.Count} cartes");
    }

    public void DrawCard(DeckManager deckManager)
    {
        if (cardsInHand.Count >= 5)
        {
            Debug.LogWarning($"Joueur {playerNumber} a déjà 5 cartes!");
            return;
        }

        Card card = deckManager.DrawCard();
        if (card != null)
        {
            cardsInHand.Add(card);
            Debug.Log($"Joueur {playerNumber} pioche: {card.cardName} ({cardsInHand.Count}/5)");
        }
    }

    public Card GetCard(int index)
    {
        if (index >= 0 && index < cardsInHand.Count)
            return cardsInHand[index];
        return null;
    }

    public int GetHandSize()
    {
        return cardsInHand.Count;
    }

    public bool PlayCard(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= cardsInHand.Count)
        {
            Debug.LogWarning("Index de carte invalide!");
            return false;
        }

        Card playedCard = cardsInHand[cardIndex];
        Debug.Log($"Joueur {playerNumber} joue: {playedCard.cardName}");

        // Les cartes ne se consomment pas
        return true;
    }
}