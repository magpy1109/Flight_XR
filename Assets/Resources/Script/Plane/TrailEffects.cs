using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 코드로 만드는 트레일 (TrailCatalog 9 ~ 20번).
///
/// 기존 트레일 프리팹(PlaneTrails)과 같은 기준으로 만든다.
/// - 줄기가 나오는 자리(emitter)의 +Z 방향으로 뿜는다. (비행기에서는 뒤쪽을 보도록 돌려서 붙임)
/// - 속도 / 크기는 프리팹과 같은 단위 (기존 불꽃 : 속도 8, 수명 0.33, 크기 0.1)
/// - 비행기와 함께 움직이는 줄기 (Local) → 스킨씬 미리보기에서도 같은 모양으로 보인다.
/// - 파티클 머티리얼은 기존 트레일 프리팹의 것을 사용 (빛나는 것 / 반투명)
/// </summary>
public static class TrailEffects
{
    // 프리팹의 줄기 크기 (Left_Trail / Right_Trail 의 localScale)
    private const float EmitterScale = 0.525408f;

    private class Layer
    {
        public string name;
        public bool glow;                 // true = 빛나게 더하기, false = 반투명
        public float rate;                // 초당 개수
        public Vector2 life;
        public Vector2 speed;
        public Vector2 size;
        public Color[] colors;            // 수명에 따른 색 (알파 포함)
        public float growth = 1f;         // 수명 끝의 크기 배율
        public float cone = 0.5f;         // 퍼지는 각도
        public float gravity;
        public bool stretch;              // 길게 늘어나는 줄기
        public Vector3 offset;            // 줄기 안에서의 위치 (무지개 줄)
    }

    /// <summary>
    /// 비행기에 트레일 붙이기. anchor = 좌우 줄기가 나오는 자리 (x = 가운데에서 좌우 거리, 모델 길이 1 기준).
    /// 좌우 간격이 아주 좁으면 가운데 한 줄기만 만든다.
    /// </summary>
    public static ParticleSystem[] Attach(Transform visual, int trailId, float length, Vector3 anchor)
    {
        GameObject root = new GameObject("Trail");
        root.layer = visual.gameObject.layer;
        root.transform.SetParent(visual, false);
        root.transform.localScale = Vector3.one * length;

        float x = Mathf.Abs(anchor.x);
        float[] sides = x < 0.02f ? new[] { 0f } : new[] { -1f, 1f };

        foreach (float side in sides)
        {
            GameObject emitter = new GameObject(side < 0f ? "Left_Trail" : side > 0f ? "Right_Trail" : "Center_Trail");
            emitter.layer = root.layer;
            emitter.transform.SetParent(root.transform, false);
            emitter.transform.localPosition = new Vector3(side * x, anchor.y, anchor.z);
            emitter.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // 뒤쪽으로 뿜기
            emitter.transform.localScale = Vector3.one * EmitterScale;

            Build(emitter.transform, trailId);
        }

        return root.GetComponentsInChildren<ParticleSystem>(true);
    }

    /// <summary>줄기 하나 만들기 (emitter의 +Z 방향으로 뿜는다)</summary>
    public static void Build(Transform emitter, int trailId)
    {
        foreach (Layer layer in Layers(trailId))
            AddLayer(emitter, layer);
    }

    // ---------- 트레일별 모양 ----------

