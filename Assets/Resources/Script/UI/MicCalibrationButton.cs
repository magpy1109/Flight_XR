using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 설정씬 "입력" 줄의 [입김 보정] 버튼.
/// 1) 조용히 있는 동안 주변 소음 크기 측정
/// 2) 마이크 쪽으로 부는 동안 입김 크기 측정
/// 측정값을 MicSensitivity에 저장해서 게임의 입김 판정 기준으로 쓴다.
/// </summary>
public class MicCalibrationButton : MonoBehaviour
{
    private const float QuietSeconds = 1.5f;
    private const float BlowSeconds = 3f;
    private const float CountdownSeconds = 1f;

    private static bool isCalibrating;

    private Button button;
    private TMP_Text buttonLabel;
    private TMP_Text descriptionText;
    private string originalDescription;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        isCalibrating = false;
    }

    /// <summary>입력 슬라이더가 있는 줄에 보정 버튼을 만든다.</summary>
    public static MicCalibrationButton Create(Slider slider)
    {
        Transform row = slider.transform.parent;

        // 줄의 기존 글꼴 사용 (한글 표시용)
        TMP_Text titleText = null;
        Transform title = row.Find("TitleText");
        if (title != null) titleText = title.GetComponent<TMP_Text>();

        Transform desc = row.Find("DescriptionText");

        // 버튼 배경
        GameObject go = new GameObject("MicCalibrationButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(row, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(220f, 34f);

        Image image = go.GetComponent<Image>();
        ColorUtility.TryParseHtmlString("#0C8CE9", out Color blue);
        image.color = blue;

        // 버튼 글자
        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);

        RectTransform labelRect = (RectTransform)labelGo.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
        if (titleText != null) label.font = titleText.font;
        label.fontSize = 24f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        MicCalibrationButton calibration = go.AddComponent<MicCalibrationButton>();
        calibration.button = go.GetComponent<Button>();
        calibration.buttonLabel = label;
        calibration.descriptionText = desc != null ? desc.GetComponent<TMP_Text>() : null;
        calibration.originalDescription = calibration.descriptionText != null ? calibration.descriptionText.text : "";

        calibration.button.onClick.AddListener(calibration.StartCalibration);
        calibration.RefreshLabel();

        return calibration;
    }

    private void OnEnable()
    {
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (buttonLabel == null || isCalibrating)
            return;

        buttonLabel.text = MicSensitivity.HasCalibration ? "입김 다시 보정" : "입김 보정";
    }

    private void SetDescription(string text)
    {
        if (descriptionText != null)
            descriptionText.text = text;
    }

    public void StartCalibration()
    {
        if (isCalibrating)
            return;

        StartCoroutine(CalibrationRoutine());
    }

    private IEnumerator CalibrationRoutine()
    {
        isCalibrating = true;
        button.interactable = false;

        if (!MicLevelMeter.IsMicRunning)
        {
            SetDescription("마이크 사용 불가 (권한 확인)");
            yield return new WaitForSecondsRealtime(2.5f);
            Finish();
            yield break;
        }

        // 1) 주변 소음
        buttonLabel.text = "조용히...";
        SetDescription("소음 측정 중, 조용히 해 주세요");

        List<float> quiet = new List<float>();
        yield return Measure(QuietSeconds, quiet);

        // 2) 준비
        buttonLabel.text = "준비...";
        SetDescription("왼쪽 아래 마이크 쪽으로 준비");
        yield return new WaitForSecondsRealtime(CountdownSeconds);

        // 3) 입김
        buttonLabel.text = "지금 부세요!";
        SetDescription("지금 왼쪽 아래로 세게 불어 주세요!");

        List<float> blow = new List<float>();
        yield return Measure(BlowSeconds, blow);

        float noise = Percentile(quiet, 0.9f);
        float peak = Percentile(blow, 0.9f);

        if (peak < Mathf.Max(noise * 3f, 0.001f))
        {
            SetDescription("인식 실패, 마이크 쪽으로 다시 시도");
            yield return new WaitForSecondsRealtime(3f);
        }
        else
        {
            MicSensitivity.SaveCalibration(noise, peak);
            Debug.Log($"[MicCalibration] 소음 {noise:F5} / 입김 {peak:F5} 저장");

            SetDescription("보정 완료! 막대로 확인해 보세요");
            yield return new WaitForSecondsRealtime(2.5f);
        }

        Finish();
    }

    private static IEnumerator Measure(float seconds, List<float> result)
    {
        float t = 0f;

        while (t < seconds)
        {
            result.Add(MicLevelMeter.CurrentRms);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static float Percentile(List<float> values, float p)
    {
        if (values.Count == 0)
            return 0f;

        values.Sort();
        int index = Mathf.Clamp(Mathf.RoundToInt((values.Count - 1) * p), 0, values.Count - 1);
        return values[index];
    }

    private void Finish()
    {
        isCalibrating = false;

        if (button != null)
            button.interactable = true;

        SetDescription(originalDescription);
        RefreshLabel();
    }

    private void OnDisable()
    {
        // 탭 전환 등으로 중간에 꺼지면 보정 취소
        if (isCalibrating)
        {
            StopAllCoroutines();
            Finish();
        }
    }
}
