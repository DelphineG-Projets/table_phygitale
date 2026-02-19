using UnityEngine;
using System.Collections;

public class PieceAnimator : MonoBehaviour
{
    // Called by GameManager immediately after instantiating a lava or water piece
    public void AnimateSpawn(Vector3 finalPosition, PieceType pieceType)
    {
        StartCoroutine(MeteoriteDrop(finalPosition, pieceType));
    }

    // ─── Meteorite drop ───────────────────────────────────────────────────────
    private IEnumerator MeteoriteDrop(Vector3 finalPos, PieceType pieceType)
    {
        // Start high above, tiny, fast spin
        float dropHeight    = 12f;
        float dropDuration  = 0.45f;   // feels snappy
        Vector3 startPos    = finalPos + Vector3.up * dropHeight;
        Vector3 targetScale = transform.localScale;

        transform.position   = startPos;
        transform.localScale = targetScale * 0.3f;  // start small (far away)

        float elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dropDuration;

            // Accelerating curve – slow start, fast end (like gravity)
            float curve = t * t * t;

            transform.position   = Vector3.Lerp(startPos, finalPos, curve);
            transform.localScale = Vector3.Lerp(targetScale * 0.3f, targetScale, curve);

            yield return null;
        }

        transform.position   = finalPos;
        transform.localScale = targetScale;

        // Impact
        SpawnImpactSplash(finalPos, pieceType);
        StartCoroutine(ImpactSquash(targetScale));
    }

    // ─── Squash‑and‑stretch on landing ────────────────────────────────────────
    private IEnumerator ImpactSquash(Vector3 normalScale)
    {
        // Squash flat on hit
        float squashDuration  = 0.07f;
        float stretchDuration = 0.12f;
        float restoreDuration = 0.15f;

        Vector3 squash  = new Vector3(normalScale.x * 1.6f, normalScale.y * 0.4f, normalScale.z * 1.6f);
        Vector3 stretch = new Vector3(normalScale.x * 0.85f, normalScale.y * 1.25f, normalScale.z * 0.85f);

        yield return StartCoroutine(LerpScale(normalScale, squash, squashDuration));
        yield return StartCoroutine(LerpScale(squash, stretch, stretchDuration));
        yield return StartCoroutine(LerpScale(stretch, normalScale, restoreDuration));
    }

    private IEnumerator LerpScale(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        transform.localScale = to;
    }

    // ─── Impact splash particles ───────────────────────────────────────────────
    private void SpawnImpactSplash(Vector3 pos, PieceType pieceType)
    {
        Color mainColor   = GetMainColor(pieceType);
        Color emberColor  = GetEmberColor(pieceType);

        // Ring of outward-flying chunks
        int chunkCount = 16;
        for (int i = 0; i < chunkCount; i++)
        {
            float angle = (360f / chunkCount) * i;
            float rad   = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0.3f + Random.value * 0.6f, Mathf.Sin(rad));

            float speed = Random.Range(3.5f, 7f);
            float size  = Random.Range(0.06f, 0.18f);
            SpawnParticle(pos + Vector3.up * 0.15f, dir * speed, mainColor, size, Random.Range(0.5f, 1.0f));
        }

        // Small upward embers / water droplets
        int emberCount = 24;
        for (int i = 0; i < emberCount; i++)
        {
            Vector3 dir = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(1.5f, 4.5f),
                Random.Range(-1f, 1f)
            );
            float size = Random.Range(0.03f, 0.1f);
            SpawnParticle(pos + Vector3.up * 0.2f, dir * Random.Range(2f, 5f), emberColor, size, Random.Range(0.4f, 0.9f));
        }

        // Shockwave ring on the ground
        StartCoroutine(ShockwaveRing(pos, mainColor));
    }

    private void SpawnParticle(Vector3 pos, Vector3 velocity, Color color, float size, float life)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.transform.position   = pos;
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

    // ─── Shockwave ring ────────────────────────────────────────────────────────
    private IEnumerator ShockwaveRing(Vector3 pos, Color color)
    {
        // Flat ring that expands and fades
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.transform.position   = pos + Vector3.up * 0.05f;
        ring.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
        Destroy(ring.GetComponent<Collider>());

        Renderer rend = ring.GetComponent<Renderer>();
        Material mat = null;
        if (rend != null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            // Enable transparency
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
        float maxRadius = 2.2f;
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
                c.a = 1f - t;
                mat.color = c;
            }

            yield return null;
        }

        Destroy(ring);
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────
    private Color GetMainColor(PieceType t)
    {
        switch (t)
        {
            case PieceType.Lava:  return new Color(0.9f, 0.15f, 0f);  // deep red-orange
            case PieceType.Water: return new Color(0.1f, 0.55f, 1f);
            case PieceType.Block: return new Color(0.55f, 0.55f, 0.55f);
            default: return Color.white;
        }
    }

    private Color GetEmberColor(PieceType t)
    {
        switch (t)
        {
            case PieceType.Lava:  return new Color(1f, 0.4f, 0f);    // bright orange embers
            case PieceType.Water: return new Color(0.6f, 0.85f, 1f); // pale blue droplets
            case PieceType.Block: return new Color(0.75f, 0.75f, 0.75f);
            default: return Color.white;
        }
    }
}

// ─── Shared enums ──────────────────────────────────────────────────────────────
public enum PieceType { Lava, Water, Block }

// ─── Simple physics particle ───────────────────────────────────────────────────
public class SimpleParticle : MonoBehaviour
{
    private Vector3 velocity;
    private float lifetime;
    private float maxLifetime;
    private float initialSize;
    private Renderer cachedRenderer;

    public void Initialize(Vector3 initialVelocity, float life)
    {
        velocity    = initialVelocity;
        maxLifetime = life;
        lifetime    = life;
        initialSize = transform.localScale.x;
        cachedRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        transform.position += velocity * Time.deltaTime;
        velocity.y         -= 12f * Time.deltaTime; // gravity

        lifetime -= Time.deltaTime;
        float alpha = Mathf.Clamp01(lifetime / maxLifetime);

        // Shrink based on remaining life
        transform.localScale = Vector3.one * initialSize * alpha;

        if (lifetime <= 0f)
            Destroy(gameObject);
    }
}
