using UnityEngine;
using TMPro;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 5f;

    [Header("References")]
    [SerializeField] private GameManager gameManager;

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void ShowGameOver(int winnerNumber)
    {
        Debug.Log($"🎯 ShowGameOver appelée pour Joueur {winnerNumber}");

        if (gameOverPanel != null)
        {
            Debug.Log("✓ Panel trouvé, activation...");
            gameOverPanel.SetActive(true);

            if (winnerText != null)
            {
                Debug.Log("✓ Texte assigné");
                winnerText.text = $"JOUEUR {winnerNumber} GAGNE !";
            }
            else
            {
                Debug.LogError("✗ WinnerText est NULL!");
            }

            StartCoroutine(CountdownAndReset());
        }
        else
        {
            Debug.LogError("✗ GameOverPanel est NULL!");
        }
    }

    private IEnumerator CountdownAndReset()
    {
        float timeRemaining = displayDuration;

        while (timeRemaining > 0)
        {
            if (countdownText != null)
            {
                countdownText.text = $"Nouvelle partie dans {Mathf.Ceil(timeRemaining)}s...";
            }

            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        ResetGame();
    }

    public void ResetGame()
    {
        Debug.Log("🔄 Reset du jeu...");

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameManager != null)
        {
            gameManager.ResetGame();
        }
    }
}