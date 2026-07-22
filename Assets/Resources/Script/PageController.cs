using UnityEngine;
using UnityEngine.UI; // 점(Image)을 다루기 위해 반드시 필요합니다!

public class PageController : MonoBehaviour
{
    [Header("페이지 그룹 목록")]
    public GameObject[] pages;
    private int currentPageIndex = 0;

    [Header("페이지 표시(점) 설정")]
    public Image[] dots;       // 이 녀석이 인스펙터에 Dots라는 빈칸을 만들어줍니다!
    public Sprite dotOnSprite;
    public Sprite dotOffSprite;

    void Start()
    {
        UpdatePageDisplay();
    }

    public void NextPage()
    {
        if (currentPageIndex < pages.Length - 1)
        {
            currentPageIndex++;
            UpdatePageDisplay();
        }
    }

    public void PrevPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePageDisplay();
        }
    }

    private void UpdatePageDisplay()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == currentPageIndex);

            // 점 상태 업데이트
            if (i < dots.Length)
            {
                if (i == currentPageIndex)
                {
                    dots[i].sprite = dotOnSprite;
                }
                else
                {
                    dots[i].sprite = dotOffSprite;
                }
            }
        }
    }
}