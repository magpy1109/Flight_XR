using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 종이비행기가 아닌 다른 비행기 모델 (여러 색 부위로 나뉜 메시).
///
/// 모델 데이터 형식 "PPM2" (Resources/PlaneModel/*.bytes)
///   'PPM2', 꼭짓점 수, 부위 수, 꼭짓점(float3), 법선(float3),
///   부위마다 : 색(float4), 인덱스 수, 인덱스
/// 부위마다 색이 다르므로 같은 머티리얼(PlaneSkin_Mat)에 부위별 색만 덮어써서 그린다.
///
/// 텍스처 모델 데이터 형식 "PPM3" (Resources/PlaneModel/이름/이름Mesh.bytes, 10번부터의 스킨 / PlaneSkins 참고)
///   'PPM3', 꼭짓점 수, 인덱스 수, 꼭짓점(float3), 법선(float3), UV(float2), 인덱스(int)
///   기수 +Z, 위 +Y, 가운데 정렬, 가장 긴 변 1. 색은 이름_BaseColor.png 텍스처를 넣은 스킨 전용 머티리얼로 그린다.
///
/// - 스텔스 폭격기 : Resources/PlaneModel/StealthBomberMesh.bytes
///   (B-2 계열 무미익 전익기 형태를 실제 치수 기준으로 만든 모델. 날개폭 52.4m / 길이 21m / 앞전 후퇴각 33°,
///    뒷전 이중 W 톱니, 조종석 창 4개, 톱니 모양 엔진 흡입구 2개, 상면 배기구, 엘레본 구분색. 실제 표식/번호 없음)
/// </summary>
public static class PlaneModels
{
    public const string BomberMeshPath = "PlaneModel/StealthBomberMesh";
    private const string MaterialPath = "PlaneModel/PlaneSkin_Mat";

    public class Model
    {
        public Mesh mesh;
        public Color[] colors;
    }

