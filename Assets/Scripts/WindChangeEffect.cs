using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class WindChangeEffect : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("UI References")]
    [SerializeField] private GameObject windChangePanel;
    [SerializeField] private TextMeshProUGUI windChangeText;
    [SerializeField] private Image windArrowImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Visual Settings")]
    [SerializeField] private Color windColor = new Color(0.5f, 1f, 0.5f);
    [SerializeField] private float rotationSpeed = 180f;

    private static WindChangeEffect instance;

    void Awake()
    {
        instance = this;

        if (windChangePanel != null)
            windChangePanel.SetActive(false);
    }

    public static void ShowWindChange(WindDirection newDirection)
    {
        if (instance != null)
        {
            instance.StartCoroutine(instance.AnimateWindChange(newDirection));
        }
    }

    private IEnumerator AnimateWindChange(WindDirection newDirection)
    {
        if (windChangePanel == null)
            yield break;

        // Activate panel
        windChangePanel.SetActive(true);

        // Setup text and arrow
        if (windChangeText != null)
        {
            windChangeText.text = $"VENT: {GetWindName(newDirection)}";
            windChangeText.color = windColor;
        }

        if (windArrowImage != null)
        {
            windArrowImage.color = windColor;
            SetArrowRotation(newDirection);
        }

        // Fade in + Scale up
        yield return StartCoroutine(FadeInAnimation());

        // Hold
        float holdTime = displayDuration - fadeInDuration - fadeOutDuration;
        float elapsed = 0f;

        while (elapsed < holdTime)
        {
            // Pulse effect
            if (windChangeText != null)
            {
                float pulse = 1f + Mathf.Sin(elapsed * 5f) * 0.05f;
                windChangeText.transform.localScale = Vector3.one * pulse;
            }

            // Rotate arrow
            if (windArrowImage != null)
            {
                windArrowImage.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Fade out
        yield return StartCoroutine(FadeOutAnimation());

        // Deactivate
        windChangePanel.SetActive(false);
    }

    private IEnumerator FadeInAnimation()
    {
        float elapsed = 0f;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        Vector3 originalScale = windChangePanel.transform.localScale;
        windChangePanel.transform.localScale = Vector3.zero;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            float curveValue = scaleCurve.Evaluate(t);

            if (canvasGroup != null)
                canvasGroup.alpha = t;

            windChangePanel.transform.localScale = originalScale * curveValue;

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        windChangePanel.transform.localScale = originalScale;
    }

    private IEnumerator FadeOutAnimation()
    {
        float elapsed = 0f;
        Vector3 originalScale = windChangePanel.transform.localScale;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            windChangePanel.transform.localScale = originalScale * (1f - t);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private string GetWindName(WindDirection direction)
    {
        switch (direction)
        {
            case WindDirection.North: return "NORD ↑";
            case WindDirection.South: return "SUD ↓";
            case WindDirection.East: return "EST →";
            case WindDirection.West: return "OUEST ←";
            default: return "?";
        }
    }

    private void SetArrowRotation(WindDirection direction)
    {
        float rotation = 0f;

        switch (direction)
        {
            case WindDirection.North: rotation = 0f; break;
            case WindDirection.East: rotation = 90f; break;
            case WindDirection.South: rotation = 180f; break;
            case WindDirection.West: rotation = 270f; break;
        }

        windArrowImage.transform.rotation = Quaternion.Euler(0, 0, -rotation);
    }
}
