using UnityEngine;
using System.IO;

public class AutoIconGenerator : MonoBehaviour
{
    [Header("촬영할 카메라 (투명 배경 세팅 필수)")]
    public Camera photoCamera;

    [Header("촬영할 비행기 스킨들")]
    public GameObject[] planeModels;

    [Header("저장할 폴더 경로 및 해상도")]
    public string savePath = "Assets/Resources/Icons/"; // 이 경로에 폴더가 없으면 자동 생성됩니다.
    public int imageSize = 512; // 512x512 고화질 아이콘

    // 인스펙터 창에서 스크립트를 우클릭하면 실행되는 마법의 버튼입니다!
    [ContextMenu("📸 모든 스킨 아이콘 자동 촬영하기")]
    public void TakeSnapshots()
    {
        if (photoCamera == null || planeModels.Length == 0) return;

        // 저장할 폴더가 없으면 알아서 만듭니다.
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // 촬영을 위한 임시 렌더 텍스처와 사진(Texture2D) 세팅
        RenderTexture rt = new RenderTexture(imageSize, imageSize, 24);
        photoCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(imageSize, imageSize, TextureFormat.ARGB32, false);

        for (int i = 0; i < planeModels.Length; i++)
        {
            // 모든 비행기 숨기기
            foreach (var model in planeModels) model.SetActive(false);

            // 현재 순서의 비행기만 짠 하고 나타나기
            planeModels[i].SetActive(true);

            // 찰칵! 렌더링
            photoCamera.Render();

            // 찍힌 화면을 사진 파일 데이터로 변환
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, imageSize, imageSize), 0, 0);
            screenShot.Apply();

            // 투명 배경 PNG로 인코딩 후 저장
            byte[] bytes = screenShot.EncodeToPNG();
            string filename = savePath + planeModels[i].name + "_Icon.png";
            File.WriteAllBytes(filename, bytes);

            Debug.Log(planeModels[i].name + " 촬영 완료! ➔ " + filename);
        }

        // 카메라 원상복구 및 메모리 청소
        photoCamera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        DestroyImmediate(screenShot);

        Debug.Log("🎉 모든 촬영이 끝났습니다! Project 창을 클릭하면 사진들이 뿅 나타납니다.");
    }
}