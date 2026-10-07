using UnityEngine;

/// <summary>
/// 스킨씬 트레일 탭의 "스킨별 기본 트레일" 미리보기.
/// 켜질 때마다 지금 장착한 비행기에 어울리는 트레일의 미리보기를 대신 켜 준다.
/// (비행기 탭에서 스킨을 바꾸고 돌아오면 그 스킨의 트레일이 보임)
/// </summary>
public class AutoTrailPreview : MonoBehaviour
{
    /// <summary>지금 보여 줄 미리보기 오브젝트를 돌려주는 함수 (SkinSceneExtraSkins에서 넣어 줌)</summary>
    public System.Func<GameObject> resolve;

    private GameObject shown;

    private void OnEnable()
    {
        shown = resolve != null ? resolve() : null;

        if (shown != null && shown != gameObject)
            shown.SetActive(true);
    }

    private void OnDisable()
    {
        if (shown != null && shown != gameObject)
            shown.SetActive(false);

        shown = null;
    }
}
