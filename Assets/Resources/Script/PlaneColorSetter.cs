using UnityEngine;

[ExecuteAlways] // 플레이 버튼을 누르지 않아도 에디터 화면에서 색상이 즉시 변하게 해주는 핵심 명령어입니다!
public class PlaneColorSetter : MonoBehaviour
{
    [Header("이 비행기의 스킨 색상")]
    public Color skinColor = Color.white;

    void OnEnable()
    {
        ApplyColor();
    }

    void OnValidate() // 인스펙터에서 색상 칸을 건드릴 때마다 실시간으로 작동합니다.
    {
        ApplyColor();
    }

    private void ApplyColor()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // 매터리얼 파일을 새로 만들지 않고, 겉면의 색상 데이터만 살짝 덮어씌우는 최적화 기법입니다.
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            rend.GetPropertyBlock(block);

            // URP 렌더러의 기본 색상 변수 이름인 "_BaseColor"를 사용해 색을 입힙니다.
            block.SetColor("_BaseColor", skinColor);

            rend.SetPropertyBlock(block);
        }
    }
}