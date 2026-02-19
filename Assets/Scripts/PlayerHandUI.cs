using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerHandUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject cardDisplayPanel;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CardPlacementSystem cardPlacementSystem;

    [Header("Settings")]
    [SerializeField] private bool showCards = true;

    private List<GameObject> cardUIElements = new List<GameObject>();

    void Start()
    {
        if (cardDisplayPanel != null)
            cardDisplayPanel.SetActive(showCards);
    }

    void Update()
    {
        // Toggle card display with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            showCards = !showCards;
            if (cardDisplayPanel != null)
                cardDisplayPanel.SetActive(showCards);
        }

        if (showCards && gameManager != null)
        {
            UpdateCardDisplay();
        }
    }

    void UpdateCardDisplay()
    {
        PlayerHand currentHand = gameManager.GetCurrentPlayerHand();

        if (currentHand == null)
            return;

        // Clear existing cards
        foreach (GameObject card in cardUIElements)
        {
            if (card != null)
                Destroy(card);
        }
        cardUIElements.Clear();

        // Create card UI for each card in hand
        for (int i = 0; i < currentHand.GetHandSize(); i++)
        {
            Card card = currentHand.GetCard(i);
            if (card != null)
            {
                CreateCardUI(card, i);
            }
        }
    }

    void CreateCardUI(Card card, int index)
    {
        GameObject cardUI;

        if (cardPrefab != null)
        {
            cardUI = Instantiate(cardPrefab, cardContainer);
        }
        else
        {
            // Create simple card UI if no prefab
            cardUI = new GameObject($"Card_{index}");
            cardUI.transform.SetParent(cardContainer);

            Image bg = cardUI.AddComponent<Image>();
            bg.color = GetCardColor(card.type);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(cardUI.transform);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = $"[{GetCardKey(index)}]\n{card.cardName}";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 14;
            text.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        cardUIElements.Add(cardUI);
    }

    string GetCardKey(int index)
    {
        return index == 9 ? "0" : (index + 1).ToString();
    }

    Color GetCardColor(CardType type)
    {
        switch (type)
        {
            case CardType.Lava:
                return new Color(1f, 0.3f, 0f); // Orange
            case CardType.Water:
                return new Color(0f, 0.5f, 1f); // Blue
            case CardType.Block:
                return new Color(0.5f, 0.5f, 0.5f); // Gray
            case CardType.WindDirection:
                return new Color(0.8f, 1f, 0.8f); // Light green
            default:
                return Color.white;
        }
    }
}
