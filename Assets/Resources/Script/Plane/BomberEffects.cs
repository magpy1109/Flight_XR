using UnityEngine;

/// <summary>
/// 스텔스 폭격기 전용 효과.
///
/// [트레일] 비행운
/// - 엔진 배기구 4곳에서 가는 흰 비행운이 길게 이어지고 (고고도 폭격기의 하얀 비행운 느낌)
/// - 양 날개 끝에서 아주 옅은 푸른빛 와류선이 짧게 나온다.
/// - 폭격기를 장착하면 트레일 탭에서 고른 트레일 대신 이 비행운을 사용한다.
///
/// [폭발] 부딪히면
/// - 번쩍임 → 주황 불덩이 → 사방으로 튀는 불꽃 → 검은 연기가 피어오름 → 파편이 떨어짐
/// - 기존 트레일 프리팹(Resources/PlaneModel/PlaneTrails)의 파티클 머티리얼(부드러운 원)을 사용
///   (불덩이·불꽃은 빛나게 더하기, 연기·파편은 반투명)
/// </summary>
public static class BomberEffects
{
    private const string TrailPrefabPath = "PlaneModel/PlaneTrails";

    // 모델 좌표(실제 치수 m) 기준 위치
    private static readonly Vector3[] ExhaustPoints =
    {
        new Vector3(-5.45f, 0.2f, -18.1f), new Vector3(-4.35f, 0.2f, -17.9f),
        new Vector3(4.35f, 0.2f, -17.9f), new Vector3(5.45f, 0.2f, -18.1f),
    };

    private static readonly Vector3[] WingtipPoints =
    {
        new Vector3(-26.0f, 0.05f, -19.6f), new Vector3(26.0f, 0.05f, -19.6f),
    };

