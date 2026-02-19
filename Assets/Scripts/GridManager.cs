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

    [Header("Circle Settings")]
    [Tooltip("Rayon en nombre de tiles. Si 0, calculé automatiquement depuis gridWidth/gridHeight.")]
    [SerializeField] private float circleRadius = 0f;

    [Header("Tile Prefab")]
    [SerializeField] private GameObject tilePrefab;

    [Header("Editor Options")]
    [SerializeField] private bool generateOnStart = false;

    // Dictionnaire pour accéder aux tiles par leur numéro
    private Dictionary<int, GameObject> tiles = new Dictionary<int, GameObject>();

    // Dictionnaire pour accéder aux tiles par leurs coordonnées
    private Dictionary<Vector2Int, GameObject> tilesByCoordinates = new Dictionary<Vector2Int, GameObject>();

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
        int tileNumber = 0;

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector3 position = new Vector3(x * tileSize, 0, y * tileSize);
                CreateTile(position, x, y, ref tileNumber);
            }
        }

        Debug.Log($"Grille carrée créée : {gridWidth}x{gridHeight} = {tileNumber} tiles");
    }

    private void GenerateCircleGrid()
    {
        // Calculer le rayon effectif
        float radius = circleRadius > 0 ? circleRadius : Mathf.Min(gridWidth, gridHeight) / 2f;

        // Le centre de la grille
        float centerX = radius;
        float centerY = radius;

        int tileNumber = 0;
        int gridDiameter = Mathf.CeilToInt(radius * 2);

        for (int y = 0; y <= gridDiameter; y++)
        {
            for (int x = 0; x <= gridDiameter; x++)
            {
                // Distance du centre (en coordonnées grille)
                float dx = x - centerX;
                float dy = y - centerY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                // Ne créer le tile que s'il est dans le cercle
                if (distance <= radius)
                {
                    Vector3 position = new Vector3(x * tileSize, 0, y * tileSize);
                    CreateTile(position, x, y, ref tileNumber);
                }
            }
        }

        Debug.Log($"Grille circulaire créée : rayon {radius} = {tileNumber} tiles");
    }

    private void CreateTile(Vector3 position, int x, int y, ref int tileNumber)
    {
        GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
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
        if (renderer != null)
        {
            Material mat = new Material(renderer.sharedMaterial);

            if (isLightTile)
            {
                mat.color = Color.white;
            }
            else
            {
                mat.color = new Color(0.7f, 0.7f, 0.7f);
            }

            renderer.material = mat;
        }
    }

    public void ClearGrid()
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

        Debug.Log($"Chargé {tiles.Count} tiles existants");
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
            gridManager.ClearGrid();
            gridManager.GenerateGrid();

            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.SendMessage("PlaceInitialLavaPiecesInEditor", SendMessageOptions.DontRequireReceiver);
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