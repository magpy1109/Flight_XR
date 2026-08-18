using UnityEngine;
using System.IO;

public class ScreenshotMaker : MonoBehaviour
{
    [Header("사진 해상도")]
    public int width = 512;
    public int height = 512;

    [Header("저장될 파일 이름")]
    public string fileName = "Trail_Icon";

    // 인스펙터 창에서 우클릭으로 바로 실행하게 해주는 마법의 명령어!
    [ContextMenu("📸 찰칵! (사진 찍기)")]
    public void TakeScreenshot()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("이 스크립트는 반드시 카메라에 붙여야 합니다!");
            return;
        }

        // 투명 배경을 위한 렌더 텍스처 세팅
        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.ARGB32, false);

        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);

        cam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        // Assets 폴더 최상단에 PNG 파일로 저장
        byte[] bytes = screenShot.EncodeToPNG();
        string path = Application.dataPath + "/" + fileName + ".png";
        File.WriteAllBytes(path, bytes);

        Debug.Log("성공! 사진이 저장되었습니다: " + path);

        // 유니티 에디터 폴더 새로고침 (사진 바로 보이게 하기)
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}