using UnityEngine;

/// <summary>
/// 스킨 종류별 충돌 파티클 효과 (PlaneCrashEffect에서 호출).
/// 폭발(BomberEffects.Explode)과 같은 파티클 도구 / 머티리얼을 쓴다.
/// size = 비행기 크기(m). 0.24m 기준으로 만들고 크기에 맞춰 키우거나 줄인다.
/// </summary>
public static class CrashEffects
{
    private static float Scale(float size)
    {
        return Mathf.Max(size, 0.05f) / 0.24f;
    }

    private static Transform Root(string name, Vector3 position, float life)
    {
        GameObject root = new GameObject(name);
        root.transform.position = position;
        Object.Destroy(root, life);
        return root.transform;
    }

    /// <summary>여객기 / 콩코드 : 번쩍 + 불꽃 + 회색 연기 + 금속 조각</summary>
    public static void MetalImpact(Vector3 position, Vector3 impact, float size)
    {
        float s = Scale(size);
        Transform root = Root("MetalImpact", position - impact.normalized * 0.02f, 3.5f);
        Material glow = BomberEffects.Glow();
        Material alpha = BomberEffects.Alpha();

        BomberEffects.Burst(root, "Flash", glow, 1, 0f,
            life: new Vector2(0.08f, 0.12f), speed: Vector2.zero, size: new Vector2(0.16f, 0.2f) * s,
            colors: new[] { new Color(1f, 0.97f, 0.85f, 0.9f), new Color(1f, 0.8f, 0.4f, 0f) },
            growth: 1.3f, gravity: 0f, radius: 0f, stretch: false);

        BomberEffects.Burst(root, "Sparks", glow, 26, 0f,
            life: new Vector2(0.35f, 0.8f), speed: new Vector2(0.7f, 1.6f) * s, size: new Vector2(0.006f, 0.012f) * s,
            colors: new[] { new Color(1f, 0.97f, 0.7f, 1f), new Color(1f, 0.6f, 0.15f, 0f) },
            growth: 0.6f, gravity: 0.9f, radius: 0.02f * s, stretch: true);

        BomberEffects.Burst(root, "Smoke", alpha, 14, 0.04f,
            life: new Vector2(1.2f, 2.0f), speed: new Vector2(0.03f, 0.14f) * s, size: new Vector2(0.05f, 0.1f) * s,
            colors: new[] { new Color(0.3f, 0.3f, 0.31f, 0.7f), new Color(0.5f, 0.5f, 0.5f, 0.4f), new Color(0.7f, 0.7f, 0.7f, 0f) },
            growth: 2.4f, gravity: -0.05f, radius: 0.03f * s, stretch: false);

        BomberEffects.Burst(root, "Debris", alpha, 12, 0f,
            life: new Vector2(0.8f, 1.3f), speed: new Vector2(0.4f, 1.0f) * s, size: new Vector2(0.008f, 0.018f) * s,
            colors: new[] { new Color(0.85f, 0.86f, 0.88f, 1f), new Color(0.6f, 0.6f, 0.62f, 0f) },
            growth: 1f, gravity: 1.2f, radius: 0.03f * s, stretch: false);
    }

    /// <summary>달러 비행기 : 금빛 동전 + 초록 지폐 조각</summary>
    public static void Coins(Vector3 position, float size)
    {
        float s = Scale(size);
        Transform root = Root("CoinBurst", position, 3.5f);
        Material glow = BomberEffects.Glow();
        Material alpha = BomberEffects.Alpha();

        BomberEffects.Burst(root, "Coins", alpha, 18, 0f,
            life: new Vector2(0.8f, 1.3f), speed: new Vector2(0.5f, 1.2f) * s, size: new Vector2(0.014f, 0.022f) * s,
            colors: new[] { new Color(1f, 0.84f, 0.2f, 1f), new Color(1f, 0.78f, 0.15f, 1f), new Color(0.9f, 0.65f, 0.1f, 0f) },
            growth: 1f, gravity: 1.3f, radius: 0.03f * s, stretch: false);

        BomberEffects.Burst(root, "Shine", glow, 10, 0f,
            life: new Vector2(0.25f, 0.5f), speed: new Vector2(0.3f, 0.9f) * s, size: new Vector2(0.008f, 0.014f) * s,
            colors: new[] { new Color(1f, 0.95f, 0.6f, 1f), new Color(1f, 0.85f, 0.3f, 0f) },
            growth: 0.5f, gravity: 0.2f, radius: 0.02f * s, stretch: false);

        BomberEffects.Burst(root, "Bills", alpha, 10, 0.03f,
            life: new Vector2(1.4f, 2.2f), speed: new Vector2(0.15f, 0.5f) * s, size: new Vector2(0.02f, 0.03f) * s,
            colors: new[] { new Color(0.45f, 0.72f, 0.42f, 1f), new Color(0.4f, 0.65f, 0.38f, 0.9f), new Color(0.4f, 0.65f, 0.38f, 0f) },
            growth: 1f, gravity: 0.25f, radius: 0.04f * s, stretch: false);
    }

