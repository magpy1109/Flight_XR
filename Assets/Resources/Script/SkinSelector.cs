using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SkinSelector : MonoBehaviour
{
    [Header("메인 미리보기 화면 (2D 전용)")]
    // 3D 비행기 매니저에서는 비워두면 에러가 나지 않고 자연스럽게 무시됩니다.
    public Image mainPreviewImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI infoText;

    [Header("적용 버튼 (파란색)")]
    public Button applyButton;
    public TextMeshProUGUI applyButtonText;

    [Header("선택 테두리 UI")]
    public Transform selectionFrame;
    public GameObject defaultButton;

    // 👇 [새로 추가된 부분] 3D 비행기 모델 관리용
    [Header("3D 비행기 모델 관리 (트레일 매니저에선 비워두세요)")]
    public GameObject[] planeModels; // 0번:클래식, 1번:스카이블루, 2번:스노우

    private GameObject currentSelectedButton;

    private int currentAppliedSkinID = 0;
    private int previewingSkinID = 0;

    void Start()
    {
        if (defaultButton != null)
        {
            currentSelectedButton = defaultButton;
            selectionFrame.position = defaultButton.transform.position;
        }

        // 게임 시작 시 0번(클래식 화이트) 모델을 기본으로 켭니다.
        Update3DModel(0);
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
        // 👇 [핵심 수정] Image가 있을 때만 2D 이미지를 교체하도록 방어막 설정! (에러 방지)
        if (mainPreviewImage != null && selectedSprite != null)
        {
            mainPreviewImage.sprite = selectedSprite;
        }

        titleText.text = title;
        infoText.text = info;

        previewingSkinID = skinID;

        // 👇 [새로 추가] 3D 모델 교체 실행
        Update3DModel(skinID);

        if (isLocked)
        {
            applyButtonText.text = "조건을 달성하세요";
            applyButton.interactable = false;
        }
        else if (skinID == currentAppliedSkinID)
        {
            applyButtonText.text = "적용 중";
            applyButton.interactable = false;
        }
        else
        {
            applyButtonText.text = "이 스킨 적용";
            applyButton.interactable = true;
        }

        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        if (clickedButton != null)
        {
            currentSelectedButton = clickedButton;
            selectionFrame.gameObject.SetActive(true);
            selectionFrame.position = clickedButton.transform.position;
        }
    }

    public void OnApplyButtonClicked()
    {
        currentAppliedSkinID = previewingSkinID;

        applyButtonText.text = "적용 중";
        applyButton.interactable = false;
    }

    // 👇 [새로 추가된 함수] 3D 비행기들에게 투명 망토를 씌우고 벗기는 기능
    private void Update3DModel(int targetID)
    {
        // 3D 세팅이 안 된 곳(예: 트레일 스킨)에서는 작동하지 않고 부드럽게 넘어갑니다.
        if (planeModels == null || planeModels.Length == 0) return;

        // 모든 비행기를 순회하면서 선택된 번호만 켜고 나머지는 끕니다.
        for (int i = 0; i < planeModels.Length; i++)
        {
            if (planeModels[i] != null)
            {
                // i와 targetID가 같으면 true(켜짐), 다르면 false(꺼짐)가 적용됩니다.
                planeModels[i].SetActive(i == targetID);
            }
        }
    }
}