using UnityEngine;
using System.Collections;

public class WaterRemovalEffect : MonoBehaviour
{
    // Call this on the lava piece that needs to be removed by water
    public static void AnimateRemoval(GameObject lavaTarget, System.Action onComplete = null)
    {
        if (lavaTarget == null) return;
        WaterRemovalEffect effect = lavaTarget.AddComponent<WaterRemovalEffect>();
        effect.StartCoroutine(effect.WaterBombSequence(onComplete));
    }

    private IEnumerator WaterBombSequence(System.Action onComplete)
    {
        Vector3 impactPos = transform.position;

        // 1) Spawn a visible water bomb that drops from the sky like a meteorite
        GameObject waterBomb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        waterBomb.transform.localScale = Vector3.one * 0.4f;
        Destroy(waterBomb.GetComponent<Collider>());

        Renderer bombRend = waterBomb.GetComponent<Renderer>();
        if (bombRend != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.1f, 0.4f, 1f);
            mat.SetFloat("_Metallic", 0.6f);
            mat.SetFloat("_Glossiness", 0.9f);
            bombRend.material = mat;
        }

        // Drop from high above with acceleration
        float dropHeight = 14f;
        float dropDuration = 0.4f;
        Vector3 startPos = impactPos + Vector3.up * dropHeight;
        float elapsed = 0f;

        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dropDuration;
            float curve = t * t * t; // accelerating

            waterBomb.transform.position = Vector3.Lerp(startPos, impactPos + Vector3.up * 0.3f, curve);
            waterBomb.transform.localScale = Vector3.Lerp(Vector3.one * 0.15f, Vector3.one * 0.45f, curve);
            waterBomb.transform.Rotate(Vector3.one, 800f * Time.deltaTime);

            yield return null;
        }

        // 2) Destroy the water bomb, spawn huge splash
        Destroy(waterBomb);
        SpawnWaterSplash(impactPos);
        SpawnShockwave(impactPos);

        // 3) The lava piece gets destroyed by the impact (sink + dissolve)
        yield return StartCoroutine(LavaSinkAndDissolve(impactPos));

        onComplete?.Invoke();
        Destroy(gameObject);
    }

    private IEnumerator LavaSinkAndDissolve(Vector3 originalPos)
    {
        Vector3 originalScale = transform.localScale;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (this == null || gameObject == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Sink down
            transform.position = originalPos + Vector3.down * t * 0.6f;

            // Flatten
            transform.localScale = new Vector3(
                originalScale.x * (1f + t * 0.5f),
                originalScale.y * Mathf.Max(0.01f, 1f - t),
                originalScale.z * (1f + t * 0.5f)
            );

            yield return null;
        }
    }

    private void SpawnWaterSplash(Vector3 pos)
    {
        Color waterMain = new Color(0.1f, 0.5f, 1f);
        Color waterLight = new Color(0.6f, 0.85f, 1f);

        // Big outward ring of water chunks
        int chunkCount = 20;
        for (int i = 0; i < chunkCount; i++)
        {
            float angle = (360f / chunkCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0.4f + Random.value * 0.5f, Mathf.Sin(rad));

            float speed = Random.Range(4f, 8f);
            float size = Random.Range(0.08f, 0.2f);
            CreateParticle(pos + Vector3.up * 0.15f, dir * speed, waterMain, size, Random.Range(0.6f, 1.1f));
        }

        // Upward water droplets
        int dropletCount = 30;
        for (int i = 0; i < dropletCount; i++)
        {
            Vector3 dir = new Vector3(
                Random.Range(-1.5f, 1.5f),
                Random.Range(2f, 6f),
                Random.Range(-1.5f, 1.5f)
            );
            float size = Random.Range(0.04f, 0.12f);
            CreateParticle(pos + Vector3.up * 0.25f, dir * Random.Range(2f, 5f), waterLight, size, Random.Range(0.5f, 1.0f));
        }

        // Steam/mist rising (lava + water = steam)
        int steamCount = 12;
        for (int i = 0; i < steamCount; i++)
        {
            Vector3 dir = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(1f, 3f), Random.Range(-0.5f, 0.5f));
            Color steamColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            CreateParticle(pos + Vector3.up * 0.3f, dir, steamColor, Random.Range(0.15f, 0.3f), Random.Range(0.8f, 1.5f));
        }
    }

    private void SpawnShockwave(Vector3 pos)
    {
        StartCoroutine(ShockwaveRing(pos));
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
            Color waterColor = new Color(0.1f, 0.55f, 1f, 0.8f);
            mat.color = waterColor;
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

        float duration = 0.4f;
        float maxRadius = 2.5f;
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
                c.a = (1f - t) * 0.8f;
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
