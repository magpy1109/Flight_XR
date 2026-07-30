using UnityEngine;

public class TabController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject flightPanel; // 비행기 화면을 담은 패널
    public GameObject trailPanel;  // 트레일 화면을 담은 패널

    void Start()
    {
        // 씬이 시작될 때 기본으로 비행기 탭이 열려있도록 설정
        ShowFlightPanel();
    }

    // '비행기' 버튼을 눌렀을 때 실행될 함수
    public void ShowFlightPanel()
    {
        flightPanel.SetActive(true);
        trailPanel.SetActive(false);
    }

    // '트레일' 버튼을 눌렀을 때 실행될 함수
    public void ShowTrailPanel()
    {
        flightPanel.SetActive(false);
        trailPanel.SetActive(true);
    }
}