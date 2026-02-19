using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameOverManager gameOverManager;

    [Header("Lava Piece")]
    [SerializeField] private GameObject lavaPiecePrefab;
    [SerializeField] private Material lavaMaterial;

    [Header("Block Piece")]
    [SerializeField] private GameObject blockPiecePrefab;
    [SerializeField] private Material blockMaterial;

    [Header("Game Settings")]
    [SerializeField] private int numberOfPlayers = 2;

    [Header("Current Game State")]
    [SerializeField] private int currentPlayerIndex = 0;
    private bool isGameOver = false;

    [Header("Card System")]
    [SerializeField] private DeckManager deckManager;
    private WindDirection currentWindDirection;
    private List<PlayerHand> playerHands = new List<PlayerHand>();
    private bool hasPlacedMandatoryLava = false;
    private bool hasPlayedCard = false;

    [Header("Player Base Settings")]
    [SerializeField] private Material baseMaterial;
    [SerializeField]
    private Color[] playerBaseColors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f),
        new Color(0.3f, 0.3f, 1f),
        new Color(0.3f, 1f, 0.3f),
        new Color(1f, 1f, 0.3f)
    };

    private Dictionary<int, Player> playerBases = new Dictionary<int, Player>();
    private List<Player> eliminatedPlayers = new List<Player>();
    private List<Player> players = new List<Player>();
    private Dictionary<int, GameObject> occupiedTiles = new Dictionary<int, GameObject>();
    private HashSet<int> lavaTiles = new HashSet<int>();
    private HashSet<int> blockTiles = new HashSet<int>();
    private HashSet<int> initialLavaTiles = new HashSet<int>();

    void Start()
    {
        InitializePlayers();
        PlacePlayerBases();
        PlaceInitialLavaPieces();
        InitializeCardSystem();
        StartGame();
    }

    void InitializePlayers()
    {
        players.Clear();
        playerHands.Clear();

        for (int i = 0; i < numberOfPlayers; i++)
        {
            Player player = new Player(i + 1, GetPlayerColor(i));
            players.Add(player);

            // Créer la main du joueur
            GameObject handObj = new GameObject($"Player{i + 1}_Hand");
            handObj.transform.SetParent(transform);
            PlayerHand hand = handObj.AddComponent<PlayerHand>();
            hand.playerNumber = i + 1;
            playerHands.Add(hand);
        }

        Debug.Log($"{numberOfPlayers} joueurs créés");
    }

    void PlacePlayerBases()
    {
        Vector2Int center = new Vector2Int(gridManager.gridWidth / 2, gridManager.gridHeight / 2);

        // Positions des bases en sens horaire autour du cercle
        // Angles: 225° (bas-gauche), 315° (bas-droite), 45° (haut-droite), 135° (haut-gauche)
        float[] angles = new float[] { 225f, 315f, 45f, 135f };
        float baseDistance = 7f; // Distance du centre (ajuste selon ton rayon)

        for (int i = 0; i < numberOfPlayers && i < angles.Length; i++)
        {
            float angleRad = angles[i] * Mathf.Deg2Rad;

            int x = Mathf.RoundToInt(center.x + Mathf.Cos(angleRad) * baseDistance);
            int y = Mathf.RoundToInt(center.y + Mathf.Sin(angleRad) * baseDistance);

            // S'assurer que la position est dans les limites
            x = Mathf.Clamp(x, 0, gridManager.gridWidth - 1);
            y = Mathf.Clamp(y, 0, gridManager.gridHeight - 1);

            Vector2Int pos = new Vector2Int(x, y);
            GameObject tile = gridManager.GetTileByCoordinates(pos.x, pos.y);

            if (tile != null)
            {
                TileData tileData = tile.GetComponent<TileData>();
                playerBases.Add(tileData.tileNumber, players[i]);

                Renderer renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = baseMaterial != null ? new Material(baseMaterial) : new Material(renderer.sharedMaterial);
                    mat.color = playerBaseColors[i];
                    renderer.material = mat;
                }

                Debug.Log($"Base du Joueur {players[i].playerNumber} placée en ({pos.x}, {pos.y})");
            }
            else
            {
                Debug.LogWarning($"Impossible de placer la base du joueur {i + 1} à ({x}, {y})");
            }
        }
    }

    Color GetPlayerColor(int index)
    {
        Color[] colors = new Color[]
        {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow
        };

        return colors[index % colors.Length];
    }

    void PlaceInitialLavaPieces()
    {
        int centerX = gridManager.gridWidth / 2;
        int centerY = gridManager.gridHeight / 2;

        Vector2Int[] centerPositions = new Vector2Int[]
        {
        new Vector2Int(centerX - 1, centerY - 1),
        new Vector2Int(centerX, centerY - 1),
        new Vector2Int(centerX - 1, centerY),
        new Vector2Int(centerX, centerY)
        };

        foreach (Vector2Int pos in centerPositions)
        {
            GameObject tile = gridManager.GetTileByCoordinates(pos.x, pos.y);
            if (tile != null)
            {
                TileData tileData = tile.GetComponent<TileData>();
                PlaceLavaPiece(tileData.tileNumber, tile.transform.position);

                // MARQUER COMME LAVE INITIALE PROTÉGÉE
                initialLavaTiles.Add(tileData.tileNumber);
            }
        }

        Debug.Log($"4 lava_pieces initiales placées au centre de la grille (protégées)");
    }

    void PlaceLavaPiece(int tileNumber, Vector3 position)
    {
        GameObject lavaPiece = Instantiate(lavaPiecePrefab, position + Vector3.up * 0.1f, Quaternion.identity, transform);
        lavaPiece.name = $"lava_piece_{tileNumber}";

        if (lavaMaterial != null)
        {
            Renderer renderer = lavaPiece.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = lavaMaterial;
            }
        }

        // DÉSACTIVER LE COLLIDER pour qu'il ne bloque pas les raycast
        Collider collider = lavaPiece.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        occupiedTiles.Add(tileNumber, lavaPiece);
        lavaTiles.Add(tileNumber);
    }

    void PlaceBlockPiece(int tileNumber, Vector3 position)
    {
        GameObject blockPiece = Instantiate(blockPiecePrefab, position + Vector3.up * 0.1f, Quaternion.identity, transform);
        blockPiece.name = $"block_piece_{tileNumber}";

        if (blockMaterial != null)
        {
            Renderer renderer = blockPiece.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = blockMaterial;
            }
        }

        // Ajouter le composant BlockPiece
        BlockPiece blockData = blockPiece.GetComponent<BlockPiece>();
        if (blockData == null)
            blockData = blockPiece.AddComponent<BlockPiece>();

        blockData.tileNumber = tileNumber;

        // DÉSACTIVER LE COLLIDER
        Collider collider = blockPiece.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        occupiedTiles.Add(tileNumber, blockPiece);
        blockTiles.Add(tileNumber);

        Debug.Log($"🚧 Bloque placé sur tile {tileNumber}");
    }

    public void PlaceInitialLavaPiecesInEditor()
    {
#if UNITY_EDITOR
        PlaceInitialLavaPieces();
#endif
    }

    void StartGame()
    {
        currentPlayerIndex = 0;
        isGameOver = false;
        Debug.Log($"=== DÉBUT DE LA PARTIE ===");
        Debug.Log($"Au tour du Joueur {GetCurrentPlayer().playerNumber}");

        // AJOUTER CETTE LIGNE
        HighlightCurrentPlayerBase();
    }

    public WindDirection GetCurrentWind()
    {
        return currentWindDirection;
    }

    public void SetWindDirection(WindDirection newDirection)
    {
        currentWindDirection = newDirection;
        Debug.Log($"🌬️ Nouvelle direction du vent: {currentWindDirection}");
    }

    public PlayerHand GetCurrentPlayerHand()
    {
        return playerHands[currentPlayerIndex];
    }

    public bool CanPlaceMandatoryLava(int tileNumber)
    {
        if (hasPlacedMandatoryLava)
        {
            Debug.LogWarning("Vous avez déjà placé votre lave obligatoire ce tour!");
            return false;
        }

        GameObject tile = gridManager.GetTileByNumber(tileNumber);
        if (tile == null)
        {
            Debug.LogWarning("Ce tile n'existe pas!");
            return false;
        }

        if (occupiedTiles.ContainsKey(tileNumber))
        {
            Debug.LogWarning("Ce tile est déjà occupé!");
            return false;
        }

        if (!IsInWindDirection(tileNumber))
        {
            Debug.LogWarning($"Vous devez placer votre lave dans la direction du vent: {currentWindDirection}");
            return false;
        }

        return true;
    }

    bool IsInWindDirection(int tileNumber)
    {
        TileData tileData = gridManager.GetTileDataByNumber(tileNumber);
        if (tileData == null)
            return false;

        int x = tileData.coordinates.x;
        int y = tileData.coordinates.y;

        Vector2Int windOffset = Vector2Int.zero;

        switch (currentWindDirection)
        {
            case WindDirection.North:
                windOffset = new Vector2Int(0, 1);
                break;
            case WindDirection.South:
                windOffset = new Vector2Int(0, -1);
                break;
            case WindDirection.East:
                windOffset = new Vector2Int(1, 0);
                break;
            case WindDirection.West:
                windOffset = new Vector2Int(-1, 0);
                break;
        }

        GameObject adjacentTile = gridManager.GetTileByCoordinates(x - windOffset.x, y - windOffset.y);
        if (adjacentTile != null)
        {
            TileData adjacentData = adjacentTile.GetComponent<TileData>();
            if (lavaTiles.Contains(adjacentData.tileNumber))
            {
                return true;
            }
        }

        return false;
    }

    public bool PlaceMandatoryLava(int tileNumber)
    {
        if (!CanPlaceMandatoryLava(tileNumber))
            return false;

        GameObject tile = gridManager.GetTileByNumber(tileNumber);
        PlaceLavaPiece(tileNumber, tile.transform.position);

        hasPlacedMandatoryLava = true;
        Debug.Log($"✅ Joueur {GetCurrentPlayer().playerNumber} a placé sa lave obligatoire (direction: {currentWindDirection})");

        CheckBaseElimination(tileNumber);

        if (CheckGameOver())
        {
            return true;
        }

        return true;
    }

    public bool CanPlayCard()
    {
        if (!hasPlacedMandatoryLava)
        {
            Debug.LogWarning("⚠️ Vous devez d'abord placer votre lave obligatoire!");
            return false;
        }

        if (hasPlayedCard)
        {
            Debug.LogWarning("Vous avez déjà joué une carte ce tour!");
            return false;
        }

        return true;
    }

    public void PlayCardFromHand(int cardIndex, List<int> targetTiles)
    {
        Debug.Log($"🎴 PlayCardFromHand appelé - cardIndex: {cardIndex}, tiles: {targetTiles.Count}");

        if (!CanPlayCard())
            return;

        PlayerHand currentHand = GetCurrentPlayerHand();
        Card card = currentHand.GetCard(cardIndex);

        if (card == null)
        {
            Debug.LogWarning("Carte invalide!");
            return;
        }

        Debug.Log($"📋 Carte sélectionnée: {card.cardName}");

        bool success = ApplyCardEffect(card, targetTiles);

        if (success)
        {
            currentHand.PlayCard(cardIndex);
            hasPlayedCard = true;

            Debug.Log($"✅ Joueur {GetCurrentPlayer().playerNumber} a joué: {card.cardName}");

            if (!CheckGameOver())
            {
                Debug.Log("🔄 Appel de EndTurn()...");
                EndTurn();

                GameTester tester = FindObjectOfType<GameTester>();
                if (tester != null)
                {
                    tester.ResetToMandatoryLavaPhase();
                }
            }
            else
            {
                Debug.Log("🏆 Game Over détecté, pas de EndTurn()");
            }
        }
        else
        {
            Debug.LogWarning("❌ ApplyCardEffect a échoué!");
        }
    }

    bool ApplyCardEffect(Card card, List<int> targetTiles)
    {
        switch (card.type)
        {
            case CardType.Lava:
                return PlayLavaCard(card.pattern, targetTiles);

            case CardType.Water:
                return PlayWaterCard(card.pattern, targetTiles);

            case CardType.Block:
                return PlayBlockCard(card.pattern, targetTiles);

            case CardType.WindDirection:
                return PlayWindCard(card.pattern);

            default:
                return false;
        }
    }

    bool PlayLavaCard(CardPattern pattern, List<int> targetTiles)
    {
        int lavasPlaced = 0;

        foreach (int tileNumber in targetTiles)
        {
            GameObject tile = gridManager.GetTileByNumber(tileNumber);

            // Vérifier si la tile existe
            if (tile == null)
                continue;

            // Si c'est un bloque, on le saute
            if (IsTileBlock(tileNumber))  // CHANGÉ ICI - pas gameManager.
            {
                Debug.Log($"⚠️ Tile {tileNumber} est un bloque, on le saute");
                continue;
            }

            // Si la tile est occupée par autre chose (lave existante), on le saute aussi
            if (occupiedTiles.ContainsKey(tileNumber))
            {
                Debug.Log($"⚠️ Tile {tileNumber} est déjà occupé, on le saute");
                continue;
            }

            // Placer la lave
            PlaceLavaPiece(tileNumber, tile.transform.position);
            CheckBaseElimination(tileNumber);
            lavasPlaced++;
        }

        Debug.Log($"🔥 {lavasPlaced} lave(s) placée(s) (certaines cases peuvent avoir été sautées)");

        // Retourner true si au moins une lave a été placée
        return lavasPlaced > 0;
    }

    bool PlayWaterCard(CardPattern pattern, List<int> targetTiles)
    {
        int waterPlaced = 0;

        foreach (int tileNumber in targetTiles)
        {
            GameObject tile = gridManager.GetTileByNumber(tileNumber);
            if (tile == null)
                continue;

            if (IsTileBlock(tileNumber))
            {
                Debug.Log($"⚠️ Tile {tileNumber} est un bloque, l'eau ne le touche pas");
                continue;
            }

            // VÉRIFIER SI C'EST UNE LAVE INITIALE PROTÉGÉE
            if (initialLavaTiles.Contains(tileNumber))
            {
                Debug.LogWarning($"🛡️ Tile {tileNumber} est une lave initiale protégée, l'eau ne peut pas l'enlever!");
                continue;
            }

            if (lavaTiles.Contains(tileNumber))
            {
                if (occupiedTiles.ContainsKey(tileNumber))
                {
                    Destroy(occupiedTiles[tileNumber]);
                    occupiedTiles.Remove(tileNumber);
                }

                lavaTiles.Remove(tileNumber);
                Debug.Log($"💧 Lave enlevée du tile {tileNumber}");
                waterPlaced++;
            }
            else if (!occupiedTiles.ContainsKey(tileNumber))
            {
                Debug.Log($"💧 Eau placée sur tile vide {tileNumber} (pas d'effet)");
                waterPlaced++;
            }
        }

        Debug.Log($"💧 {waterPlaced} action(s) d'eau effectuée(s)");
        return waterPlaced > 0;
    }

    bool PlayBlockCard(CardPattern pattern, List<int> targetTiles)
    {
        foreach (int tileNumber in targetTiles)
        {
            GameObject tile = gridManager.GetTileByNumber(tileNumber);
            if (tile != null && !occupiedTiles.ContainsKey(tileNumber))
            {
                PlaceBlockPiece(tileNumber, tile.transform.position);
            }
        }

        Debug.Log($"🚧 {targetTiles.Count} bloque(s) placé(s)");
        return true;
    }

    bool PlayWindCard(CardPattern pattern)
    {
        WindDirection newDirection = WindDirection.North;

        switch (pattern)
        {
            case CardPattern.North:
                newDirection = WindDirection.North;
                break;
            case CardPattern.South:
                newDirection = WindDirection.South;
                break;
            case CardPattern.East:
                newDirection = WindDirection.East;
                break;
            case CardPattern.West:
                newDirection = WindDirection.West;
                break;
        }

        SetWindDirection(newDirection);
        return true;
    }

    public void EndTurn()
    {
        if (!hasPlacedMandatoryLava)
        {
            Debug.LogWarning("⚠️ Vous devez placer votre lave obligatoire avant de finir le tour!");
            return;
        }

        if (!hasPlayedCard)
        {
            Debug.LogWarning("⚠️ Vous devez jouer une carte avant de finir le tour!");
            return;
        }

        hasPlacedMandatoryLava = false;
        hasPlayedCard = false;

        NextTurn();
    }

    void InitializeCardSystem()
    {
        currentWindDirection = (WindDirection)Random.Range(0, 4);
        Debug.Log($"🌬️ Direction du vent initiale: {currentWindDirection}");

        foreach (PlayerHand hand in playerHands)
        {
            hand.InitializeAllCards();
        }
    }

    public Player GetCurrentPlayer()
    {
        return players[currentPlayerIndex];
    }

    void CheckBaseElimination(int tileNumber)
    {
        if (playerBases.ContainsKey(tileNumber))
        {
            Player eliminatedPlayer = playerBases[tileNumber];

            if (!eliminatedPlayers.Contains(eliminatedPlayer))
            {
                eliminatedPlayers.Add(eliminatedPlayer);
                Debug.Log($"💀 JOUEUR {eliminatedPlayer.playerNumber} A ÉTÉ ÉLIMINÉ! Sa base a été recouverte de lave!");
            }
        }
    }

    void HighlightCurrentPlayerBase()
    {
        // Réinitialiser toutes les bases à leur couleur normale
        foreach (var baseEntry in playerBases)
        {
            int tileNumber = baseEntry.Key;
            Player player = baseEntry.Value;

            GameObject tile = gridManager.GetTileByNumber(tileNumber);
            if (tile != null)
            {
                Renderer renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(renderer.sharedMaterial);

                    // Trouver l'index du joueur
                    int playerIndex = player.playerNumber - 1;

                    // Couleur normale ou brillante selon si c'est le joueur actuel
                    if (player.playerNumber == GetCurrentPlayer().playerNumber)
                    {
                        // Rendre plus clair/brillant pour le joueur actuel
                        Color brightColor = playerBaseColors[playerIndex];
                        brightColor = Color.Lerp(brightColor, Color.white, 0.5f); // 50% plus clair
                        mat.color = brightColor;

                        // Optionnel: ajouter de l'émission pour un effet brillant
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", playerBaseColors[playerIndex] * 0.5f);
                    }
                    else
                    {
                        // Couleur normale pour les autres joueurs
                        mat.color = playerBaseColors[playerIndex];
                        mat.DisableKeyword("_EMISSION");
                    }

                    renderer.material = mat;
                }
            }
        }
    }

    bool CheckGameOver()
    {
        int playersAlive = numberOfPlayers - eliminatedPlayers.Count;

        if (playersAlive <= 1)
        {
            isGameOver = true;

            Player winner = null;
            foreach (Player player in players)
            {
                if (!eliminatedPlayers.Contains(player))
                {
                    winner = player;
                    break;
                }
            }

            if (winner != null)
            {
                Debug.Log($"🏆 PARTIE TERMINÉE! JOUEUR {winner.playerNumber} GAGNE! 🏆");

                if (gameOverManager != null)
                {
                    Debug.Log($"✓ Appel de ShowGameOver pour Joueur {winner.playerNumber}");
                    gameOverManager.ShowGameOver(winner.playerNumber);
                }
                else
                {
                    Debug.LogError("✗ GameOverManager est NULL!");
                }
            }
            else
            {
                Debug.Log("PARTIE TERMINÉE! Égalité!");
            }

            return true;
        }

        return false;
    }

    bool IsPlayerEliminated(Player player)
    {
        return eliminatedPlayers.Contains(player);
    }

    void NextTurn()
    {
        hasPlacedMandatoryLava = false;
        hasPlayedCard = false;

        int attempts = 0;
        do
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % numberOfPlayers;
            attempts++;

            if (attempts > numberOfPlayers)
            {
                Debug.LogError("Tous les joueurs sont éliminés!");
                return;
            }
        }
        while (IsPlayerEliminated(GetCurrentPlayer()));

        Debug.Log($"--- Au tour du Joueur {GetCurrentPlayer().playerNumber} ---");
        Debug.Log($"🌬️ Direction du vent: {currentWindDirection}");

        // AJOUTER CETTE LIGNE
        HighlightCurrentPlayerBase();
    }

    public bool IsTileOccupied(int tileNumber)
    {
        return occupiedTiles.ContainsKey(tileNumber);
    }

    public bool IsTileLava(int tileNumber)
    {
        return lavaTiles.Contains(tileNumber);
    }

    public bool IsTileBlock(int tileNumber)
    {
        return blockTiles.Contains(tileNumber);
    }

    public void ResetGame()
    {
        Debug.Log("🔄 RESET COMPLET DU JEU");

        hasPlacedMandatoryLava = false;
        hasPlayedCard = false;
        isGameOver = false;

        foreach (var kvp in occupiedTiles)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }

        occupiedTiles.Clear();
        lavaTiles.Clear();
        blockTiles.Clear();
        initialLavaTiles.Clear(); // AJOUTER CETTE LIGNE
        playerBases.Clear();
        eliminatedPlayers.Clear();

        for (int i = 0; i < gridManager.GetTotalTiles(); i++)
        {
            GameObject tile = gridManager.GetTileByNumber(i);
            if (tile != null)
            {
                TileData tileData = tile.GetComponent<TileData>();
                bool isLightTile = (tileData.coordinates.x + tileData.coordinates.y) % 2 == 0;

                Renderer renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(renderer.sharedMaterial);
                    mat.color = isLightTile ? Color.white : new Color(0.7f, 0.7f, 0.7f);
                    renderer.material = mat;
                }
            }
        }

        PlacePlayerBases();
        PlaceInitialLavaPieces();
        InitializeCardSystem();
        StartGame();
    }
}



[System.Serializable]
public class Player
{
    public int playerNumber;
    public Color color;

    public Player(int number, Color playerColor)
    {
        playerNumber = number;
        color = playerColor;
    }
}