    private static Material glowMaterial;
    private static Material alphaMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        glowMaterial = null;
        alphaMaterial = null;
    }

    // ---------- 머티리얼 ----------

    private static Material Glow()
    {
        if (glowMaterial != null)
            return glowMaterial;

        GameObject prefab = Resources.Load<GameObject>(TrailPrefabPath);
        if (prefab != null)
        {
            ParticleSystemRenderer r = prefab.GetComponentInChildren<ParticleSystemRenderer>(true);
            if (r != null)
                glowMaterial = r.sharedMaterial;
        }

        if (glowMaterial == null)
            Debug.LogWarning("[BomberEffects] 파티클 머티리얼을 찾지 못했습니다.");

        return glowMaterial;
    }

    /// <summary>같은 머티리얼을 반투명(알파 블렌드)으로 바꾼 복사본 (연기 / 비행운용)</summary>
    private static Material Alpha()
    {
        if (alphaMaterial != null)
            return alphaMaterial;

        Material glow = Glow();
        if (glow == null)
            return null;

        alphaMaterial = new Material(glow);
        alphaMaterial.name = "BomberSmoke_Mat";
        alphaMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        alphaMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        alphaMaterial.SetFloat("_Blend", 0f);
        return alphaMaterial;
    }

    // ---------- 비행운 ----------

    /// <summary>모델(실제 치수 좌표)에 비행운 붙이기. 반환 : 붙인 TrailRenderer들</summary>
    public static TrailRenderer[] AttachContrails(Transform model)
    {
        var list = new System.Collections.Generic.List<TrailRenderer>();

        foreach (Vector3 p in ExhaustPoints)
            list.Add(CreateTrail(model, p, "Contrail", 0.0045f, 1.6f,
                new Color(1f, 1f, 1f, 0.75f), new Color(0.92f, 0.95f, 1f, 0f)));

        foreach (Vector3 p in WingtipPoints)
            list.Add(CreateTrail(model, p, "WingtipVortex", 0.0018f, 0.55f,
                new Color(0.75f, 0.88f, 1f, 0.45f), new Color(0.75f, 0.88f, 1f, 0f)));

        return list.ToArray();
    }

    private static TrailRenderer CreateTrail(Transform model, Vector3 localPoint, string name, float width, float time, Color start, Color end)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(model, false);
        go.transform.localPosition = localPoint;

        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = Alpha();
        trail.time = time;
        trail.minVertexDistance = 0.01f;
        trail.widthMultiplier = 1f;
        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, width * 0.6f),
            new Keyframe(0.15f, width),
            new Keyframe(1f, width * 2.2f));   // 뒤로 갈수록 퍼짐

        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(start.a, 0.08f), new GradientAlphaKey(start.a * 0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = g;

        trail.textureMode = LineTextureMode.Stretch;
        trail.alignment = LineAlignment.View;
        trail.numCapVertices = 2;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = true;
        return trail;
    }

    /// <summary>비행운을 비행기에서 떼어내 남은 꼬리만 자연스럽게 사라지게</summary>
    public static void ReleaseContrails(TrailRenderer[] trails)
    {
        if (trails == null)
            return;

        foreach (TrailRenderer t in trails)
        {
            if (t == null)
                continue;

            t.emitting = false;
            t.transform.SetParent(null, true);
            t.autodestruct = true;
            Object.Destroy(t.gameObject, t.time + 0.5f);
        }
    }

    // ---------- 폭발 ----------

    /// <summary>폭발 효과 (size = 비행기 크기 m, 기본 0.24)</summary>
    public static void Explode(Vector3 position, Vector3 impact, float size)
    {
        float s = Mathf.Max(size, 0.05f) / 0.24f;

        GameObject root = new GameObject("BomberExplosion");
        root.transform.position = position - impact.normalized * 0.02f;

        Material glow = Glow();
        Material alpha = Alpha();

        // 1) 번쩍임
        Burst(root.transform, "Flash", glow, 1, 0f,
            life: new Vector2(0.12f, 0.16f), speed: Vector2.zero, size: new Vector2(0.32f, 0.36f) * s,
            colors: new[] { new Color(1f, 1f, 0.9f, 1f), new Color(1f, 0.8f, 0.4f, 0f) },
            growth: 1.4f, gravity: 0f, radius: 0f, stretch: false);

        // 2) 불덩이
        Burst(root.transform, "Fireball", glow, 34, 0f,
            life: new Vector2(0.45f, 0.85f), speed: new Vector2(0.12f, 0.55f) * s, size: new Vector2(0.05f, 0.11f) * s,
            colors: new[] { new Color(1f, 0.95f, 0.75f, 1f), new Color(1f, 0.55f, 0.12f, 0.9f), new Color(0.55f, 0.12f, 0.02f, 0f) },
            growth: 2.0f, gravity: -0.08f, radius: 0.03f * s, stretch: false);

        // 3) 불꽃 (길게 늘어나는 점)
        Burst(root.transform, "Sparks", glow, 28, 0f,
            life: new Vector2(0.5f, 1.0f), speed: new Vector2(0.9f, 1.8f) * s, size: new Vector2(0.008f, 0.016f) * s,
            colors: new[] { new Color(1f, 0.95f, 0.6f, 1f), new Color(1f, 0.5f, 0.1f, 0f) },
            growth: 0.6f, gravity: 0.7f, radius: 0.02f * s, stretch: true);

        // 4) 연기
        Burst(root.transform, "Smoke", alpha, 20, 0.06f,
            life: new Vector2(1.6f, 2.4f), speed: new Vector2(0.04f, 0.18f) * s, size: new Vector2(0.07f, 0.13f) * s,
            colors: new[] { new Color(0.12f, 0.11f, 0.11f, 0.85f), new Color(0.25f, 0.24f, 0.24f, 0.55f), new Color(0.4f, 0.4f, 0.4f, 0f) },
            growth: 2.6f, gravity: -0.04f, radius: 0.04f * s, stretch: false);

        // 5) 파편 (어두운 조각이 떨어짐)
        Burst(root.transform, "Debris", alpha, 16, 0f,
            life: new Vector2(0.9f, 1.4f), speed: new Vector2(0.5f, 1.1f) * s, size: new Vector2(0.01f, 0.022f) * s,
            colors: new[] { new Color(0.1f, 0.1f, 0.11f, 1f), new Color(0.1f, 0.1f, 0.11f, 0f) },
            growth: 1f, gravity: 1.2f, radius: 0.03f * s, stretch: false);

        Object.Destroy(root, 3.5f);
    }

    private static void Burst(Transform parent, string name, Material material, int count, float delay,
        Vector2 life, Vector2 speed, Vector2 size, Color[] colors, float growth, float gravity, float radius, bool stretch)
    {
        if (material == null)
            return;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.2f;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = count + 4;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = radius > 0f;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(radius, 0.001f);

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        var ck = new GradientColorKey[colors.Length];
        var ak = new GradientAlphaKey[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            float t = colors.Length == 1 ? 0f : i / (float)(colors.Length - 1);
            ck[i] = new GradientColorKey(colors[i], t);
            ak[i] = new GradientAlphaKey(colors[i].a, t);
        }
        g.SetKeys(ck, ak);
        col.color = new ParticleSystem.MinMaxGradient(g);

        ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, growth));

        // 공기 저항 : 처음엔 빠르게 퍼지고 곧 느려짐
        ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
        limit.enabled = !stretch;
        limit.dampen = 0.15f;
        limit.limit = 0.05f;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        if (stretch)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.08f;
            renderer.lengthScale = 1.5f;
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        ps.Play();
    }
}