    private static List<Layer> Layers(int trailId)
    {
        var list = new List<Layer>();

        switch (trailId)
        {
            case 10:   // 로켓 화염 : 밝은 불꽃 + 회색 연기
                list.Add(new Layer { name = "Flame", glow = true, rate = 110f, stretch = true, cone = 2f,
                    life = new Vector2(0.18f, 0.26f), speed = new Vector2(8f, 10f), size = new Vector2(0.14f, 0.18f),
                    colors = new[] { new Color(1f, 0.97f, 0.7f, 1f), new Color(1f, 0.5f, 0.1f, 0.85f), new Color(0.6f, 0.1f, 0f, 0f) } });
                list.Add(new Layer { name = "Smoke", glow = false, rate = 38f, cone = 6f, growth = 2.6f,
                    life = new Vector2(0.8f, 1.1f), speed = new Vector2(3.5f, 4.5f), size = new Vector2(0.12f, 0.16f),
                    colors = new[] { new Color(0.55f, 0.55f, 0.56f, 0f), new Color(0.6f, 0.6f, 0.62f, 0.45f), new Color(0.75f, 0.75f, 0.76f, 0f) } });
                break;

            case 11:   // 제트 불꽃 : 푸른 애프터버너
                list.Add(new Layer { name = "Core", glow = true, rate = 90f, stretch = true, cone = 1f,
                    life = new Vector2(0.12f, 0.18f), speed = new Vector2(9f, 11f), size = new Vector2(0.06f, 0.08f),
                    colors = new[] { new Color(1f, 1f, 1f, 1f), new Color(0.7f, 0.9f, 1f, 0f) } });
                list.Add(new Layer { name = "Flame", glow = true, rate = 110f, stretch = true, cone = 2f,
                    life = new Vector2(0.24f, 0.32f), speed = new Vector2(8f, 10f), size = new Vector2(0.1f, 0.13f),
                    colors = new[] { new Color(0.7f, 0.92f, 1f, 1f), new Color(0.2f, 0.55f, 1f, 0.9f), new Color(0.1f, 0.2f, 0.9f, 0f) } });
                break;

            case 12:   // 구름 줄기 : 하얀 수증기
                list.Add(new Layer { name = "Vapor", glow = false, rate = 70f, cone = 3f, growth = 2.0f,
                    life = new Vector2(0.6f, 0.8f), speed = new Vector2(4.5f, 5.5f), size = new Vector2(0.08f, 0.1f),
                    colors = new[] { new Color(1f, 1f, 1f, 0.85f), new Color(1f, 1f, 1f, 0.45f), new Color(1f, 1f, 1f, 0f) } });
                break;

            case 13:   // 보랏빛 별가루
                list.Add(new Layer { name = "Streak", glow = true, rate = 60f, stretch = true, cone = 1.5f,
                    life = new Vector2(0.25f, 0.35f), speed = new Vector2(7f, 8f), size = new Vector2(0.06f, 0.08f),
                    colors = new[] { new Color(0.8f, 0.5f, 1f, 0.9f), new Color(0.5f, 0.25f, 0.95f, 0f) } });
                list.Add(new Layer { name = "Stars", glow = true, rate = 45f, cone = 14f, growth = 0.3f,
                    life = new Vector2(0.6f, 0.9f), speed = new Vector2(3f, 4.5f), size = new Vector2(0.05f, 0.11f),
                    colors = new[] { new Color(1f, 0.85f, 1f, 1f), new Color(0.7f, 0.35f, 1f, 0.9f), new Color(0.4f, 0.2f, 0.9f, 0f) } });
                break;

            case 14:   // 황금 동전 : 동전 + 지폐 조각 + 반짝임
                list.Add(new Layer { name = "Coins", glow = false, rate = 28f, cone = 12f, gravity = 0.6f,
                    life = new Vector2(0.7f, 1.0f), speed = new Vector2(3f, 4f), size = new Vector2(0.08f, 0.1f),
                    colors = new[] { new Color(1f, 0.85f, 0.2f, 1f), new Color(1f, 0.78f, 0.15f, 1f), new Color(0.9f, 0.65f, 0.1f, 0f) } });
                list.Add(new Layer { name = "Bills", glow = false, rate = 14f, cone = 16f, gravity = 0.25f,
                    life = new Vector2(0.8f, 1.1f), speed = new Vector2(2.5f, 3.5f), size = new Vector2(0.1f, 0.13f),
                    colors = new[] { new Color(0.45f, 0.72f, 0.42f, 1f), new Color(0.4f, 0.65f, 0.38f, 0.9f), new Color(0.4f, 0.65f, 0.38f, 0f) } });
                list.Add(new Layer { name = "Shine", glow = true, rate = 30f, cone = 10f, growth = 0.3f,
                    life = new Vector2(0.3f, 0.5f), speed = new Vector2(3.5f, 5f), size = new Vector2(0.04f, 0.07f),
                    colors = new[] { new Color(1f, 0.95f, 0.6f, 1f), new Color(1f, 0.85f, 0.3f, 0f) } });
                break;

            case 15:   // 은빛 반짝이
                list.Add(new Layer { name = "Glitter", glow = true, rate = 60f, cone = 16f, growth = 0.3f,
                    life = new Vector2(0.45f, 0.7f), speed = new Vector2(3.5f, 5f), size = new Vector2(0.04f, 0.09f),
                    colors = new[] { new Color(1f, 1f, 1f, 1f), new Color(0.75f, 0.85f, 1f, 0.9f), new Color(0.7f, 0.8f, 1f, 0f) } });
                list.Add(new Layer { name = "Streak", glow = true, rate = 50f, stretch = true, cone = 1.5f,
                    life = new Vector2(0.2f, 0.3f), speed = new Vector2(7f, 8f), size = new Vector2(0.05f, 0.06f),
                    colors = new[] { new Color(0.9f, 0.95f, 1f, 0.7f), new Color(0.7f, 0.8f, 1f, 0f) } });
                break;

            case 16:   // 여객기 비행운 : 가늘고 긴 흰 줄
                list.Add(new Layer { name = "Contrail", glow = false, rate = 70f, cone = 1.5f, growth = 2.6f,
                    life = new Vector2(1.0f, 1.3f), speed = new Vector2(4f, 5f), size = new Vector2(0.07f, 0.09f),
                    colors = new[] { new Color(1f, 1f, 1f, 0.9f), new Color(1f, 1f, 1f, 0.5f), new Color(1f, 1f, 1f, 0f) } });
                break;

            case 17:   // 초음속 불꽃 : 길게 뻗는 주황 불꽃 + 옅은 연기
                list.Add(new Layer { name = "Flame", glow = true, rate = 120f, stretch = true, cone = 1.5f,
                    life = new Vector2(0.26f, 0.34f), speed = new Vector2(10f, 12f), size = new Vector2(0.11f, 0.14f),
                    colors = new[] { new Color(1f, 1f, 0.9f, 1f), new Color(1f, 0.6f, 0.15f, 0.9f), new Color(0.8f, 0.15f, 0.05f, 0f) } });
                list.Add(new Layer { name = "Haze", glow = false, rate = 24f, cone = 4f, growth = 2.2f,
                    life = new Vector2(0.6f, 0.8f), speed = new Vector2(4f, 5f), size = new Vector2(0.1f, 0.12f),
                    colors = new[] { new Color(0.7f, 0.7f, 0.72f, 0f), new Color(0.75f, 0.75f, 0.77f, 0.3f), new Color(0.85f, 0.85f, 0.86f, 0f) } });
                break;

            case 18:   // 증기 구름 : 몽글몽글한 흰 덩어리
                list.Add(new Layer { name = "Puffs", glow = false, rate = 9f, cone = 8f, growth = 1.8f,
                    life = new Vector2(1.1f, 1.4f), speed = new Vector2(1.8f, 2.6f), size = new Vector2(0.24f, 0.32f),
                    colors = new[] { new Color(1f, 1f, 1f, 0.9f), new Color(0.96f, 0.97f, 1f, 0.55f), new Color(0.95f, 0.96f, 1f, 0f) } });
                break;

            case 19:   // 흙먼지 + 나뭇잎
                list.Add(new Layer { name = "Dust", glow = false, rate = 26f, cone = 14f, growth = 2.2f,
                    life = new Vector2(0.7f, 1.0f), speed = new Vector2(2.5f, 3.5f), size = new Vector2(0.14f, 0.18f),
                    colors = new[] { new Color(0.62f, 0.52f, 0.38f, 0.65f), new Color(0.7f, 0.62f, 0.5f, 0.35f), new Color(0.8f, 0.75f, 0.65f, 0f) } });
                list.Add(new Layer { name = "Leaves", glow = false, rate = 9f, cone = 20f, gravity = 0.4f,
                    life = new Vector2(0.8f, 1.1f), speed = new Vector2(2.5f, 4f), size = new Vector2(0.06f, 0.09f),
                    colors = new[] { new Color(0.35f, 0.6f, 0.25f, 1f), new Color(0.4f, 0.55f, 0.2f, 0.9f), new Color(0.4f, 0.5f, 0.2f, 0f) } });
                break;

            case 20:   // 무지개 : 여섯 색 줄이 나란히
                for (int i = 0; i < Rainbow.Length; i++)
                {
                    Color c = Rainbow[i];
                    list.Add(new Layer { name = "Rainbow" + i, glow = false, rate = 70f, stretch = true, cone = 0.2f,
                        life = new Vector2(0.5f, 0.5f), speed = new Vector2(6f, 6f), size = new Vector2(0.065f, 0.065f),
                        offset = new Vector3(0f, (2.5f - i) * 0.055f, 0f),
                        colors = new[] { new Color(c.r, c.g, c.b, 1f), new Color(c.r, c.g, c.b, 0.9f), new Color(c.r, c.g, c.b, 0f) } });
                }
                break;

            default:   // 9번 폭격기 비행운 (다른 비행기에 붙일 때) : 하얗고 긴 비행운
                list.Add(new Layer { name = "Contrail", glow = false, rate = 60f, cone = 2f, growth = 2.2f,
                    life = new Vector2(0.8f, 1.0f), speed = new Vector2(4.5f, 5.5f), size = new Vector2(0.09f, 0.11f),
                    colors = new[] { new Color(1f, 1f, 1f, 0.75f), new Color(1f, 1f, 1f, 0.4f), new Color(1f, 1f, 1f, 0f) } });
                break;
        }

        return list;
    }

