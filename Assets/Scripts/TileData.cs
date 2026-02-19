using UnityEngine;

public class TileData : MonoBehaviour
{
    public int tileNumber;
    public Vector2Int coordinates;

    // Tu peux ajouter d'autres propriétés ici selon tes besoins
    // Par exemple : type de terrain, occupé ou non, etc.

    public void DisplayInfo()
    {
        Debug.Log($"Tile #{tileNumber} - Position: ({coordinates.x}, {coordinates.y})");
    }
}