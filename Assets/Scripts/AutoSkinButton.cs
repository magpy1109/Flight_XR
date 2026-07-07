using UnityEngine;
using UnityEngine.UI;

public class AutoSkinButton : MonoBehaviour
{
    [Header("중앙에 띄울 고화질 큰 비행기 이미지")]
    public Sprite bigSkinSprite; // 인스펙터에서 직접 짝꿍을 지어줄 빈칸입니다.

    void Start()
    {
        SkinSelector mySelector = GetComponentInParent<SkinSelector>();

        // 3. 버튼 클릭 이벤트 자동 연결
        if (mySelector != null)
        {
            // 이제 내 껍데기가 아니라, 지정해둔 '큰 이미지(bigSkinSprite)'를 던져줍니다!
            GetComponent<Button>().onClick.AddListener(() => mySelector.ChangePreviewImage(bigSkinSprite));
        }
    }
}