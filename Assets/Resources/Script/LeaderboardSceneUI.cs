using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 리더보드 씬 화면.
///
/// - 오른쪽 위 [거리 | 점수] 버튼으로 거리 랭킹 / 점수 랭킹 전환 (마지막으로 본 랭킹을 기억)
/// - 상위 3명 카드, 4위부터 목록, 아래 내 기록 줄이 선택한 랭킹 기준으로 바뀐다
/// - 목록 머리글 "최고기록" → "최고거리" / "최고점수"
/// </summary>
public class LeaderboardSceneUI : MonoBehaviour
{
    [Header("상위 3명")]
    [SerializeField] private TopRankCard rank01;
    [SerializeField] private TopRankCard rank02;
    [SerializeField] private TopRankCard rank03;

    [Header("전체 리더보드")]
    [SerializeField] private Transform content;
    [SerializeField] private RankRow rowTemplate;

    [Header("내 기록")]
    [SerializeField] private RankRow myRecordRow;

    // 오른쪽 위 전환 버튼 (Canvas 1920x1080 기준, 닉네임 검색창 왼쪽)
    private static readonly Vector2 TabsCenter = new Vector2(372f, 445f);
    private static readonly Vector2 TabSize = new Vector2(120f, 52f);
    private const float TabPadding = 6f;

    private static readonly Color Primary = Hex("#0C8CE9");
    private static readonly Color TrackColor = Hex("#E9ECEF");
    private static readonly Color TextDark = Hex("#1F2328");
    private static readonly Color TextGray = Hex("#6B7280");

    private readonly List<RankRow> generatedRows = new List<RankRow>();

    private LeaderboardMode mode;
    private int showRequest;

    private Image distanceTab;
    private Image scoreTab;
    private TMP_Text distanceTabText;
    private TMP_Text scoreTabText;
    private TMP_Text bestHeader;

    private IEnumerator Start()
    {
        while (LeaderboardManager.Instance == null)
            yield return null;

        BuildTabs();
        FindHeader();

        Show(LeaderboardModeSetting.Saved);
    }

    // ---------- 전환 ----------

    private void Show(LeaderboardMode newMode)
    {
        mode = newMode;
        LeaderboardModeSetting.Saved = newMode;

        UpdateTabs();

        // 원래 "최고기록"과 같은 글자 수 (띄어쓰기 없이)
        if (bestHeader != null)
            bestHeader.text = newMode == LeaderboardMode.Score ? "최고점수" : "최고거리";

        LeaderboardManager.Instance.SetMode(newMode);

        StartCoroutine(ShowWhenLoaded(newMode, ++showRequest));
    }

    private IEnumerator ShowWhenLoaded(LeaderboardMode target, int request)
    {
        // 아직 안 불러왔으면 비워 두고 기다림
        if (!LeaderboardManager.Instance.IsModeLoaded(target))
        {
            ShowEntries(new List<LeaderboardEntry>(), target, true);

            // 목록을 기다리는 동안에도 내 기록은 먼저 표시
            UpdateMyRecord(target, request);

            while (LeaderboardManager.Instance != null &&
                   !LeaderboardManager.Instance.IsModeLoaded(target))
            {
                yield return null;
            }
        }

        // 기다리는 동안 다른 탭을 눌렀으면 무시
        if (request != showRequest || LeaderboardManager.Instance == null)
            yield break;

        Debug.Log($"LeaderboardSceneUI : 리더보드 ({target}) 표시");

        ShowEntries(LeaderboardManager.Instance.GetEntries(target), target, false);
        UpdateMyRecord(target, request);
    }

    private void ShowEntries(List<LeaderboardEntry> entries, LeaderboardMode target, bool loading)
    {
        SetupCard(rank01, entries, 0, target, loading);
        SetupCard(rank02, entries, 1, target, loading);
        SetupCard(rank03, entries, 2, target, loading);

        UpdateScrollList(entries, target);
    }

    private static void SetupCard(TopRankCard card, List<LeaderboardEntry> entries, int index, LeaderboardMode target, bool loading)
    {
        if (card == null)
            return;

        if (index < entries.Count)
            card.SetupText(entries[index].nickname, entries[index].ValueText(target));
        else
            card.SetupText(loading ? "불러오는 중..." : "-", "");
    }