    private static readonly Dictionary<string, Model> cache = new Dictionary<string, Model>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cache.Clear();
        texturedCache.Clear();
        texturedMaterials.Clear();
    }

    public static Model Load(string path)
    {
        Model model;
        if (cache.TryGetValue(path, out model) && model != null && model.mesh != null)
            return model;

        TextAsset data = Resources.Load<TextAsset>(path);
        if (data == null)
        {
            Debug.LogError($"[PlaneModels] Resources/{path} 모델 데이터를 찾지 못했습니다.");
            return null;
        }

        byte[] b = data.bytes;
        if (b.Length < 12 || b[0] != 'P' || b[1] != 'P' || b[2] != 'M' || b[3] != '2')
        {
            Debug.LogError($"[PlaneModels] {path} 모델 데이터 형식이 올바르지 않습니다.");
            return null;
        }

        int vertexCount = System.BitConverter.ToInt32(b, 4);
        int subCount = System.BitConverter.ToInt32(b, 8);
        int offset = 12;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];

        for (int i = 0; i < vertexCount; i++, offset += 12)
            vertices[i] = new Vector3(
                System.BitConverter.ToSingle(b, offset),
                System.BitConverter.ToSingle(b, offset + 4),
                System.BitConverter.ToSingle(b, offset + 8));

        for (int i = 0; i < vertexCount; i++, offset += 12)
            normals[i] = new Vector3(
                System.BitConverter.ToSingle(b, offset),
                System.BitConverter.ToSingle(b, offset + 4),
                System.BitConverter.ToSingle(b, offset + 8));

        Mesh mesh = new Mesh();
        mesh.name = System.IO.Path.GetFileName(path);
        if (vertexCount > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.subMeshCount = subCount;

        Color[] colors = new Color[subCount];

        for (int s = 0; s < subCount; s++)
        {
            colors[s] = new Color(
                System.BitConverter.ToSingle(b, offset),
                System.BitConverter.ToSingle(b, offset + 4),
                System.BitConverter.ToSingle(b, offset + 8),
                System.BitConverter.ToSingle(b, offset + 12));
            offset += 16;

            int count = System.BitConverter.ToInt32(b, offset);
            offset += 4;

            int[] indices = new int[count];
            for (int i = 0; i < count; i++, offset += 4)
                indices[i] = System.BitConverter.ToInt32(b, offset);

            mesh.SetTriangles(indices, s, false);
        }

        mesh.RecalculateBounds();

        model = new Model { mesh = mesh, colors = colors };
        cache[path] = model;
        return model;
    }

    /// <summary>
    /// 모델 오브젝트 만들기 (부위별 색 적용). 날개폭(x 크기)이 span(m)이 되도록 맞추고 가운데 정렬.
    /// </summary>
    public static GameObject Create(string path, Transform parent, float span, out MeshFilter filter)
    {
        filter = null;

        Model model = Load(path);
        if (model == null)
            return null;

        GameObject go = new GameObject("Model", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);

        filter = go.GetComponent<MeshFilter>();
        filter.sharedMesh = model.mesh;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        Material material = Resources.Load<Material>(MaterialPath);

        Material[] materials = new Material[model.colors.Length];
        for (int i = 0; i < materials.Length; i++)
            materials[i] = material;
        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        for (int i = 0; i < model.colors.Length; i++)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", model.colors[i]);
            block.SetColor("_Color", model.colors[i]);
            renderer.SetPropertyBlock(block, i);
        }

        Bounds bounds = model.mesh.bounds;
        float scale = span / Mathf.Max(bounds.size.x, 0.0001f);
        go.transform.localScale = Vector3.one * scale;
        go.transform.localPosition = -bounds.center * scale;
        go.transform.localRotation = Quaternion.identity;

        return go;
    }

    // ---------- 텍스처 모델 (PPM3) ----------

    private static readonly Dictionary<string, Mesh> texturedCache = new Dictionary<string, Mesh>();

    /// <summary>텍스처 모델 메시 불러오기 (한 번만 만들고 재사용)</summary>
    public static Mesh LoadTextured(string path)
    {
        Mesh mesh;
        if (texturedCache.TryGetValue(path, out mesh) && mesh != null)
            return mesh;

        TextAsset data = Resources.Load<TextAsset>(path);
        if (data == null)
        {
            Debug.LogError($"[PlaneModels] Resources/{path} 모델 데이터를 찾지 못했습니다.");
            return null;
        }

        byte[] b = data.bytes;
        if (b.Length < 12 || b[0] != 'P' || b[1] != 'P' || b[2] != 'M' || b[3] != '3')
        {
            Debug.LogError($"[PlaneModels] {path} 모델 데이터 형식이 올바르지 않습니다.");
            return null;
        }

        int vertexCount = System.BitConverter.ToInt32(b, 4);
        int indexCount = System.BitConverter.ToInt32(b, 8);

        const int floatsPerVertex = 8;   // 꼭짓점 3 + 법선 3 + UV 2
        long floatBytes = (long)vertexCount * floatsPerVertex * 4;
        long indexBytes = (long)indexCount * 4;

        if (vertexCount <= 0 || indexCount <= 0 || b.Length < 12 + floatBytes + indexBytes)
        {
            Debug.LogError($"[PlaneModels] {path} 모델 데이터 크기가 올바르지 않습니다.");
            return null;
        }

        // 한 번에 복사 (꼭짓점 전체 → 법선 전체 → UV 전체 순서로 들어 있음)
        float[] f = new float[vertexCount * floatsPerVertex];
        System.Buffer.BlockCopy(b, 12, f, 0, (int)floatBytes);

        int[] indices = new int[indexCount];
        System.Buffer.BlockCopy(b, 12 + (int)floatBytes, indices, 0, (int)indexBytes);

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];

        int normalStart = vertexCount * 3;
        int uvStart = vertexCount * 6;

        for (int i = 0; i < vertexCount; i++)
        {
            vertices[i] = new Vector3(f[i * 3], f[i * 3 + 1], f[i * 3 + 2]);
            normals[i] = new Vector3(f[normalStart + i * 3], f[normalStart + i * 3 + 1], f[normalStart + i * 3 + 2]);
            uv[i] = new Vector2(f[uvStart + i * 2], f[uvStart + i * 2 + 1]);
        }

        mesh = new Mesh();
        mesh.name = System.IO.Path.GetFileName(path);
        if (vertexCount > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = indices;
        mesh.RecalculateBounds();

        texturedCache[path] = mesh;
        return mesh;
    }

    private static readonly Dictionary<string, Material> texturedMaterials = new Dictionary<string, Material>();

    /// <summary>
    /// 스킨 전용 머티리얼 (PlaneSkin_Mat 복사본 + 색 텍스처). 한 번만 만들고 재사용.
    ///
    /// 텍스처를 MaterialPropertyBlock으로만 넣으면 Unity가 "아무도 안 쓰는 에셋"으로 보고
    /// 씬을 불러올 때 메모리에서 내려 버려서 모델이 검게 나온다. (스킨씬 미리보기가 검게 보였던 원인)
    /// 머티리얼에 직접 넣고 여기서 계속 들고 있으면 내려가지 않는다.
    /// </summary>
    private static Material GetTexturedMaterial(PlaneSkins.Skin skin)
    {
        Material material;
        if (texturedMaterials.TryGetValue(skin.key, out material) && material != null &&
            material.GetTexture("_BaseMap") != null)
            return material;

        Material source = Resources.Load<Material>(MaterialPath);
        if (source == null)
        {
            Debug.LogError($"[PlaneModels] Resources/{MaterialPath} 머티리얼을 찾지 못했습니다.");
            return null;
        }

        if (material != null)
            Object.Destroy(material);   // 텍스처가 내려간 예전 것

        material = new Material(source);
        material.name = skin.key + "_Mat";
        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_Color", Color.white);
        material.SetFloat("_Smoothness", 0.2f);

        Texture2D texture = Resources.Load<Texture2D>(skin.TexturePath);
        if (texture != null)
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
        }
        else
        {
            Debug.LogWarning($"[PlaneModels] Resources/{skin.TexturePath} 텍스처를 찾지 못했습니다.");
        }

        texturedMaterials[skin.key] = material;
        return material;
    }

    /// <summary>
    /// 텍스처 모델 오브젝트 만들기. 가장 긴 변이 size(m)가 되도록 맞추고 가운데 정렬.
    /// </summary>
    public static GameObject CreateTextured(PlaneSkins.Skin skin, Transform parent, float size, out MeshFilter filter)
    {
        filter = null;

        if (skin == null)
            return null;

        Mesh mesh = LoadTextured(skin.MeshPath);
        if (mesh == null)
            return null;

        GameObject go = new GameObject("Model", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);

        filter = go.GetComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = GetTexturedMaterial(skin);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Bounds bounds = mesh.bounds;
        float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.0001f);
        float scale = size / longest;
        go.transform.localScale = Vector3.one * scale;
        go.transform.localPosition = -bounds.center * scale;
        go.transform.localRotation = Quaternion.identity;

        return go;
    }
}
