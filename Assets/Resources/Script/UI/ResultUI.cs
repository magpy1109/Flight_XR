using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultUI : MonoBehaviour
{
    public static ResultUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject hud;
    [SerializeField] private GameObject resultPanel;

    [Header("Result Texts")]
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalDistanceText;
    [SerializeField] private TMP_Text finalHeightText;
    [SerializeField] private TMP_Text finalRingText;

    [Header("MooBehaviour Script")]
    [SerializeField] private PlaneLauncher launcher;

    [Header("Scene")]
    [SerializeField] private string homeSceneName = "MainMenuScene";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 게임 시작 시 상태
        if (hud != null)
            hud.SetActive(true);

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    public void ShowResult(
        int score,
        float distance,
        float height,
        int ringCount)
    {
        Debug.Log("ResultUI.ShowResult 실행");

        // 게임 HUD 숨기기
        if (hud != null)
            hud.SetActive(false);

        // 결과창 표시
        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultTitle != null)
            resultTitle.text = "GAME OVER";

        if (finalScoreText != null)
            finalScoreText.text = $"SCORE : {score}";

        if (finalDistanceText != null)
            finalDistanceText.text =
                $"DISTANCE : {distance:F1} m";

        if (finalHeightText != null)
            finalHeightText.text =
                $"HEIGHT : {height:F1} m";

        if (finalRingText != null)
            finalRingText.text =
                $"RINGS : {ringCount}";
    }

    // 다시 시작
    public void RestartGame()
    {
        Debug.Log("RestartGame 실행");

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (hud != null)
            hud.SetActive(true);

        if (GameManager.Instance != null)
            GameManager.Instance.ResetGame();

        CountdownManager.Instance.StartCountdown(() =>
        {
            launcher.Launch();
        });
    }

    // 홈으로 이동
    public void GoHome()
    {
        Debug.Log($"홈 이동 : {homeSceneName}");

        SceneManager.LoadScene(homeSceneName);
    }
}