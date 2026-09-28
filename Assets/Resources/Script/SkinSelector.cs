using System.Collections;
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

    private IEnumerator Start()
    {
        // SaveManager가 생성될 때까지 대기
        while (SaveManager.Instance == null)
        {
            yield return null;
        }

        // Firebase에서 유저 데이터가 모두 로드될 때까지 대기
        while (!SaveManager.Instance.IsLoaded)
        {
            yield return null;
        }

        Debug.Log("SkinSelector : 게임 데이터 로드 확인");

        // Firebase에 저장된 장착 스킨 가져오기
        currentAppliedSkinID = GetEquippedSkinID();

        // 기존처럼 화면 진입 시 0번을 미리보기
        previewingSkinID = 0;

        if (defaultButton != null && selectionFrame != null)
        {
            selectionFrame.position = defaultButton.transform.position;
            selectionFrame.gameObject.SetActive(true);
        }

        Update3DModel(previewingSkinID);

        // 실제 Firebase 보유 여부 기준으로 버튼 상태 결정
        UpdateButtonState(previewingSkinID);
    }

    public void ChangePreviewImage(
        Sprite selectedSprite,
        string title,
        string info,
        int skinID,
        bool isLocked,
        Transform buttonTransform)
    {
        if (mainPreviewImage != null && selectedSprite != null)
            mainPreviewImage.sprite = selectedSprite;

        if (titleText != null)
            titleText.text = title;

        if (infoText != null)
            infoText.text = info;

        previewingSkinID = skinID;

        Update3DModel(skinID);

        // Inspector의 isLocked는 사용하지 않고
        // Firebase 보유 여부로 판단
        UpdateButtonState(skinID);

        if (selectionFrame != null && buttonTransform != null)
        {
            selectionFrame.gameObject.SetActive(true);
            selectionFrame.position = buttonTransform.position;
        }
    }

    private void UpdateButtonState(int skinID)
    {
        if (applyButton == null || applyButtonText == null)
            return;

        if (SaveManager.Instance == null ||
            !SaveManager.Instance.IsLoaded)
        {
            applyButtonText.text = "불러오는 중...";
            applyButton.interactable = false;
            return;
        }

        string firestoreSkinID = ConvertSkinID(skinID);

        bool isOwned =
            SaveManager.Instance.HasSkin(firestoreSkinID);

        if (!isOwned)
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
        if (SaveManager.Instance == null ||
            !SaveManager.Instance.IsLoaded)
        {
            Debug.LogWarning("SaveManager 데이터가 아직 로드되지 않았습니다.");
            return;
        }

        string firestoreSkinID =
            ConvertSkinID(previewingSkinID);

        // 실제 보유 스킨인지 다시 확인
        if (!SaveManager.Instance.HasSkin(firestoreSkinID))
        {
            Debug.LogWarning(
                $"보유하지 않은 스킨입니다 : {firestoreSkinID}"
            );

            return;
        }

        currentAppliedSkinID = previewingSkinID;

        // Firebase users 문서에 장착 스킨 저장
        SaveManager.Instance.EquipSkin(firestoreSkinID);

        Debug.Log(
            $"🎉 스킨 장착 완료! 저장된 스킨 ID: {firestoreSkinID}"
        );

        if (applyButtonText != null)
            applyButtonText.text = "적용 중";

        if (applyButton != null)
            applyButton.interactable = false;
    }

    private int GetEquippedSkinID()
    {
        if (SaveManager.Instance == null ||
            SaveManager.Instance.CurrentUser == null)
        {
            return 0;
        }

        string equippedID =
            SaveManager.Instance.CurrentUser.equipped_skin_id;

        if (string.IsNullOrEmpty(equippedID))
            return 0;

        // 기본 스킨
        if (equippedID == "default")
            return 0;

        // 숫자 스킨
        if (int.TryParse(equippedID, out int result))
            return result;

        Debug.LogWarning(
            $"알 수 없는 장착 스킨 ID : {equippedID}"
        );

        return 0;
    }

    private string ConvertSkinID(int skinID)
    {
        // UI의 0번 = Firebase의 default
        if (skinID == 0)
            return "default";

        return skinID.ToString();
    }

    private void Update3DModel(int targetID)
    {
        if (planeModels == null || planeModels.Length == 0)
            return;

        // 모든 모델 끄기
        for (int i = 0; i < planeModels.Length; i++)
        {
            if (planeModels[i] != null)
                planeModels[i].SetActive(false);
        }

        // 현재 구조 유지
        int modelIndex = targetID % planeModels.Length;

        if (planeModels[modelIndex] != null)
            planeModels[modelIndex].SetActive(true);
    }

#if UNITY_EDITOR
    [ContextMenu("✨ 1초 컷! 버튼 자동 번호 매기기")]
    public void AutoAssignSkinIDs()
    {
        AutoSkinButton[] buttons =
            GetComponentsInChildren<AutoSkinButton>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].skinID = i;
            UnityEditor.EditorUtility.SetDirty(buttons[i]);
        }

        Debug.Log(
            $"🎉 [성공] 총 {buttons.Length}개의 버튼에 0번부터 번호를 자동으로 매겼습니다!"
        );
    }
#endif
}