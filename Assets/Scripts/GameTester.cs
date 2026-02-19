using UnityEngine;

public class GameTester : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CardPlacementSystem cardPlacementSystem;

    private enum TurnPhase
    {
        PlacingMandatoryLava,
        PlayingCard
    }

    private TurnPhase currentPhase = TurnPhase.PlacingMandatoryLava;

    void Update()
    {
        // Vérification de sécurité
        if (cardPlacementSystem == null)
        {
            Debug.LogError("❌ CardPlacementSystem n'est pas assigné dans GameTester!");
            return;
        }

        // H pour l'aide
        if (Input.GetKeyDown(KeyCode.H))
        {
            ShowHelp();
        }

        // Phase 1 : Placer la lave obligatoire
        if (currentPhase == TurnPhase.PlacingMandatoryLava && !cardPlacementSystem.IsPlacingCard())
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 1000f))
                {
                    TileData tileData = hit.collider.GetComponent<TileData>();
                    if (tileData != null)
                    {
                        bool success = gameManager.PlaceMandatoryLava(tileData.tileNumber);

                        if (success)
                        {
                            Debug.Log("✅ Lave obligatoire placée ! Jouez une carte (appuyez sur H pour voir les cartes)");
                            currentPhase = TurnPhase.PlayingCard;
                        }
                    }
                }
            }
        }

        // Phase 2 : Jouer une carte (touches 1-0 pour cartes 1-10)
        if (currentPhase == TurnPhase.PlayingCard && !cardPlacementSystem.IsPlacingCard())
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryPlayCard(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryPlayCard(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryPlayCard(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TryPlayCard(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) TryPlayCard(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) TryPlayCard(5);
            if (Input.GetKeyDown(KeyCode.Alpha7)) TryPlayCard(6);
            if (Input.GetKeyDown(KeyCode.Alpha8)) TryPlayCard(7);
            if (Input.GetKeyDown(KeyCode.Alpha9)) TryPlayCard(8);
            if (Input.GetKeyDown(KeyCode.Alpha0)) TryPlayCard(9); // 0 = carte #10
        }

        // R pour reset
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("=== RESET DE LA PARTIE ===");
            gameManager.ResetGame();
            currentPhase = TurnPhase.PlacingMandatoryLava;
        }
    }

    void TryPlayCard(int cardIndex)
    {
        PlayerHand hand = gameManager.GetCurrentPlayerHand();

        if (cardIndex >= hand.GetHandSize())
        {
            Debug.LogWarning($"Pas de carte à l'index {cardIndex + 1}!");
            return;
        }

        Card card = hand.GetCard(cardIndex);

        // Si c'est une carte de vent, jouer directement
        if (card.type == CardType.WindDirection)
        {
            System.Collections.Generic.List<int> emptyList = new System.Collections.Generic.List<int>();
            gameManager.PlayCardFromHand(cardIndex, emptyList);
        }
        else
        {
            // Démarrer le mode placement pour les autres cartes
            cardPlacementSystem.StartPlacingCard(cardIndex);
        }
    }

    void ShowHelp()
    {
        Debug.Log("=== AIDE ===");
        Debug.Log($"Phase: {currentPhase}");
        Debug.Log($"Vent: {gameManager.GetCurrentWind()}");
        Debug.Log("");
        Debug.Log("PHASE 1 - Placer lave obligatoire:");
        Debug.Log("  Clic gauche: Placer selon le vent");
        Debug.Log("");
        Debug.Log("PHASE 2 - Jouer une carte:");
        Debug.Log("  Touches 1-9, 0: Sélectionner cartes 1-10");
        Debug.Log("  Q: Faire pivoter le pattern");
        Debug.Log("  Clic gauche: Confirmer placement");
        Debug.Log("  Clic droit: Annuler");
        Debug.Log("");
        Debug.Log("Autres:");
        Debug.Log("  H: Afficher cette aide");
        Debug.Log("  R: Recommencer la partie");
        Debug.Log("");

        // Afficher toutes les cartes
        PlayerHand hand = gameManager.GetCurrentPlayerHand();
        Debug.Log($"=== Main Joueur {gameManager.GetCurrentPlayer().playerNumber} ({hand.GetHandSize()} cartes) ===");
        for (int i = 0; i < hand.GetHandSize(); i++)
        {
            Card c = hand.GetCard(i);
            string key = i == 9 ? "0" : (i + 1).ToString();
            Debug.Log($"[{key}] {c.cardName}");
        }
    }

    public void ResetToMandatoryLavaPhase()
    {
        currentPhase = TurnPhase.PlacingMandatoryLava;
    }
}