    private static readonly Color[] Rainbow =
    {
        new Color(1f, 0.2f, 0.2f), new Color(1f, 0.6f, 0.1f), new Color(1f, 0.92f, 0.15f),
        new Color(0.25f, 0.85f, 0.3f), new Color(0.2f, 0.55f, 1f), new Color(0.6f, 0.35f, 1f),
    };

    // ---------- 파티클 ----------

    private static void AddLayer(Transform emitter, Layer layer)
    {
        Material material = layer.glow ? BomberEffects.Glow() : BomberEffects.Alpha();
        if (material == null)
            return;

        GameObject go = new GameObject(layer.name);
        go.layer = emitter.gameObject.layer;
        go.transform.SetParent(emitter, false);
        go.transform.localPosition = layer.offset;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(layer.life.x, layer.life.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(layer.speed.x, layer.speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(layer.size.x, layer.size.y);
        main.startRotation = layer.stretch
            ? new ParticleSystem.MinMaxCurve(0f)
            : new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.gravityModifier = layer.gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = Mathf.CeilToInt(layer.rate * layer.life.y) + 16;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = layer.rate;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = Mathf.Max(layer.cone, 0.01f);
        shape.radius = 0.001f;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        var ck = new GradientColorKey[layer.colors.Length];
        var ak = new GradientAlphaKey[layer.colors.Length];
        for (int i = 0; i < layer.colors.Length; i++)
        {
            float t = layer.colors.Length == 1 ? 0f : i / (float)(layer.colors.Length - 1);
            ck[i] = new GradientColorKey(layer.colors[i], t);
            ak[i] = new GradientAlphaKey(layer.colors[i].a, t);
        }
        g.SetKeys(ck, ak);
        col.color = new ParticleSystem.MinMaxGradient(g);

        if (!Mathf.Approximately(layer.growth, 1f))
        {
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, layer.growth));
        }

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        if (layer.stretch)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0f;
            renderer.lengthScale = 8f;   // 기존 트레일 프리팹과 같은 길이
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        ps.Play();
    }
}
