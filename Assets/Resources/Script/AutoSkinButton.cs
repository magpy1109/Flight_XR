using UnityEngine;
using UnityEngine.UI;

public class AutoSkinButton : MonoBehaviour
{
    [Header("중앙에 띄울 고화질 큰 비행기 이미지")]
    public Sprite bigSkinSprite;

    [Header("스킨 제목 및 설명")]
    public string skinTitle;

    [TextArea]
    public string skinInfo;

    [Header("스킨 고유 번호")]
    public int skinID;

    private void Start()
    {
        SkinSelector mySelector =
            GetComponentInParent<SkinSelector>();

        if (mySelector != null)
        {
            GetComponent<Button>().onClick.AddListener(() =>
                mySelector.ChangePreviewImage(
                    bigSkinSprite,
                    skinTitle,
                    skinInfo,
                    skinID,
                    false,
                    transform
                )
            );
        }
    }
}