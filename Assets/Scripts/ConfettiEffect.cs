using UnityEngine;
using System.Collections.Generic;

public class ConfettiEffect : MonoBehaviour
{
    [Header("Confetti Settings")]
    [SerializeField] private GameObject confettiPrefab;
    [SerializeField] private int confettiCount = 100;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float upwardForce = 10f;
    [SerializeField] private float sidewaysForce = 5f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Colors")]
    [SerializeField] private Color[] confettiColors = new Color[]
    {
        Color.red,
        Color.yellow,
        Color.green,
        Color.blue,
        Color.magenta,
        new Color(1f, 0.5f, 0f), // Orange
        new Color(1f, 0.75f, 0.8f) // Pink
    };

    private List<ConfettiParticle> activeParticles = new List<ConfettiParticle>();

    void OnEnable()
    {
        SpawnConfetti();
    }

    void OnDisable()
    {
        ClearConfetti();
    }

    void Update()
    {
        UpdateParticles();
    }

    void SpawnConfetti()
    {
        ClearConfetti();

        for (int i = 0; i < confettiCount; i++)
        {
            Vector3 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + randomOffset;

            GameObject confetti;
            if (confettiPrefab != null)
            {
                confetti = Instantiate(confettiPrefab, spawnPos, Random.rotation, transform);
            }
            else
            {
                // Create simple quad if no prefab
                confetti = GameObject.CreatePrimitive(PrimitiveType.Quad);
                confetti.transform.SetParent(transform);
                confetti.transform.position = spawnPos;
                confetti.transform.rotation = Random.rotation;
                confetti.transform.localScale = Vector3.one * 0.1f;
            }

            // Apply random color
            Renderer renderer = confetti.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color randomColor = confettiColors[Random.Range(0, confettiColors.Length)];
                renderer.material.color = randomColor;
            }

            // Create particle data
            ConfettiParticle particle = new ConfettiParticle
            {
                gameObject = confetti,
                velocity = new Vector3(
                    Random.Range(-sidewaysForce, sidewaysForce),
                    Random.Range(upwardForce * 0.5f, upwardForce),
                    Random.Range(-sidewaysForce, sidewaysForce)
                ),
                angularVelocity = Random.insideUnitSphere * 360f,
                lifetime = lifetime
            };

            activeParticles.Add(particle);
        }
    }

    void UpdateParticles()
    {
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            ConfettiParticle particle = activeParticles[i];

            if (particle.gameObject == null)
            {
                activeParticles.RemoveAt(i);
                continue;
            }

            // Apply physics
            particle.velocity.y += gravity * Time.deltaTime;
            particle.gameObject.transform.position += particle.velocity * Time.deltaTime;
            particle.gameObject.transform.Rotate(particle.angularVelocity * Time.deltaTime);

            // Update lifetime
            particle.lifetime -= Time.deltaTime;

            if (particle.lifetime <= 0)
            {
                Destroy(particle.gameObject);
                activeParticles.RemoveAt(i);
            }
        }
    }

    void ClearConfetti()
    {
        foreach (ConfettiParticle particle in activeParticles)
        {
            if (particle.gameObject != null)
                Destroy(particle.gameObject);
        }

        activeParticles.Clear();
    }

    private class ConfettiParticle
    {
        public GameObject gameObject;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public float lifetime;
    }
}
