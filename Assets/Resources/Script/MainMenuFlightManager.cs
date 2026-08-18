using UnityEngine;

public class MainMenuFlightManager : MonoBehaviour
{
    [Header("메인 화면에 띄울 비행기 9대")]
    public GameObject[] planeModels;

    void Start()
    {
        // 👇 [충돌 방지!] 스킨 화면(SkinSelector가 있는 곳)에서는 작동을 멈춰서 3D 모델을 꼬이지 않게 합니다.
        if (FindObjectOfType<SkinSelector>() != null) return;

        int equippedSkinID = PlayerPrefs.GetInt("EquippedSkin", 0);
        if (planeModels == null || planeModels.Length == 0) return;

        // 1. 전부 끕니다.
        for (int i = 0; i < planeModels.Length; i++)
        {
            planeModels[i].SetActive(false);
        }

        // 2. 9대 모델로 45번 버튼까지 커버하는 마법(%)
        int modelIndex = equippedSkinID % planeModels.Length;

        // 3. 장착된 번호의 비행기를 켭니다.
        if (planeModels[modelIndex] != null) planeModels[modelIndex].SetActive(true);
    }
}