    /// <summary>은박 오리가미 : 은빛 반짝이</summary>
    public static void Glitter(Vector3 position, float size)
    {
        float s = Scale(size);
        Transform root = Root("FoilGlitter", position, 2.5f);
        Material glow = BomberEffects.Glow();

        BomberEffects.Burst(root, "Glitter", glow, 26, 0f,
            life: new Vector2(0.4f, 0.9f), speed: new Vector2(0.3f, 1.0f) * s, size: new Vector2(0.006f, 0.014f) * s,
            colors: new[] { new Color(1f, 1f, 1f, 1f), new Color(0.75f, 0.85f, 1f, 0.9f), new Color(0.7f, 0.8f, 1f, 0f) },
            growth: 0.4f, gravity: 0.5f, radius: 0.03f * s, stretch: false);
    }

    /// <summary>공룡 : 흙먼지</summary>
    public static void Dust(Vector3 position, float size)
    {
        float s = Scale(size);
        Transform root = Root("DustPuff", position, 3f);
        Material alpha = BomberEffects.Alpha();

        BomberEffects.Burst(root, "Dust", alpha, 18, 0f,
            life: new Vector2(0.9f, 1.6f), speed: new Vector2(0.1f, 0.4f) * s, size: new Vector2(0.05f, 0.1f) * s,
            colors: new[] { new Color(0.62f, 0.52f, 0.38f, 0.7f), new Color(0.7f, 0.62f, 0.5f, 0.4f), new Color(0.8f, 0.75f, 0.65f, 0f) },
            growth: 2.2f, gravity: -0.02f, radius: 0.04f * s, stretch: false);

        BomberEffects.Burst(root, "Pebbles", alpha, 10, 0f,
            life: new Vector2(0.6f, 1.0f), speed: new Vector2(0.4f, 0.9f) * s, size: new Vector2(0.008f, 0.016f) * s,
            colors: new[] { new Color(0.4f, 0.33f, 0.25f, 1f), new Color(0.4f, 0.33f, 0.25f, 0f) },
            growth: 1f, gravity: 1.4f, radius: 0.03f * s, stretch: false);
    }

    /// <summary>비행선 : 터진 자리에서 새어 나오는 흰 가스</summary>
    public static void GasPuff(Vector3 position, float size)
    {
        float s = Scale(size);
        Transform root = Root("GasPuff", position, 2.5f);
        Material alpha = BomberEffects.Alpha();

        BomberEffects.Burst(root, "Gas", alpha, 12, 0f,
            life: new Vector2(0.6f, 1.1f), speed: new Vector2(0.15f, 0.5f) * s, size: new Vector2(0.04f, 0.08f) * s,
            colors: new[] { new Color(1f, 1f, 1f, 0.75f), new Color(0.95f, 0.97f, 1f, 0.4f), new Color(0.95f, 0.97f, 1f, 0f) },
            growth: 2.2f, gravity: -0.06f, radius: 0.03f * s, stretch: false);
    }

    private static readonly Color[] Rainbow =
    {
        new Color(1f, 0.25f, 0.25f), new Color(1f, 0.6f, 0.15f), new Color(1f, 0.92f, 0.2f),
        new Color(0.3f, 0.9f, 0.35f), new Color(0.25f, 0.6f, 1f), new Color(0.65f, 0.4f, 1f),
    };

    /// <summary>냥캣 : 흰 번쩍임 + 무지개 별</summary>
    public static void RainbowBurst(Vector3 position, float size)
    {
        float s = Scale(size);
        Transform root = Root("RainbowBurst", position, 3f);
        Material glow = BomberEffects.Glow();

        BomberEffects.Burst(root, "Flash", glow, 1, 0f,
            life: new Vector2(0.14f, 0.18f), speed: Vector2.zero, size: new Vector2(0.22f, 0.26f) * s,
            colors: new[] { new Color(1f, 1f, 1f, 0.95f), new Color(1f, 0.8f, 1f, 0f) },
            growth: 1.5f, gravity: 0f, radius: 0f, stretch: false);

        for (int i = 0; i < Rainbow.Length; i++)
        {
            Color c = Rainbow[i];

            BomberEffects.Burst(root, "Stars" + i, glow, 7, 0f,
                life: new Vector2(0.6f, 1.1f), speed: new Vector2(0.5f, 1.3f) * s, size: new Vector2(0.014f, 0.026f) * s,
                colors: new[] { new Color(c.r, c.g, c.b, 1f), new Color(c.r, c.g, c.b, 0.9f), new Color(c.r, c.g, c.b, 0f) },
                growth: 0.5f, gravity: 0.35f, radius: 0.03f * s, stretch: false);
        }
    }
}
