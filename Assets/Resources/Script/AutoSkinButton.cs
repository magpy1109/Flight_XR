using UnityEngine;
using UnityEngine.UI;

public class AutoSkinButton : MonoBehaviour
{
    [Header("중앙에 띄울 고화질 큰 비행기 이미지")]
    public Sprite bigSkinSprite;

    [Header("스킨 제목 및 설명")]
    public string skinTitle;
    [TextArea] public string skinInfo;

    // 👇 [새로 추가된 부분] 스킨의 상태 정보
    [Header("스킨 고유 번호 (0부터 순서대로 적어주세요)")]
    public int skinID;

    [Header("잠긴 스킨인가요? (체크하면 '조건 달성'이 뜹니다)")]
    public bool isLocked;

    void Start()
    {
        SkinSelector mySelector = GetComponentInParent<SkinSelector>();

        if (mySelector != null)
        {
            // 이제 번호(skinID)와 잠김 여부(isLocked)도 같이 던져줍니다!
            GetComponent<Button>().onClick.AddListener(() =>
                mySelector.ChangePreviewImage(bigSkinSprite, skinTitle, skinInfo, skinID, isLocked));
        }
    }
}