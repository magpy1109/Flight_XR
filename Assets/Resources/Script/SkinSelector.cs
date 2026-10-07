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

    // 선택 테두리가 따라다닐 버튼 (다른 페이지로 넘기면 버튼이 꺼지므로 테두리도 숨긴다)
    private Transform selectedButton;

    /// <summary>
    /// 트레일 탭인지 (TrailPanel에 붙은 SkinSelector, 또는 미리보기 모델이 파티클 트레일인 경우)
    /// 트레일은 비행기 스킨과 따로 저장한다. (예전에는 같은 칸에 저장돼서 트레일을 적용하면 비행기 스킨이 바뀌었음)
    /// </summary>
    private bool IsTrailSelector
    {
        get
        {
            if (gameObject.name.Contains("Trail"))
                return true;

            if (planeModels != null && planeModels.Length > 0 && planeModels[0] != null)
                return planeModels[0].GetComponentInChildren<ParticleSystem>(true) != null;

            return false;
        }
    }

    private void Start()
    {
        // Firebase 로드를 기다리지 않고 바로 표시한다.
        // (보유 스킨 / 장착 스킨은 지난번에 저장해 둔 값 사용 → 로드가 끝나면 최신 값으로 갱신)
        currentAppliedSkinID = GetEquippedSkinID();

        // 기존처럼 화면 진입 시 0번을 미리보기
        previewingSkinID = 0;

        if (defaultButton != null && selectionFrame != null)
        {
            selectedButton = defaultButton.transform;
            selectionFrame.position = defaultButton.transform.position;
            selectionFrame.gameObject.SetActive(true);
        }

        Update3DModel(previewingSkinID);
        UpdateButtonState(previewingSkinID);

        SaveManager.Loaded += OnSaveDataLoaded;
    }

    private void OnDestroy()
    {
        SaveManager.Loaded -= OnSaveDataLoaded;
    }

    /// <summary>Firebase 데이터가 늦게 도착하면 버튼 상태만 최신으로</summary>
    private void OnSaveDataLoaded()
    {
        currentAppliedSkinID = GetEquippedSkinID();
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
            selectedButton = buttonTransform;
            selectionFrame.gameObject.SetActive(true);
            selectionFrame.position = buttonTransform.position;
        }
    }

    /// <summary>
    /// 선택 테두리를 고른 버튼에 붙여 둔다.
    /// 고른 버튼이 없는 페이지(예 : 1페이지에서 고르고 2페이지로 넘김)에서는 테두리를 숨긴다.
    /// (예전에는 테두리가 제자리에 남아서 다른 페이지의 엉뚱한 칸 / 빈 칸이 선택된 것처럼 보였음)
    /// </summary>
    private void LateUpdate()
    {
        if (selectionFrame == null || selectedButton == null)
            return;

        bool show = selectedButton.gameObject.activeInHierarchy;

        if (selectionFrame.gameObject.activeSelf != show)
            selectionFrame.gameObject.SetActive(show);

        if (show)
            selectionFrame.position = selectedButton.position;
    }

    private void UpdateButtonState(int skinID)
    {
        if (applyButton == null || applyButtonText == null)
            return;

        string firestoreSkinID = ConvertSkinID(skinID);

        bool isOwned = IsOwned(firestoreSkinID);

        if (!isOwned)
        {
            applyButtonText.text = "조건을 달성하세요";
            applyButton.interactable = false;
        }
        else if (NormalizeID(skinID) == NormalizeID(currentAppliedSkinID))
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
        string firestoreSkinID =
            ConvertSkinID(previewingSkinID);

        // 삭제된 비행기 스킨(1~8번)은 적용하지 않는다
        if (!IsTrailSelector && !PlaneSkinState.IsAvailable(NormalizeID(previewingSkinID)))
        {
            Debug.LogWarning($"사용할 수 없는 스킨입니다 : {firestoreSkinID}");
            return;
        }

        // 보유 스킨인지 다시 확인 (로드 전이면 저장해 둔 보유 목록 기준)
        if (!IsOwned(firestoreSkinID))
        {
            Debug.LogWarning(
                $"보유하지 않은 스킨입니다 : {firestoreSkinID}"
            );

            return;
        }

        currentAppliedSkinID = previewingSkinID;

        // 바로 적용(로컬 저장) + Firebase users 문서에 저장 (로드 전이면 로드 후 저장)
        int normalized = NormalizeID(previewingSkinID);

        if (IsTrailSelector)
        {
            PlayerPrefs.SetInt(PlaneSkinState.TrailKey, normalized);
            if (SaveManager.Instance != null)
                SaveManager.Instance.EquipTrail(firestoreSkinID);
        }
        else
        {
            PlayerPrefs.SetInt(PlaneSkinState.PlaneKey, normalized);
            if (SaveManager.Instance != null)
                SaveManager.Instance.EquipSkin(firestoreSkinID);
        }
        PlayerPrefs.Save();

        Debug.Log(
            $"🎉 스킨 장착 완료! 저장된 스킨 ID: {firestoreSkinID}"
        );

        if (applyButtonText != null)
            applyButtonText.text = "적용 중";

        if (applyButton != null)
            applyButton.interactable = false;
    }

    /// <summary>장착한 번호. 비행기 탭에서는 삭제된 스킨(1~8번)을 기본 스킨으로 본다.</summary>
    private int GetEquippedSkinID()
    {
        int id = GetSavedEquippedID();
        return IsTrailSelector ? id : PlaneSkinState.NormalizeSkin(id);
    }

    private int GetSavedEquippedID()
    {
        if (SaveManager.Instance == null ||
            SaveManager.Instance.CurrentUser == null)
        {
            // Firebase 로드 전 : 지난번에 장착한 값
            return PlayerPrefs.GetInt(
                IsTrailSelector ? PlaneSkinState.TrailKey : PlaneSkinState.PlaneKey, 0);
        }

        string equippedID = IsTrailSelector
            ? SaveManager.Instance.CurrentUser.equipped_trail_id
            : SaveManager.Instance.CurrentUser.equipped_skin_id;

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

    /// <summary>
    /// 페이지마다 같은 스킨이 반복되므로(버튼 번호 0~44) 실제 스킨 번호(0~8)로 바꾼다.
    /// (3D 모델 표시와 같은 규칙. 예전에는 2페이지 이후 버튼이 항상 "조건을 달성하세요"였음)
    /// </summary>
    private int NormalizeID(int skinID)
    {
        int count = planeModels != null && planeModels.Length > 0 ? planeModels.Length : PlaneSkinState.SkinCount;
        return ((skinID % count) + count) % count;
    }

    private static bool IsOwned(string firestoreSkinID)
    {
        if (SaveManager.Instance != null)
            return SaveManager.Instance.HasSkin(firestoreSkinID);

        return SaveManager.HasSkinCached(firestoreSkinID);
    }

    private string ConvertSkinID(int skinID)
    {
        skinID = NormalizeID(skinID);

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