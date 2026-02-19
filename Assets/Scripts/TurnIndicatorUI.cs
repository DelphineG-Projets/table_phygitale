using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class TurnIndicatorUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private Image playerColorIndicator;
    [SerializeField] private GameObject turnPanel;

    [Header("Animation Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 1.2f;

    [Header("References")]
    [SerializeField] private GameManager gameManager;

    private Vector3 originalScale;
    private bool isPulsing = true;

    void Start()
    {
        if (turnPanel != null)
            originalScale = turnPanel.transform.localScale;
    }

    void Update()
    {
        UpdateTurnDisplay();

        if (isPulsing && turnPanel != null)
        {
            AnimatePulse();
        }
    }

    void UpdateTurnDisplay()
    {
        if (gameManager == null)
            return;

        Player currentPlayer = gameManager.GetCurrentPlayer();

        // Update player number text
        if (currentPlayerText != null)
        {
            currentPlayerText.text = $"JOUEUR {currentPlayer.playerNumber}";
        }

        // Update phase text
        if (phaseText != null)
        {
            string phase = gameManager.HasPlacedMandatoryLava() ? "Jouez une carte" : "Placez votre lave";
            phaseText.text = phase;
        }

        // Update color indicator
        if (playerColorIndicator != null)
        {
            playerColorIndicator.color = currentPlayer.color;
        }
    }

    void AnimatePulse()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * (pulseAmount - 1f) * 0.5f;
        turnPanel.transform.localScale = originalScale * pulse;
    }

    public void StopPulsing()
    {
        isPulsing = false;
        if (turnPanel != null)
            turnPanel.transform.localScale = originalScale;
    }

    public void StartPulsing()
    {
        isPulsing = true;
    }
}
