using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkinSelector : MonoBehaviour
{
    [Header("메인 미리보기 화면")]
    public Image mainPreviewImage;

    [Header("선택 테두리 UI")]
    public Transform selectionFrame;

    [Header("기본 선택 버튼 (1페이지 1번 버튼)")]
    public GameObject defaultButton; // 시작할 때 처음 선택되어 있을 버튼

    // 현재 선택된 '진짜' 버튼이 무엇인지 기억해둘 빈 공간
    private GameObject currentSelectedButton;

    void Start()
    {
        // 게임이 시작될 때, 기본 버튼(1번)을 '현재 선택된 버튼'으로 기억시킵니다.
        if (defaultButton != null)
        {
            currentSelectedButton = defaultButton;
            selectionFrame.position = defaultButton.transform.position;
        }
    }

    void Update()
    {
        // 매 순간마다 확인합니다: "내가 기억하고 있는 버튼이 지금 화면에 켜져 있는가?"
        if (currentSelectedButton != null)
        {
            // activeInHierarchy는 오브젝트가 현재 화면에 켜져있는지(true/false) 알려줍니다.
            // 버튼이 켜져있으면 테두리도 켜고, 페이지가 넘어가서 버튼이 꺼지면 테두리도 숨깁니다!
            selectionFrame.gameObject.SetActive(currentSelectedButton.activeInHierarchy);
        }
    }

    public void ChangePreviewImage(Sprite selectedSprite)
    {
        mainPreviewImage.sprite = selectedSprite;
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;

        if (clickedButton != null)
        {
            currentSelectedButton = clickedButton; // 새로 클릭한 버튼을 기억합니다.

            // 테두리를 켜고 클릭한 버튼 위치로 이동시킵니다.
            selectionFrame.gameObject.SetActive(true);
            selectionFrame.position = clickedButton.transform.position;
        }
    }
}