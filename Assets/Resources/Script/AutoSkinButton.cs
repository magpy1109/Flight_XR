using UnityEngine;
using UnityEngine.UI;

public class AutoSkinButton : MonoBehaviour
{
    [Header("중앙에 띄울 고화질 큰 비행기 이미지")]
    public Sprite bigSkinSprite;

    [Header("스킨 제목 및 설명")]
    public string skinTitle;
    [TextArea] public string skinInfo;

    [Header("스킨 고유 번호 (0부터 44까지 순서대로)")]
    public int skinID;

    [Header("잠긴 스킨인가요?")]
    public bool isLocked;

    void Start()
    {
        SkinSelector mySelector = GetComponentInParent<SkinSelector>();
        if (mySelector != null)
        {
            // 👇 [변경됨] 버튼이 눌리면 자기 정보와 '내 위치(transform)'까지 한 방에 매니저로 쏩니다!
            GetComponent<Button>().onClick.AddListener(() =>
                mySelector.ChangePreviewImage(bigSkinSprite, skinTitle, skinInfo, skinID, isLocked, transform));
        }
    }
}