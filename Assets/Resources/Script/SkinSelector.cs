using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SkinSelector : MonoBehaviour
{
    [Header("메인 미리보기 화면")]
    public Image mainPreviewImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI infoText;

    // 👇 [새로 추가된 부분] 파란색 적용 버튼 제어용
    [Header("적용 버튼 (파란색)")]
    public Button applyButton; // 버튼 자체 (클릭 켜고 끄기 위해)
    public TextMeshProUGUI applyButtonText; // 버튼 안의 글씨

    [Header("선택 테두리 UI")]
    public Transform selectionFrame;
    public GameObject defaultButton;

    private GameObject currentSelectedButton;

    // 상태 기억용 변수들
    private int currentAppliedSkinID = 0; // 현재 '진짜 적용중'인 스킨 번호 (시작은 0번 클래식 화이트)
    private int previewingSkinID = 0;     // 방금 클릭해서 '미리보기' 중인 스킨 번호

    void Start()
    {
        if (defaultButton != null)
        {
            currentSelectedButton = defaultButton;
            selectionFrame.position = defaultButton.transform.position;
        }
    }

    void Update()
    {
        if (currentSelectedButton != null)
        {
            selectionFrame.gameObject.SetActive(currentSelectedButton.activeInHierarchy);
        }
    }

    public void ChangePreviewImage(Sprite selectedSprite, string title, string info, int skinID, bool isLocked)
    {
        mainPreviewImage.sprite = selectedSprite;
        titleText.text = title;
        infoText.text = info;

        previewingSkinID = skinID; // 지금 미리보기로 띄운 스킨 번호 저장

        // 👇 파란색 버튼 상태 업데이트 로직
        if (isLocked)
        {
            applyButtonText.text = "조건을 달성하세요";
            applyButton.interactable = false; // 회색으로 변하며 클릭 안 됨
        }
        else if (skinID == currentAppliedSkinID)
        {
            applyButtonText.text = "적용중";
            applyButton.interactable = false; // 이미 적용중이므로 클릭 안 됨
        }
        else
        {
            applyButtonText.text = "이 스킨 적용";
            applyButton.interactable = true; // 클릭 가능하게 활성화!
        }

        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        if (clickedButton != null)
        {
            currentSelectedButton = clickedButton;
            selectionFrame.gameObject.SetActive(true);
            selectionFrame.position = clickedButton.transform.position;
        }
    }

    // 👇 [새로 추가된 부분] 파란색 적용 버튼을 '클릭'했을 때 실행될 함수
    public void OnApplyButtonClicked()
    {
        currentAppliedSkinID = previewingSkinID; // 미리보기 중이던 스킨을 '적용중'으로 확정 땅땅!

        applyButtonText.text = "적용중";
        applyButton.interactable = false; // 방금 적용했으니 다시 못 누르게 잠금
    }
}