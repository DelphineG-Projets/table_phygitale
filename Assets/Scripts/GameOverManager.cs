using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Image victoireImage;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Victory Images (1 per player)")]
    [SerializeField] private Texture2D victoire1;
    [SerializeField] private Texture2D victoire2;
    [SerializeField] private Texture2D victoire3;
    [SerializeField] private Texture2D victoire4;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 8f;

    [Header("References")]
    [SerializeField] private GameManager gameManager;

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void ShowGameOver(int winnerNumber)
    {
        Debug.Log($"ShowGameOver pour Joueur {winnerNumber}");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            StartCoroutine(VictorySequence(winnerNumber));
        }
    }

    private IEnumerator VictorySequence(int winnerNumber)
    {
        // Pick the right image and convert Texture2D to Sprite at runtime
        Texture2D winnerTexture = GetVictoireTexture(winnerNumber);

        if (victoireImage != null && winnerTexture != null)
        {
            Sprite sprite = Sprite.Create(
                winnerTexture,
                new Rect(0, 0, winnerTexture.width, winnerTexture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            victoireImage.sprite = sprite;
            victoireImage.type = Image.Type.Simple;
            victoireImage.preserveAspect = true;

            // Force fullscreen
            RectTransform rt = victoireImage.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Start invisible and small
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        RectTransform imageRT = victoireImage != null ? victoireImage.GetComponent<RectTransform>() : null;

        // === PHASE 1: Slam in from big to normal (0.4s) ===
        float slamDuration = 0.4f;
        float slamElapsed = 0f;
        float startScale = 3f;

        while (slamElapsed < slamDuration)
        {
            slamElapsed += Time.deltaTime;
            float t = slamElapsed / slamDuration;

            // Overshoot curve: goes past 1 then settles back
            float curve = 1f - Mathf.Pow(1f - t, 3f);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(t * 4f); // fully visible by 25%

            if (imageRT != null)
            {
                float scale = Mathf.Lerp(startScale, 1f, curve);
                imageRT.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        if (imageRT != null)
            imageRT.localScale = Vector3.one;

        // === PHASE 2: Impact shake (0.3s) ===
        float shakeDuration = 0.3f;
        float shakeElapsed = 0f;
        float shakeIntensity = 25f;

        while (shakeElapsed < shakeDuration)
        {
            shakeElapsed += Time.deltaTime;
            float t = 1f - (shakeElapsed / shakeDuration); // fades out

            float offsetX = Random.Range(-1f, 1f) * shakeIntensity * t;
            float offsetY = Random.Range(-1f, 1f) * shakeIntensity * t;

            if (imageRT != null)
                imageRT.anchoredPosition = new Vector2(offsetX, offsetY);

            yield return null;
        }

        if (imageRT != null)
            imageRT.anchoredPosition = Vector2.zero;

        // === PHASE 3: Gentle breathing pulse + countdown ===
        float timeRemaining = displayDuration;
        float breatheSpeed = 2.5f;
        float breatheAmount = 0.03f;

        while (timeRemaining > 0)
        {
            // Breathing pulse on the image
            if (imageRT != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * breatheSpeed) * breatheAmount;
                imageRT.localScale = Vector3.one * pulse;
            }

            if (countdownText != null)
                countdownText.text = $"Nouvelle partie dans {Mathf.Ceil(timeRemaining)}s...";

            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        // Reset scale before hiding
        if (imageRT != null)
            imageRT.localScale = Vector3.one;

        ResetGame();
    }

    private Texture2D GetVictoireTexture(int playerNumber)
    {
        switch (playerNumber)
        {
            case 1: return victoire1;
            case 2: return victoire2;
            case 3: return victoire3;
            case 4: return victoire4;
            default: return victoire1;
        }
    }

    public void ResetGame()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameManager != null)
            gameManager.ResetGame();
    }
}
