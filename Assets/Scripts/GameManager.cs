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
    public bool isGameOver = false;

    [Header("Card System")]
    [SerializeField] private DeckManager deckManager;
    private WindDirection currentWindDirection;
    private List<PlayerHand> playerHands = new List<PlayerHand>();
    public bool hasPlacedMandatoryLava = false;
    public bool hasPlayedCard = false;

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
    private List<GameObject> playerBaseLabels = new List<GameObject>();

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

            GameObject handObj = new GameObject($"Player{i + 1}_Hand");
            handObj.transform.SetParent(transform);
            PlayerHand hand = handObj.AddComponent<PlayerHand>();
            hand.playerNumber = i + 1;
            playerHands.Add(hand);
        }

        Debug.Log($"{numberOfPlayers} joueurs créés");
    }

    // ==========================================
    // PLACEMENT DES BASES - adapté carré/cercle
    // ==========================================

    void PlacePlayerBases()
    {
        GridShape shape = gridManager.GetGridShape();

        if (shape == GridShape.Square)
        {
            PlaceBasesSquare();
        }
        else
        {
            PlaceBasesCircle();
        }
    }

    void PlaceBasesSquare()
    {
        int w = gridManager.gridWidth;
        int h = gridManager.gridHeight;
        int margin = 1;

        // Coins : bas-gauche, bas-droite, haut-droite, haut-gauche
        Vector2Int[] cornerPositions = new Vector2Int[]
        {
            new Vector2Int(margin, margin),
            new Vector2Int(w - 1 - margin, margin),
            new Vector2Int(w - 1 - margin, h - 1 - margin),
            new Vector2Int(margin, h - 1 - margin)
        };

        for (int i = 0; i < numberOfPlayers && i < cornerPositions.Length; i++)
        {
            PlaceBaseAtPosition(cornerPositions[i], i);
        }
    }

    void PlaceBasesCircle()
    {
        // Angles diagonaux : bas-gauche, bas-droite, haut-droite, haut-gauche
        float[] angles = new float[] { 225f, 315f, 45f, 135f };

        for (int i = 0; i < numberOfPlayers && i < angles.Length; i++)
        {
            Vector2Int bestPos = FindBestBasePositionOnCircle(angles[i]);

            if (bestPos.x >= 0)
            {
                PlaceBaseAtPosition(bestPos, i);
            }
            else
            {
                Debug.LogWarning($"Impossible de trouver une position pour la base du joueur {i + 1}");
            }
        }
    }

    Vector2Int FindBestBasePositionOnCircle(float angleDeg)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float dirX = Mathf.Cos(angleRad);
        float dirY = Mathf.Sin(angleRad);

        Vector2 center = gridManager.GridCenter;
        float maxDist = Mathf.Max(gridManager.gridWidth, gridManager.gridHeight);

        for (float dist = maxDist; dist >= 2f; dist -= 0.5f)
        {
            int x = Mathf.RoundToInt(center.x + dirX * dist);
            int y = Mathf.RoundToInt(center.y + dirY * dist);

            if (gridManager.HasTileAt(x, y))
            {
                float actualDist = Vector2.Distance(new Vector2(x, y), center);
                if (actualDist >= 3f)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return new Vector2Int(-1, -1);
    }

    void PlaceBaseAtPosition(Vector2Int pos, int playerIndex)
    {
        GameObject tile = gridManager.GetTileByCoordinates(pos.x, pos.y);

        if (tile != null)
        {
            TileData tileData = tile.GetComponent<TileData>();
            playerBases.Add(tileData.tileNumber, players[playerIndex]);

            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = baseMaterial != null ? new Material(baseMaterial) : new Material(renderer.sharedMaterial);
                mat.color = playerBaseColors[playerIndex];
                renderer.material = mat;
            }

            // Créer le label flottant au-dessus de la base
            CreatePlayerLabel(tile.transform.position, players[playerIndex].playerNumber, playerBaseColors[playerIndex]);

            Debug.Log($"Base du Joueur {players[playerIndex].playerNumber} placée en ({pos.x}, {pos.y})");
        }
        else
        {
            Debug.LogWarning($"Impossible de placer la base du joueur {playerIndex + 1} à ({pos.x}, {pos.y})");
        }
    }

    void CreatePlayerLabel(Vector3 tilePosition, int playerNumber, Color playerColor)
    {
        GameObject labelObj = new GameObject($"PlayerLabel_{playerNumber}");
        labelObj.transform.position = tilePosition + Vector3.up * 0.8f;
        labelObj.transform.SetParent(transform);

        PlayerLabelBillboard billboard = labelObj.AddComponent<PlayerLabelBillboard>();

        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = $"J{playerNumber}";
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.12f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = Color.black;

        MeshRenderer meshRend = labelObj.GetComponent<MeshRenderer>();
        if (meshRend != null)
        {
            meshRend.sortingOrder = 10;
        }

        playerBaseLabels.Add(labelObj);
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

    // ==========================================
    // LAVE INITIALE - utilise le vrai centre
    // ==========================================

    void PlaceInitialLavaPieces()
    {
        Vector2 center = gridManager.GridCenter;
        int centerX = Mathf.FloorToInt(center.x);
        int centerY = Mathf.FloorToInt(center.y);

        Vector2Int[] centerPositions = new Vector2Int[]
        {
            new Vector2Int(centerX, centerY),
            new Vector2Int(centerX + 1, centerY),
            new Vector2Int(centerX, centerY + 1),
            new Vector2Int(centerX + 1, centerY + 1)
        };

        int placed = 0;
        foreach (Vector2Int pos in centerPositions)
        {
            if (!gridManager.HasTileAt(pos.x, pos.y))
                continue;

            GameObject tile = gridManager.GetTileByCoordinates(pos.x, pos.y);
            if (tile != null)
            {
                TileData tileData = tile.GetComponent<TileData>();
                PlaceLavaPiece(tileData.tileNumber, tile.transform.position);
                initialLavaTiles.Add(tileData.tileNumber);
                placed++;
            }
        }

        Debug.Log($"{placed} lava_pieces initiales placées au centre ({centerX},{centerY}) (protégées)");
    }

    // ==========================================
    // PLACEMENT DES PIÈCES (avec animations)
    // ==========================================

    void PlaceLavaPiece(int tileNumber, Vector3 tileWorldPosition)
    {
        GameObject lavaPiece = Instantiate(lavaPiecePrefab, tileWorldPosition + Vector3.up * 0.1f, Quaternion.identity, transform);
        lavaPiece.name = $"lava_piece_{tileNumber}";

        if (lavaMaterial != null)
        {
            Renderer renderer = lavaPiece.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = lavaMaterial;
            }
        }

        Collider collider = lavaPiece.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // Animation de spawn
        if (Application.isPlaying)
        {
            PieceAnimator animator = lavaPiece.AddComponent<PieceAnimator>();
            animator.AnimateSpawn(tileWorldPosition + Vector3.up * 0.1f, PieceType.Lava);
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

        BlockPiece blockData = blockPiece.GetComponent<BlockPiece>();
        if (blockData == null)
            blockData = blockPiece.AddComponent<BlockPiece>();

        blockData.tileNumber = tileNumber;

        Collider collider = blockPiece.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // Animation de spawn
        PieceAnimator animator = blockPiece.AddComponent<PieceAnimator>();
        animator.AnimateSpawn(position + Vector3.up * 0.1f, PieceType.Block);

        occupiedTiles.Add(tileNumber, blockPiece);
        blockTiles.Add(tileNumber);

        Debug.Log($"🚧 Bloque placé sur tile {tileNumber}");
    }

    // ==========================================
    // NETTOYAGE - fonctionne en Editor ET Play
    // ==========================================

    /// <summary>
    /// Supprime tous les enfants du GameManager (lava, block, labels, hands).
    /// Fonctionne en Editor mode ET en Play mode.
    /// </summary>
    public void ClearAllPieces()
    {
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(transform.GetChild(i).gameObject);
            else
                Destroy(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }

        occupiedTiles.Clear();
        lavaTiles.Clear();
        blockTiles.Clear();
        initialLavaTiles.Clear();
        playerBases.Clear();
        eliminatedPlayers.Clear();
        players.Clear();
        playerHands.Clear();
        playerBaseLabels.Clear();

        Debug.Log($"🧹 Toutes les pièces du GameManager nettoyées ({childCount} objets supprimés)");
    }

    public void PlaceInitialLavaPiecesInEditor()
    {
        PlaceInitialLavaPieces();
    }

    // ==========================================
    // GAME FLOW
    // ==========================================

    void StartGame()
    {
        currentPlayerIndex = 0;
        isGameOver = false;
        Debug.Log($"=== DÉBUT DE LA PARTIE ===");
        Debug.Log($"Au tour du Joueur {GetCurrentPlayer().playerNumber}");

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

        // Animation du changement de vent
        WindChangeEffect.ShowWindChange(newDirection);
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

    // ==========================================
    // EFFETS DES CARTES
    // ==========================================

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

            if (tile == null)
                continue;

            if (IsTileBlock(tileNumber))
            {
                Debug.Log($"⚠️ Tile {tileNumber} est un bloque, on le saute");
                continue;
            }

            if (occupiedTiles.ContainsKey(tileNumber))
            {
                Debug.Log($"⚠️ Tile {tileNumber} est déjà occupé, on le saute");
                continue;
            }

            PlaceLavaPiece(tileNumber, tile.transform.position);
            CheckBaseElimination(tileNumber);
            lavasPlaced++;
        }

        Debug.Log($"🔥 {lavasPlaced} lave(s) placée(s) (certaines cases peuvent avoir été sautées)");

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

            if (initialLavaTiles.Contains(tileNumber))
            {
                Debug.LogWarning($"🛡️ Tile {tileNumber} est une lave initiale protégée, l'eau ne peut pas l'enlever!");
                continue;
            }

            if (lavaTiles.Contains(tileNumber))
            {
                if (occupiedTiles.ContainsKey(tileNumber))
                {
                    GameObject lavaToRemove = occupiedTiles[tileNumber];

                    // Animation de suppression par l'eau
                    WaterRemovalEffect.AnimateRemoval(lavaToRemove, () => { });

                    occupiedTiles.Remove(tileNumber);
                }

                lavaTiles.Remove(tileNumber);
                Debug.Log($"💧 Lave enlevée du tile {tileNumber}");
                waterPlaced++;

                // Effet visuel splash
                CreateWaterSplashEffect(tile.transform.position);
            }
            else if (!occupiedTiles.ContainsKey(tileNumber))
            {
                Debug.Log($"💧 Eau placée sur tile vide {tileNumber} (pas d'effet)");
                CreateWaterSplashEffect(tile.transform.position);
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

    // ==========================================
    // GESTION DES TOURS
    // ==========================================

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
                    int playerIndex = player.playerNumber - 1;

                    if (player.playerNumber == GetCurrentPlayer().playerNumber)
                    {
                        Color brightColor = playerBaseColors[playerIndex];
                        brightColor = Color.Lerp(brightColor, Color.white, 0.5f);
                        mat.color = brightColor;

                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", playerBaseColors[playerIndex] * 0.5f);
                    }
                    else
                    {
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

        HighlightCurrentPlayerBase();
    }

    // ==========================================
    // QUERIES PUBLIQUES
    // ==========================================

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

    public bool HasPlacedMandatoryLava()
    {
        return hasPlacedMandatoryLava;
    }

    // ==========================================
    // EFFETS VISUELS
    // ==========================================

    void CreateWaterSplashEffect(Vector3 position)
    {
        GameObject effectObj = new GameObject("WaterBombEffect");
        effectObj.transform.position = position;
        WaterBombEffect bomb = effectObj.AddComponent<WaterBombEffect>();
        bomb.Launch(position);
    }

    // ==========================================
    // RESET
    // ==========================================

    public void ResetGame()
    {
        Debug.Log("🔄 RESET COMPLET DU JEU");

        // Nettoyer toutes les pièces via la méthode unifiée
        ClearAllPieces();

        hasPlacedMandatoryLava = false;
        hasPlayedCard = false;
        isGameOver = false;

        // Restaurer les couleurs des tiles via le GridManager
        gridManager.RestoreTileColors();

        // Réinitialiser le jeu
        InitializePlayers();
        PlacePlayerBases();
        PlaceInitialLavaPieces();
        InitializeCardSystem();
        StartGame();

        // Reset GameTester phase
        GameTester tester = FindObjectOfType<GameTester>();
        if (tester != null)
        {
            tester.ResetToMandatoryLavaPhase();
        }
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