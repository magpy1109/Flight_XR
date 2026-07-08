using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoundRowMuter : MonoBehaviour
{
    [Header("[ UI Elements ]")]
    public Button iconButton;              // 클릭할 스피커 아이콘 버튼
    public TextMeshProUGUI titleText;      // 투명도 40%로 만들 제목 텍스트
    public CanvasGroup sliderCanvasGroup;  // 투명도 40% 및 클릭 막기용 슬라이더 CanvasGroup
    public TextMeshProUGUI descText;       // 투명도 20%로 만들 설명 텍스트

    [Header("[ Sprites ]")]
    public Sprite activeSprite;            // 소리 켜짐 아이콘 (파란색 또는 기본)
    public Sprite muteSprite;              // 음소거 아이콘 (회색 또는 꺼짐)

    public bool isMuted = false;

    // 다른 탭과 동기화하기 위해 매니저에게 보낼 신호 대리자
    public System.Action<bool> onMuteChanged;

    void Start()
    {
        if (iconButton != null)
        {
            // 아이콘 버튼에 클릭 이벤트 등록
            iconButton.onClick.AddListener(ToggleMute);
        }
        UpdateUI();
    }

    public void ToggleMute()
    {
        SetMute(!isMuted);
        onMuteChanged?.Invoke(isMuted); // 매니저에게 나 상태 바뀌었다고 신호 보냄
    }

    public void SetMute(bool mute)
    {
        isMuted = mute;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (isMuted)
        {
            if (iconButton != null && muteSprite != null) iconButton.image.sprite = muteSprite;
            if (titleText != null) titleText.alpha = 0.4f;               // 제목 투명도 40%
            if (descText != null) descText.alpha = 0.2f;                // 설명 투명도 20%
            if (sliderCanvasGroup != null)
            {
                sliderCanvasGroup.alpha = 0.4f;                          // 슬라이더 부품 전체 투명도 40%
                sliderCanvasGroup.interactable = false;                  // 슬라이더 조작 금지
                sliderCanvasGroup.blocksRaycasts = false;                // 마우스 클릭 통과시키기
            }
        }
        else
        {
            if (iconButton != null && activeSprite != null) iconButton.image.sprite = activeSprite;
            if (titleText != null) titleText.alpha = 1.0f;               // 원래대로
            if (descText != null) descText.alpha = 1.0f;                // 원래대로
            if (sliderCanvasGroup != null)
            {
                sliderCanvasGroup.alpha = 1.0f;
                sliderCanvasGroup.interactable = true;                   // 슬라이더 조작 허용
                sliderCanvasGroup.blocksRaycasts = true;
            }
        }
    }
}