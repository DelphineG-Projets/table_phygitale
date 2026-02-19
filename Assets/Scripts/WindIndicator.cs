using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WindIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Image windArrow;
    [SerializeField] private TextMeshProUGUI windText;

    void Update()
    {
        UpdateWindDisplay();
    }

    void UpdateWindDisplay()
    {
        WindDirection wind = gameManager.GetCurrentWind();

        // Rotation de la flèche
        float rotation = 0f;
        string windName = "";

        switch (wind)
        {
            case WindDirection.North:
                rotation = 0f;
                windName = "NORD ↑";
                break;
            case WindDirection.East:
                rotation = 90f;
                windName = "EST →";
                break;
            case WindDirection.South:
                rotation = 180f;
                windName = "SUD ↓";
                break;
            case WindDirection.West:
                rotation = 270f;
                windName = "OUEST ←";
                break;
        }

        if (windArrow != null)
        {
            windArrow.transform.rotation = Quaternion.Euler(0, 0, -rotation);
        }

        if (windText != null)
        {
            windText.text = $"Vent: {windName}";
        }
    }
}