using UnityEngine;
using System.Collections.Generic;

public class CardPlacementSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GridManager gridManager;

    [Header("Preview Settings")]
    [SerializeField] private Material previewValidMaterial;
    [SerializeField] private Material previewInvalidMaterial;
    [SerializeField] private GameObject previewPrefab;

    private Card selectedCard = null;
    private int selectedCardIndex = -1;
    private List<GameObject> previewObjects = new List<GameObject>();
    private List<int> selectedTiles = new List<int>();
    private bool isPlacingCard = false;

    // Pour la rotation du pattern
    private int rotationIndex = 0; // 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°

    void Update()
    {
        // Debug pour voir l'état
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"🔍 DEBUG État:");
            Debug.Log($"  isPlacingCard: {isPlacingCard}");
            Debug.Log($"  selectedCard: {(selectedCard != null ? selectedCard.cardName : "NULL")}");
            Debug.Log($"  selectedCardIndex: {selectedCardIndex}");
        }

        if (isPlacingCard && selectedCard != null)
        {
            UpdatePreview();

            // Q pour faire pivoter le pattern
            if (Input.GetKeyDown(KeyCode.Q))
            {
                rotationIndex = (rotationIndex + 1) % 4;
                Debug.Log($"🔄 Rotation: {rotationIndex * 90}°");
                // Forcer la mise à jour du preview après rotation
                ClearPreviews();
                selectedTiles.Clear();
            }

            // Clic gauche pour confirmer
            if (Input.GetMouseButtonDown(0))
            {
                ConfirmPlacement();
            }

            // Clic droit pour annuler
            if (Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
            }
        }
    }

    public void StartPlacingCard(int cardIndex)
    {
        Debug.Log($"🎯 StartPlacingCard appelé avec index {cardIndex}");

        PlayerHand hand = gameManager.GetCurrentPlayerHand();

        Debug.Log($"  Main du joueur a {hand.GetHandSize()} cartes");

        if (cardIndex >= hand.GetHandSize())
        {
            Debug.LogWarning("Carte invalide!");
            return;
        }

        selectedCard = hand.GetCard(cardIndex);
        selectedCardIndex = cardIndex;
        isPlacingCard = true;
        rotationIndex = 0;

        Debug.Log($"🎴 Mode placement ACTIVÉ: {selectedCard.cardName}");
        Debug.Log($"  ✓ isPlacingCard = {isPlacingCard}");
        Debug.Log($"  ✓ selectedCard = {selectedCard.cardName}");
        Debug.Log($"  ✓ selectedCardIndex = {selectedCardIndex}");
        Debug.Log("📍 Bougez la souris sur la grille pour voir le preview...");
        Debug.Log("Q: Faire pivoter | Clic gauche: Confirmer | Clic droit: Annuler | P: Debug état");
    }

    void UpdatePreview()
    {
        // Obtenir la position de la souris
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            TileData hoveredTile = hit.collider.GetComponent<TileData>();
            if (hoveredTile != null)
            {
                // Calculer les tiles selon le pattern
                List<int> newSelectedTiles = GetPatternTiles(hoveredTile.tileNumber);

                // Seulement recréer les previews si les tiles ont changé
                if (!TilesAreEqual(newSelectedTiles, selectedTiles))
                {
                    selectedTiles = newSelectedTiles;

                    // Nettoyer les anciens previews
                    ClearPreviews();

                    // Vérifier si le placement est valide
                    bool isValid = ValidatePlacement(selectedTiles);

                    // Créer les nouveaux previews
                    foreach (int tileNum in selectedTiles)
                    {
                        GameObject tile = gridManager.GetTileByNumber(tileNum);
                        if (tile != null)
                        {
                            GameObject preview = Instantiate(previewPrefab, tile.transform.position + Vector3.up * 0.5f, Quaternion.identity);

                            Renderer renderer = preview.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material = isValid ? previewValidMaterial : previewInvalidMaterial;
                            }

                            previewObjects.Add(preview);
                        }
                    }
                }
            }
        }
        else
        {
            // Souris hors de la grille - nettoyer les previews
            if (previewObjects.Count > 0)
            {
                ClearPreviews();
                selectedTiles.Clear();
            }
        }
    }

    bool TilesAreEqual(List<int> list1, List<int> list2)
    {
        if (list1.Count != list2.Count)
            return false;

        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i] != list2[i])
                return false;
        }

        return true;
    }

    List<int> GetPatternTiles(int baseTileNumber)
    {
        List<int> tiles = new List<int>();
        TileData baseTile = gridManager.GetTileDataByNumber(baseTileNumber);

        if (baseTile == null)
            return tiles;

        int x = baseTile.coordinates.x;
        int y = baseTile.coordinates.y;

        switch (selectedCard.pattern)
        {
            case CardPattern.Line3:
                tiles = GetLine3Pattern(x, y);
                break;

            case CardPattern.Square2x2:
                tiles = GetSquare2x2Pattern(x, y);
                break;

            case CardPattern.TwoAdjacent:
                tiles = GetTwoAdjacentPattern(x, y);
                break;

            case CardPattern.OneSpaceOne:
                tiles = GetOneSpaceOnePattern(x, y);
                break;
        }

        return tiles;
    }

    List<int> GetLine3Pattern(int x, int y)
    {
        List<int> tiles = new List<int>();

        // Selon la rotation
        switch (rotationIndex)
        {
            case 0: // Horizontal vers la droite
                tiles.Add(GetTileNumber(x, y));
                tiles.Add(GetTileNumber(x + 1, y));
                tiles.Add(GetTileNumber(x + 2, y));
                break;

            case 1: // Vertical vers le haut
                tiles.Add(GetTileNumber(x, y));
                tiles.Add(GetTileNumber(x, y + 1));
                tiles.Add(GetTileNumber(x, y + 2));
                break;

            case 2: // Horizontal vers la gauche
                tiles.Add(GetTileNumber(x, y));
                tiles.Add(GetTileNumber(x - 1, y));
                tiles.Add(GetTileNumber(x - 2, y));
                break;

            case 3: // Vertical vers le bas
                tiles.Add(GetTileNumber(x, y));
                tiles.Add(GetTileNumber(x, y - 1));
                tiles.Add(GetTileNumber(x, y - 2));
                break;
        }

        return tiles;
    }

    List<int> GetSquare2x2Pattern(int x, int y)
    {
        List<int> tiles = new List<int>();

        tiles.Add(GetTileNumber(x, y));
        tiles.Add(GetTileNumber(x + 1, y));
        tiles.Add(GetTileNumber(x, y + 1));
        tiles.Add(GetTileNumber(x + 1, y + 1));

        return tiles;
    }

    List<int> GetTwoAdjacentPattern(int x, int y)
    {
        List<int> tiles = new List<int>();

        switch (rotationIndex % 2) // Horizontal ou Vertical
        {
            case 0: // Horizontal
                tiles.Add(GetTileNumber(x, y));
                tiles.Add(GetTileNumber(x + 1, y));
                break;

            case 1: // Vertical
                tiles.Add(GetTileNumber(x, y));
                tiles.Add(GetTileNumber(x, y + 1));
                break;
        }

        return tiles;
    }

    List<int> GetOneSpaceOnePattern(int x, int y)
    {
        List<int> tiles = new List<int>();

        switch (rotationIndex % 2)
        {
            case 0: // Horizontal
                tiles.Add(GetTileNumber(x, y));
                tiles.Add(GetTileNumber(x + 2, y)); // Un espace entre
                break;

            case 1: // Vertical
                tiles.Add(GetTileNumber(x, y));
                tiles.Add(GetTileNumber(x, y + 2));
                break;
        }

        return tiles;
    }

    int GetTileNumber(int x, int y)
    {
        GameObject tile = gridManager.GetTileByCoordinates(x, y);
        if (tile != null)
        {
            TileData data = tile.GetComponent<TileData>();
            return data.tileNumber;
        }
        return -1;
    }

    bool ValidatePlacement(List<int> tiles)
    {
        // Vérifier qu'aucune tile n'est -1 (hors grille)
        if (tiles.Contains(-1))
            return false;

        switch (selectedCard.type)
        {
            case CardType.Lava:
                return ValidateLavaPlacement(tiles);

            case CardType.Water:
                return ValidateWaterPlacement(tiles);

            case CardType.Block:
                return ValidateBlockPlacement(tiles);

            default:
                return false;
        }
    }

    bool ValidateLavaPlacement(List<int> tiles)
    {
        // Au moins UNE tile du pattern doit toucher une lave existante OU un bloque
        bool atLeastOneTouchesExistingLava = false;

        // Compter combien de tiles sont placables
        int placeableTiles = 0;
        List<int> placeableTilesList = new List<int>();

        foreach (int tileNum in tiles)
        {
            // Si la tile est hors grille (-1), on l'ignore
            if (tileNum == -1)
                continue;

            // Si c'est un bloque, on l'accepte dans le pattern mais on ne le compte pas comme plaçable
            if (gameManager.IsTileBlock(tileNum))
            {
                // Vérifier si le bloque est à côté d'une lave
                if (IsAdjacentToLava(tileNum))
                {
                    atLeastOneTouchesExistingLava = true;
                }
                continue;
            }

            // Vérifier que la tile n'est pas déjà de la lave
            if (gameManager.IsTileLava(tileNum))
            {
                continue; // On ignore les laves existantes
            }

            placeableTiles++;
            placeableTilesList.Add(tileNum);

            // Vérifier si cette tile touche une lave EXISTANTE
            if (IsAdjacentToLava(tileNum))
            {
                atLeastOneTouchesExistingLava = true;
            }
        }

        // Si aucune tile n'est plaçable, invalide
        if (placeableTiles == 0)
            return false;

        // Si aucune tile ne touche une lave existante, invalide
        if (!atLeastOneTouchesExistingLava)
            return false;

        // Vérifier la connectivité seulement pour les tiles placables
        if (placeableTilesList.Count == 0)
            return false;

        return AreAllTilesConnected(placeableTilesList);
    }

    bool ValidateBlockPlacement(List<int> tiles)
    {
        int placeableTiles = 0;
        List<int> placeableTilesList = new List<int>();

        foreach (int tileNum in tiles)
        {
            // Ignorer les tiles hors grille
            if (tileNum == -1)
                continue;

            // Ne peut pas placer sur une case déjà occupée
            if (gameManager.IsTileOccupied(tileNum))
                continue;

            placeableTiles++;
            placeableTilesList.Add(tileNum);
        }

        // Il faut au moins une case libre
        if (placeableTiles == 0)
            return false;

        return AreAllTilesConnected(placeableTilesList);
    }

    // Vérifier que les tiles sont connectées (pas de diagonale)
    bool AreAllTilesConnected(List<int> tiles)
    {
        if (tiles.Count == 0)
            return false;

        if (tiles.Count == 1)
            return true;

        // Filtrer pour ne garder que les tiles qui existent vraiment (pas -1)
        List<int> validTiles = new List<int>();
        foreach (int tileNum in tiles)
        {
            if (tileNum != -1)
            {
                validTiles.Add(tileNum);
            }
        }

        if (validTiles.Count == 0)
            return false;

        if (validTiles.Count == 1)
            return true;

        // Utiliser un algorithme de flood-fill pour vérifier la connectivité
        HashSet<int> visited = new HashSet<int>();
        Queue<int> toVisit = new Queue<int>();

        toVisit.Enqueue(validTiles[0]);
        visited.Add(validTiles[0]);

        while (toVisit.Count > 0)
        {
            int current = toVisit.Dequeue();
            TileData currentData = gridManager.GetTileDataByNumber(current);

            if (currentData == null)
                continue;

            int x = currentData.coordinates.x;
            int y = currentData.coordinates.y;

            // Vérifier les 4 voisins adjacents (pas diagonale)
            Vector2Int[] adjacents = new Vector2Int[]
            {
            new Vector2Int(x + 1, y),
            new Vector2Int(x - 1, y),
            new Vector2Int(x, y + 1),
            new Vector2Int(x, y - 1)
            };

            foreach (Vector2Int adj in adjacents)
            {
                GameObject tile = gridManager.GetTileByCoordinates(adj.x, adj.y);
                if (tile != null)
                {
                    TileData adjData = tile.GetComponent<TileData>();

                    // Si ce voisin fait partie du pattern ET est valide ET n'a pas été visité
                    if (validTiles.Contains(adjData.tileNumber) && !visited.Contains(adjData.tileNumber))
                    {
                        visited.Add(adjData.tileNumber);
                        toVisit.Enqueue(adjData.tileNumber);
                    }
                }
            }
        }

        // Toutes les tiles VALIDES doivent avoir été visitées
        return visited.Count == validTiles.Count;
    }

    bool ValidateWaterPlacement(List<int> tiles)
    {
        // L'eau peut être placée N'IMPORTE OÙ
        // Pas de vérification de connectivité pour l'eau
        // Pas de vérification si occupé ou non

        int validTiles = 0;

        foreach (int tileNum in tiles)
        {
            // Ignorer seulement les tiles hors grille
            if (tileNum == -1)
                continue;

            validTiles++;
        }

        // Il faut au moins une tile dans la grille
        return validTiles > 0;
    }

    bool IsAdjacentToLava(int tileNumber)
    {
        TileData tileData = gridManager.GetTileDataByNumber(tileNumber);
        if (tileData == null)
            return false;

        int x = tileData.coordinates.x;
        int y = tileData.coordinates.y;

        Vector2Int[] adjacents = new Vector2Int[]
        {
            new Vector2Int(x + 1, y),
            new Vector2Int(x - 1, y),
            new Vector2Int(x, y + 1),
            new Vector2Int(x, y - 1)
        };

        foreach (Vector2Int adj in adjacents)
        {
            GameObject tile = gridManager.GetTileByCoordinates(adj.x, adj.y);
            if (tile != null)
            {
                TileData data = tile.GetComponent<TileData>();
                // Vérifier si c'est de la LAVE, pas juste occupé
                if (gameManager.IsTileLava(data.tileNumber))
                    return true;
            }
        }

        return false;
    }

    void ConfirmPlacement()
    {
        Debug.Log($"🎯 Tentative de confirmer placement de {selectedTiles.Count} tiles");

        if (selectedTiles.Count > 0 && ValidatePlacement(selectedTiles))
        {
            Debug.Log("✅ Placement validé, appel de PlayCardFromHand...");

            // Sauvegarder les tiles avant de nettoyer
            List<int> tilesToPlace = new List<int>(selectedTiles);
            int cardIndex = selectedCardIndex;

            // Nettoyer AVANT de jouer la carte
            ClearPreviews();
            selectedCard = null;
            selectedCardIndex = -1;
            isPlacingCard = false;
            selectedTiles.Clear();
            rotationIndex = 0;

            Debug.Log("🎴 Carte confirmée, envoi au GameManager...");

            // Maintenant jouer la carte
            gameManager.PlayCardFromHand(cardIndex, tilesToPlace);
        }
        else
        {
            Debug.LogWarning("❌ Placement invalide!");
            Debug.LogWarning($"  Tiles count: {selectedTiles.Count}");
            Debug.LogWarning($"  Validation: {ValidatePlacement(selectedTiles)}");
        }
    }

    void CancelPlacement()
    {
        ClearPreviews();
        selectedCard = null;
        selectedCardIndex = -1;
        isPlacingCard = false;
        selectedTiles.Clear();
        rotationIndex = 0;

        Debug.Log("❌ Placement annulé par le joueur");
    }

    void ClearPreviews()
    {
        foreach (GameObject preview in previewObjects)
        {
            if (preview != null)
                Destroy(preview);
        }
        previewObjects.Clear();
    }

    public bool IsPlacingCard()
    {
        return isPlacingCard;
    }
}