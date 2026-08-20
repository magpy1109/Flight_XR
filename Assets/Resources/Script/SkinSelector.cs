using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinSelector : MonoBehaviour
{
    [Header("메인 미리보기 화면 (2D 전용)")]
    public Image mainPreviewImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI infoText;

    [Header("적용 버튼 (파란색)")]
    public Button applyButton;
    public TextMeshProUGUI applyButtonText;

    [Header("선택 테두리 UI")]
    public Transform selectionFrame;
    public GameObject defaultButton;

    [Header("3D 비행기/트레일 모델 관리")]
    public GameObject[] planeModels;

    private int currentAppliedSkinID = 0;
    private int previewingSkinID = 0;

    void Start()
    {
        // 1. 장착해둔 스킨(예: 스카이블루) 번호는 기억만 해둡니다.
        currentAppliedSkinID = PlayerPrefs.GetInt("EquippedSkin", 0);

        // 👇 [문제 해결의 핵심!] 스킨 화면에 들어오면 장착 스킨과 무관하게 무조건 0번(클래식 화이트)을 보여줍니다.
        previewingSkinID = 0;

        if (defaultButton != null && selectionFrame != null)
        {
            selectionFrame.position = defaultButton.transform.position;
            selectionFrame.gameObject.SetActive(true);
        }

        // 장착된 스킨(currentAppliedSkinID)이 아니라, 미리보기 스킨(0번)으로 3D 모델을 켭니다!
        Update3DModel(previewingSkinID);
        UpdateButtonState(previewingSkinID, false);
    }

    public void ChangePreviewImage(Sprite selectedSprite, string title, string info, int skinID, bool isLocked, Transform buttonTransform)
    {
        if (mainPreviewImage != null && selectedSprite != null) mainPreviewImage.sprite = selectedSprite;
        if (titleText != null) titleText.text = title;
        if (infoText != null) infoText.text = info;

        previewingSkinID = skinID;
        Update3DModel(skinID);
        UpdateButtonState(skinID, isLocked);

        if (selectionFrame != null && buttonTransform != null)
        {
            selectionFrame.gameObject.SetActive(true);
            selectionFrame.position = buttonTransform.position;
        }
    }

    private void UpdateButtonState(int skinID, bool isLocked)
    {
        if (applyButton == null || applyButtonText == null) return;

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
    }

    public void OnApplyButtonClicked()
    {
        currentAppliedSkinID = previewingSkinID;

        PlayerPrefs.SetInt("EquippedSkin", currentAppliedSkinID);
        PlayerPrefs.Save();

        Debug.Log($"🎉 스킨 장착 완료! 저장된 스킨 ID: {currentAppliedSkinID}");

        if (applyButtonText != null) applyButtonText.text = "적용 중";
        if (applyButton != null) applyButton.interactable = false;
    }

    private void Update3DModel(int targetID)
    {
        if (planeModels == null || planeModels.Length == 0) return;

        // 1. 모든 비행기 끄기
        for (int i = 0; i < planeModels.Length; i++)
        {
            if (planeModels[i] != null) planeModels[i].SetActive(false);
        }

        // 2. 9대 모델로 45번 버튼까지 커버하는 마법(%)
        int modelIndex = targetID % planeModels.Length;

        // 3. 알맞은 모델 1대 켜기
        if (planeModels[modelIndex] != null) planeModels[modelIndex].SetActive(true);
    }

#if UNITY_EDITOR    
    [ContextMenu("✨ 1초 컷! 버튼 자동 번호 매기기 (노가다 해방)")]
    public void AutoAssignSkinIDs()
    {
        AutoSkinButton[] buttons = GetComponentsInChildren<AutoSkinButton>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].skinID = i;
            UnityEditor.EditorUtility.SetDirty(buttons[i]); 
        }
        Debug.Log($"🎉 [성공] 총 {buttons.Length}개의 버튼에 0번부터 번호를 자동으로 매겼습니다!");
    }
#endif
}