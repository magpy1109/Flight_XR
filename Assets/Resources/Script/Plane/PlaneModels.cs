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
}