    private void UpdateScrollList(List<LeaderboardEntry> entries, LeaderboardMode target)
    {
        if (content == null || rowTemplate == null)
        {
            Debug.LogError("LeaderboardSceneUI : Content 또는 Row Template이 연결되지 않았습니다.");
            return;
        }

        // 기존에 생성된 행 삭제
        foreach (RankRow row in generatedRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        generatedRows.Clear();

        // 4위부터 생성
        for (int i = 3; i < entries.Count; i++)
        {
            LeaderboardEntry entry = entries[i];

            RankRow newRow = Instantiate(rowTemplate, content);
            newRow.gameObject.SetActive(true);
            newRow.transform.SetAsLastSibling();
            newRow.SetDataText(entry.rank, entry.nickname, entry.ValueText(target), null);

            generatedRows.Add(newRow);
        }
    }

    private void UpdateMyRecord(LeaderboardMode target, int request)
    {
        if (myRecordRow == null)
        {
            Debug.LogError("LeaderboardSceneUI : MyRecordRow가 연결되지 않았습니다.");
            return;
        }

        string nickname = "Player";
        if (SaveManager.Instance != null &&
            SaveManager.Instance.CurrentUser != null &&
            !string.IsNullOrEmpty(SaveManager.Instance.CurrentUser.nickname))
        {
            nickname = SaveManager.Instance.CurrentUser.nickname;
        }

        // 1) 내 기록은 바로 표시 (순위는 불러오는 동안 "…")
        float myValue = LeaderboardManager.Instance.MyBest(target);
        myRecordRow.SetDataText(0, nickname, LeaderboardQueries.Format(target, myValue), null);
        myRecordRow.SetRankLabel(myValue > 0f ? "…" : "-");

        if (myValue <= 0f)
            return;

        // 2) 순위
        bool answered = false;

        LeaderboardManager.Instance.LoadMyRank(target, (rank, value) =>
        {
            answered = true;

            if (request != showRequest || myRecordRow == null)
                return;

            myRecordRow.SetDataText(
                rank,
                nickname,
                LeaderboardQueries.Format(target, value),
                null);
        });

        // 3) 응답이 너무 늦으면 "-"
        if (!answered)
            StartCoroutine(RankTimeout(request, () => answered));
    }

    private IEnumerator RankTimeout(int request, System.Func<bool> answered)
    {
        yield return new WaitForSecondsRealtime(8f);

        if (!answered() && request == showRequest && myRecordRow != null)
            myRecordRow.SetRankLabel("-");
    }

    // ---------- 버튼 만들기 ----------

    private void BuildTabs()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("LeaderboardSceneUI : Canvas를 찾지 못해 거리/점수 버튼을 만들지 못했습니다.");
            return;
        }

        TMP_FontAsset font = KoreanFont.Get(null);

        // 바깥 회색 판
        Image track = CreateImage("RankModeTabs", canvas.transform, TrackColor, TabSize.y / 2f + TabPadding);
        RectTransform trackRect = track.rectTransform;
        trackRect.anchorMin = trackRect.anchorMax = new Vector2(0.5f, 0.5f);
        trackRect.pivot = new Vector2(0.5f, 0.5f);
        trackRect.anchoredPosition = TabsCenter;
        trackRect.sizeDelta = new Vector2(TabSize.x * 2f + TabPadding * 2f, TabSize.y + TabPadding * 2f);
        track.raycastTarget = false;

        // FadePanel 같은 전체 화면 패널보다 뒤에 오도록 (페이드가 버튼을 덮게)
        Transform fade = canvas.transform.Find("FadePanel");
        if (fade != null)
            track.transform.SetSiblingIndex(fade.GetSiblingIndex());

        distanceTab = CreateTab(trackRect, "DistanceTab", "거리", -TabSize.x / 2f, font, out distanceTabText,
            () => Show(LeaderboardMode.Distance));

        scoreTab = CreateTab(trackRect, "ScoreTab", "점수", TabSize.x / 2f, font, out scoreTabText,
            () => Show(LeaderboardMode.Score));
    }

    private Image CreateTab(RectTransform parent, string name, string label, float x, TMP_FontAsset font,
        out TMP_Text text, UnityEngine.Events.UnityAction onClick)
    {
        Image image = CreateImage(name, parent, Color.clear, TabSize.y / 2f);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = TabSize;

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(onClick);

        UIHoverScale hover = image.gameObject.AddComponent<UIHoverScale>();
        hover.hoverScale = 1.06f;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(rect, false);

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 26f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;

        RectTransform textRect = tmp.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        text = tmp;
        return image;
    }

    private void UpdateTabs()
    {
        bool score = mode == LeaderboardMode.Score;

        if (distanceTab != null)
            distanceTab.color = score ? new Color(1f, 1f, 1f, 0.001f) : Primary;   // 완전 투명이면 레이가 안 맞으므로 아주 살짝

        if (scoreTab != null)
            scoreTab.color = score ? Primary : new Color(1f, 1f, 1f, 0.001f);

        if (distanceTabText != null)
            distanceTabText.color = score ? TextGray : Color.white;

        if (scoreTabText != null)
            scoreTabText.color = score ? Color.white : TextGray;
    }

    private Canvas FindCanvas()
    {
        if (rank01 != null)
        {
            Canvas c = rank01.GetComponentInParent<Canvas>(true);
            if (c != null)
                return c.rootCanvas;
        }

        return FindFirstObjectByType<Canvas>();
    }

    /// <summary>목록 머리글 "최고기록" 글자</summary>
    private void FindHeader()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
            return;

        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == "BestText")
            {
                bestHeader = text;

                // 글자 영역이 좁아(오른쪽 여백) 글자가 바뀌면 두 줄로 깨지던 문제 → 한 줄로 고정
                bestHeader.textWrappingMode = TextWrappingModes.NoWrap;
                bestHeader.overflowMode = TextOverflowModes.Overflow;
                break;
            }
        }
    }

    private static Image CreateImage(string name, Transform parent, Color color, float radius)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.sprite = RoundedSprites.Rect;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = radius > 0f ? RoundedSprites.RectRadius / radius : 1f;
        image.color = color;
        return image;
    }

    private static Color Hex(string hex)
    {
        Color color;
        ColorUtility.TryParseHtmlString(hex, out color);
        return color;
    }
}
