using UnityEngine;
using System.Collections;

/// <summary>
/// Meteorite-style water bomb that drops from the sky and splashes on impact.
/// Used when water is placed on an empty tile (no lava to remove).
/// </summary>
public class WaterBombEffect : MonoBehaviour
{
    public void Launch(Vector3 impactPos)
    {
        StartCoroutine(WaterMeteoriteSequence(impactPos));
    }

    private IEnumerator WaterMeteoriteSequence(Vector3 impactPos)
    {
        // 1) Create visible water sphere that falls from the sky
        GameObject waterBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        waterBall.transform.localScale = Vector3.one * 0.35f;
        Destroy(waterBall.GetComponent<Collider>());

        Renderer rend = waterBall.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.15f, 0.45f, 1f);
            mat.SetFloat("_Metallic", 0.5f);
            mat.SetFloat("_Glossiness", 0.85f);
            rend.material = mat;
        }

        // Drop from high above with accelerating curve
        float dropHeight = 12f;
        float dropDuration = 0.4f;
        Vector3 startPos = impactPos + Vector3.up * dropHeight;
        float elapsed = 0f;

        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dropDuration;
            float curve = t * t * t; // accelerating like gravity

            waterBall.transform.position = Vector3.Lerp(startPos, impactPos + Vector3.up * 0.15f, curve);
            waterBall.transform.localScale = Vector3.Lerp(Vector3.one * 0.12f, Vector3.one * 0.4f, curve);
            waterBall.transform.Rotate(Vector3.one, 700f * Time.deltaTime);

            yield return null;
        }

        // 2) Destroy the ball, big splash
        Destroy(waterBall);
        SpawnSplash(impactPos);
        yield return StartCoroutine(ShockwaveRing(impactPos));

        // 3) Self-cleanup
        Destroy(gameObject);
    }

    private void SpawnSplash(Vector3 pos)
    {
        Color waterMain = new Color(0.1f, 0.5f, 1f);
        Color waterLight = new Color(0.6f, 0.85f, 1f);

        // Ring of outward water chunks
        int chunkCount = 16;
        for (int i = 0; i < chunkCount; i++)
        {
            float angle = (360f / chunkCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0.3f + Random.value * 0.5f, Mathf.Sin(rad));

            float speed = Random.Range(3f, 6f);
            float size = Random.Range(0.06f, 0.16f);
            CreateParticle(pos + Vector3.up * 0.1f, dir * speed, waterMain, size, Random.Range(0.5f, 0.9f));
        }

        // Upward droplets
        int dropCount = 20;
        for (int i = 0; i < dropCount; i++)
        {
            Vector3 dir = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(2f, 5f),
                Random.Range(-1f, 1f)
            );
            float size = Random.Range(0.03f, 0.1f);
            CreateParticle(pos + Vector3.up * 0.2f, dir * Random.Range(2f, 4f), waterLight, size, Random.Range(0.4f, 0.8f));
        }
    }

    private IEnumerator ShockwaveRing(Vector3 pos)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.transform.position = pos + Vector3.up * 0.05f;
        ring.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
        Destroy(ring.GetComponent<Collider>());

        Renderer rend = ring.GetComponent<Renderer>();
        Material mat = null;
        if (rend != null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.1f, 0.55f, 1f, 0.7f);
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            rend.material = mat;
        }

        float duration = 0.35f;
        float maxRadius = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float radius = Mathf.Lerp(0.05f, maxRadius, t);
            ring.transform.localScale = new Vector3(radius, 0.01f, radius);

            if (mat != null)
            {
                Color c = mat.color;
                c.a = (1f - t) * 0.7f;
                mat.color = c;
            }

            yield return null;
        }

        Destroy(ring);
    }

    private void CreateParticle(Vector3 pos, Vector3 velocity, Color color, float size, float life)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.transform.position = pos;
        p.transform.localScale = Vector3.one * size;
        Destroy(p.GetComponent<Collider>());

        Renderer r = p.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            r.material = mat;
        }

        SimpleParticle sp = p.AddComponent<SimpleParticle>();
        sp.Initialize(velocity, life);
    }
}
