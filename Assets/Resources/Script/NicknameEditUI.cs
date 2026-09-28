using UnityEngine;
using TMPro;

public class NicknameEditUI : MonoBehaviour
{
    [Header("닉네임 변경 패널")]
    [SerializeField] private GameObject editPanel;

    [Header("닉네임 입력")]
    [SerializeField] private TMP_InputField nicknameInput;

    [Header("현재 닉네임 텍스트")]
    [SerializeField] private TextMeshProUGUI nicknameText;

    private void Start()
    {
        if (editPanel != null)
            editPanel.SetActive(false);
    }

    public void OpenEditPanel()
    {
        if (SaveManager.Instance == null ||
            !SaveManager.Instance.IsLoaded)
        {
            Debug.LogWarning(
                "SaveManager 데이터가 아직 로드되지 않았습니다."
            );
            return;
        }

        if (SaveManager.Instance.CurrentUser == null)
        {
            Debug.LogWarning("CurrentUser가 없습니다.");
            return;
        }

        nicknameInput.text =
            SaveManager.Instance.CurrentUser.nickname;

        editPanel.SetActive(true);
    }

    public void SaveNickname()
    {
        string newNickname =
            nicknameInput.text.Trim();

        if (string.IsNullOrEmpty(newNickname))
        {
            Debug.LogWarning("닉네임을 입력해주세요.");
            return;
        }

        SaveManager.Instance.UpdateNickname(newNickname);

        if (nicknameText != null)
            nicknameText.text = newNickname;

        editPanel.SetActive(false);

        Debug.Log("닉네임 변경 : " + newNickname);
    }

    public void CancelEdit()
    {
        editPanel.SetActive(false);
    }
}