using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum GridShape
{
    Square,
    Circle
}

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 16;
    public int gridHeight = 16;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private GridShape gridShape = GridShape.Square;

    [Header("Grid Colors")]
    [SerializeField] private bool useCustomColors = false;
    [Tooltip("Couleur des cases claires (si custom)")]
    [SerializeField] private Color lightTileColor = Color.white;
    [Tooltip("Couleur des cases foncées (si custom)")]
    [SerializeField] private Color darkTileColor = new Color(0.7f, 0.7f, 0.7f);
    [Tooltip("Intensité de la variation clair/foncé (0 = pas de différence, 0.2 = subtil, 0.5 = fort)")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float checkerboardContrast = 0.15f;

    [Header("Tile Prefab")]
    [SerializeField] private GameObject tilePrefab;

    [Header("Editor Options")]
    [SerializeField] private bool generateOnStart = false;

    // Dictionnaire pour accéder aux tiles par leur numéro
    private Dictionary<int, GameObject> tiles = new Dictionary<int, GameObject>();

    // Dictionnaire pour accéder aux tiles par leurs coordonnées
    private Dictionary<Vector2Int, GameObject> tilesByCoordinates = new Dictionary<Vector2Int, GameObject>();

    // Centre réel de la grille (utile pour le cercle)
    private Vector2 gridCenter;
    public Vector2 GridCenter => gridCenter;

    void Start()
    {
        if (generateOnStart)
        {
            ClearGrid();
            GenerateGrid();
        }

        // Toujours charger les tiles existants dans les dictionnaires
        LoadExistingTiles();
    }

    public void GenerateGrid()
    {
        switch (gridShape)
        {
            case GridShape.Square:
                GenerateSquareGrid();
                break;
            case GridShape.Circle:
                GenerateCircleGrid();
                break;
        }
    }

    private void GenerateSquareGrid()
    {
        gridCenter = new Vector2(gridWidth / 2f, gridHeight / 2f);

        // Offset pour centrer la grille sur le GridManager
        float offsetX = -(gridWidth - 1) * tileSize / 2f;
        float offsetZ = -(gridHeight - 1) * tileSize / 2f;

        int tileNumber = 0;

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector3 position = new Vector3(x * tileSize + offsetX, 0, y * tileSize + offsetZ);
                CreateTile(position, x, y, ref tileNumber);
            }
        }

        Debug.Log($"Grille carrée créée : {gridWidth}x{gridHeight} = {tileNumber} tiles");
    }

    private void GenerateCircleGrid()
    {
        // Le rayon est la moitié de la plus petite dimension
        float radius = Mathf.Min(gridWidth, gridHeight) / 2f;

        // Le centre de la grille
        float centerX = radius;
        float centerY = radius;

        gridCenter = new Vector2(centerX, centerY);

        int gridDiameter = Mathf.CeilToInt(radius * 2);

        // Phase 1 : Déterminer quelles positions sont dans le cercle
        HashSet<Vector2Int> validPositions = new HashSet<Vector2Int>();

        for (int y = 0; y <= gridDiameter; y++)
        {
            for (int x = 0; x <= gridDiameter; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance <= radius)
                {
                    validPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        // Phase 2 : Filtrer les tiles isolées (sans voisin cardinal)
        HashSet<Vector2Int> filteredPositions = new HashSet<Vector2Int>();
        Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int pos in validPositions)
        {
            int neighborCount = 0;
            foreach (Vector2Int dir in neighbors)
            {
                if (validPositions.Contains(pos + dir))
                    neighborCount++;
            }

            // Garder seulement si au moins 2 voisins (pas de pièce qui dépasse seule)
            if (neighborCount >= 2)
            {
                filteredPositions.Add(pos);
            }
        }

        // Phase 3 : Créer les tiles (centrées sur le GridManager)
        int tileNumber = 0;
        float offsetX = -centerX * tileSize;
        float offsetZ = -centerY * tileSize;

        for (int y = 0; y <= gridDiameter; y++)
        {
            for (int x = 0; x <= gridDiameter; x++)
            {
                if (filteredPositions.Contains(new Vector2Int(x, y)))
                {
                    Vector3 position = new Vector3(x * tileSize + offsetX, 0, y * tileSize + offsetZ);
                    CreateTile(position, x, y, ref tileNumber);
                }
            }
        }

        Debug.Log($"Grille circulaire créée : rayon {radius} = {tileNumber} tiles (filtré {validPositions.Count - filteredPositions.Count} tiles isolées)");
    }

    private void CreateTile(Vector3 localPos, int x, int y, ref int tileNumber)
    {
        GameObject tile = Instantiate(tilePrefab, transform);
        tile.transform.localPosition = localPos;
        tile.transform.localRotation = Quaternion.identity;
        tile.name = $"Tile_{tileNumber} ({x},{y})";

        TileData tileData = tile.GetComponent<TileData>();
        if (tileData == null)
            tileData = tile.AddComponent<TileData>();

        tileData.tileNumber = tileNumber;
        tileData.coordinates = new Vector2Int(x, y);

        ApplyCheckerboardPattern(tile, x, y);

        tiles.Add(tileNumber, tile);
        tilesByCoordinates.Add(new Vector2Int(x, y), tile);

        tileNumber++;
    }

    private void ApplyCheckerboardPattern(GameObject tile, int x, int y)
    {
        bool isLightTile = (x + y) % 2 == 0;

        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer == null) return;

        Material mat = new Material(renderer.sharedMaterial);

        if (useCustomColors)
        {
            // Mode custom : utiliser les couleurs définies dans l'Inspector
            mat.color = isLightTile ? lightTileColor : darkTileColor;
        }
        else
        {
            // Mode auto : partir de la couleur du material du prefab
            Color baseColor = renderer.sharedMaterial.color;

            if (isLightTile)
            {
                mat.color = Color.Lerp(baseColor, Color.white, checkerboardContrast);
            }
            else
            {
                mat.color = Color.Lerp(baseColor, Color.black, checkerboardContrast);
            }
        }

        renderer.material = mat;
    }

    public void ClearGrid()
    {
        // Nettoyer les pièces du GameManager (lava, block, etc.)
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.ClearAllPieces();
        }

        // Supprimer tous les tiles enfants
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

        tiles.Clear();
        tilesByCoordinates.Clear();

        Debug.Log("Grille effacée");
    }

    private void LoadExistingTiles()
    {
        tiles.Clear();
        tilesByCoordinates.Clear();

        foreach (Transform child in transform)
        {
            TileData tileData = child.GetComponent<TileData>();
            if (tileData != null)
            {
                tiles[tileData.tileNumber] = child.gameObject;
                tilesByCoordinates[tileData.coordinates] = child.gameObject;
            }
        }

        // Recalculer le centre si pas déjà fait
        if (tiles.Count > 0 && gridCenter == Vector2.zero)
        {
            if (gridShape == GridShape.Circle)
            {
                float radius = Mathf.Min(gridWidth, gridHeight) / 2f;
                gridCenter = new Vector2(radius, radius);
            }
            else
            {
                gridCenter = new Vector2(gridWidth / 2f, gridHeight / 2f);
            }
        }

        Debug.Log($"Chargé {tiles.Count} tiles existants (centre: {gridCenter})");
    }

    // --- Méthodes d'accès publiques ---

    public GameObject GetTileByNumber(int number)
    {
        if (tiles.ContainsKey(number))
            return tiles[number];

        Debug.LogWarning($"Tile {number} n'existe pas!");
        return null;
    }

    public GameObject GetTileByCoordinates(int x, int y)
    {
        Vector2Int coords = new Vector2Int(x, y);
        if (tilesByCoordinates.ContainsKey(coords))
            return tilesByCoordinates[coords];

        Debug.LogWarning($"Tile aux coordonnées ({x},{y}) n'existe pas!");
        return null;
    }

    public TileData GetTileDataByNumber(int number)
    {
        GameObject tile = GetTileByNumber(number);
        return tile != null ? tile.GetComponent<TileData>() : null;
    }

    public int GetTotalTiles()
    {
        return tiles.Count;
    }

    /// <summary>
    /// Vérifie si des coordonnées existent dans la grille (utile pour le cercle où toutes les coords ne sont pas remplies)
    /// </summary>
    public bool HasTileAt(int x, int y)
    {
        return tilesByCoordinates.ContainsKey(new Vector2Int(x, y));
    }

    /// <summary>
    /// Restaure les couleurs du damier sur toutes les tiles (utile après un ResetGame)
    /// </summary>
    public void RestoreTileColors()
    {
        foreach (var kvp in tilesByCoordinates)
        {
            Vector2Int coords = kvp.Key;
            GameObject tile = kvp.Value;
            ApplyCheckerboardPattern(tile, coords.x, coords.y);
        }
    }

    public GridShape GetGridShape()
    {
        return gridShape;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GridManager gridManager = (GridManager)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Grid", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(gridManager.gameObject, "Generate Grid");

            gridManager.ClearGrid();
            gridManager.GenerateGrid();

            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(gameManager.gameObject, "Place Initial Lava");
                gameManager.PlaceInitialLavaPiecesInEditor();
                EditorUtility.SetDirty(gameManager);
            }

            EditorUtility.SetDirty(gridManager);
        }

        if (GUILayout.Button("Clear Grid", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Confirmer",
                "Êtes-vous sûr de vouloir effacer toute la grille ?",
                "Oui", "Annuler"))
            {
                gridManager.ClearGrid();
                EditorUtility.SetDirty(gridManager);
            }
        }
    }
}
#endif