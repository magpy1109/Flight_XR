using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultUI : MonoBehaviour
{
    public static ResultUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject hud;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalDistanceText;
    [SerializeField] private TMP_Text finalHeightText;
    [SerializeField] private TMP_Text finalRingText;

    [Header("Game")]
    [SerializeField] private PlaneLauncher launcher;
    [SerializeField] private GameObject gameCanvas;

    [Header("Scene")]
    [SerializeField] private string homeSceneName = "MainMenuScene";

    public bool IsResultShown =>
        GameOverResultScreen.IsShown ||
        (resultPanel != null && resultPanel.activeSelf);

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

        if (hud != null)
            hud.SetActive(true);

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    // GameCanvas는 씬이 바뀌어도 유지되는데, 홈으로 갈 때(GoHome) HUD를 꺼 두기 때문에
    // 다시 게임씬에 들어와 GameCanvas가 켜질 때마다 HUD를 켜고 결과창을 닫는다.
    // (예전에는 Awake에서 한 번만 켜서 두 번째 플레이부터 HUD가 보이지 않았음)
    private void OnEnable()
    {
        if (Instance != this)
            return;

        if (hud != null)
            hud.SetActive(true);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        GameOverResultScreen.Hide();
    }

    public void ShowResult(
        int score,
        float distance,
        float height,
        int ringCount)
    {
        Debug.Log("ResultUI.ShowResult 호출");

        if (hud != null)
            hud.SetActive(false);

        // 새 결과 화면 (Resources/Prefab/ResultPanel.prefab)
        // 프리팹을 찾지 못한 경우에만 GameCanvas 안의 예전 결과창 사용
        if (GameOverResultScreen.Show(distance))
            return;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (finalScoreText != null)
            finalScoreText.text = score.ToString();

        if (finalDistanceText != null)
            finalDistanceText.text = "distance: " + distance.ToString("F1") + " m";

        if (finalHeightText != null)
            finalHeightText.text = "height: " + height.ToString("F1") + " m";

        if (finalRingText != null)
            finalRingText.text = ringCount.ToString();
    }

    public void RestartGame()
    {
        Debug.Log("RestartGame 실행");

        GameOverResultScreen.Hide();

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (hud != null)
            hud.SetActive(true);

        if (GameManager.Instance != null)
            GameManager.Instance.ResetGame();

        // 현재 SampleScene의 PlaneLauncher 다시 찾기
        launcher = FindFirstObjectByType<PlaneLauncher>(
            FindObjectsInactive.Include
        );

        if (launcher == null)
        {
            Debug.LogError(
                "RestartGame: 현재 씬에서 PlaneLauncher를 찾을 수 없습니다."
            );
            return;
        }

        if (CountdownManager.Instance == null)
        {
            Debug.LogError(
                "RestartGame: CountdownManager.Instance가 없습니다."
            );
            return;
        }

        Debug.Log("Restart용 PlaneLauncher 찾음 : " + launcher.name);

        CountdownManager.Instance.StartCountdown(() =>
        {
            Debug.Log("Restart 카운트다운 완료 → 비행기 발사");

            if (launcher != null)
                launcher.Launch();
            else
                Debug.LogError("Restart 발사 시점에 PlaneLauncher가 null입니다.");
        });
    }

    public void GoHome()
    {
        Debug.Log("홈 이동 : " + homeSceneName);

        GameOverResultScreen.Hide();

        // Persistent GameCanvas 끄기
        if (gameCanvas != null)
            gameCanvas.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (hud != null)
            hud.SetActive(false);

        SceneManager.LoadScene(homeSceneName);
    }
}