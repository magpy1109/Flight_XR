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
/// - 내 기록은 아래 고정 줄뿐 아니라 랭킹 안(카드 / 목록)에도 파란 글씨로 표시.
///   상위 목록 밖이면 목록 맨 아래에 내 줄을 덧붙인다
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

    private LeaderboardManager subscribed;
    private RankRow myListRow;      // 목록 안의 내 줄 (순위를 나중에 채울 때 사용)

    private IEnumerator Start()
    {
        while (LeaderboardManager.Instance == null)
            yield return null;

        BuildTabs();
        FindHeader();

        // 기록 / 닉네임이 도착할 때마다 다시 그림
        subscribed = LeaderboardManager.Instance;
        subscribed.Updated += OnLeaderboardUpdated;

        Show(LeaderboardModeSetting.Saved);
    }

    private void OnDestroy()
    {
        if (subscribed != null)
            subscribed.Updated -= OnLeaderboardUpdated;
    }

    private void OnLeaderboardUpdated(LeaderboardMode updated)
    {
        if (updated == mode)
            Render();
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

        Render();
    }

    /// <summary>지금 선택한 랭킹을 화면에 그림 (아직 못 불러왔으면 "불러오는 중", 내 기록은 먼저 표시)</summary>
    private void Render()
    {
        LeaderboardManager manager = LeaderboardManager.Instance;
        if (manager == null)
            return;

        int request = ++showRequest;
        LeaderboardMode target = mode;
        bool loaded = manager.IsModeLoaded(target);

        string myId = LeaderboardManager.MyUserId;
        string myNickname = MyNickname();
        float myValue = manager.MyBest(target);

        // 보여줄 목록 = 서버 랭킹 + (랭킹에 없으면) 나
        var entries = new List<LeaderboardEntry>();
        LeaderboardEntry me = null;

        if (loaded)
        {
            foreach (LeaderboardEntry e in manager.GetEntries(target))
            {
                LeaderboardEntry copy = new LeaderboardEntry
                {
                    rank = e.rank,
                    userId = e.userId,
                    nickname = e.nickname,
                    bestDistance = e.bestDistance,
                    bestScore = e.bestScore
                };

                if (myId != null && e.userId == myId)
                {
                    me = copy;
                    me.nickname = myNickname;
                    myValue = Mathf.Max(myValue, me.Value(target));
                }

                entries.Add(copy);
            }

            if (me == null && myValue > 0f)
            {
                me = new LeaderboardEntry { userId = myId, nickname = myNickname, rank = 0 };
                if (target == LeaderboardMode.Score)
                    me.bestScore = Mathf.RoundToInt(myValue);
                else
                    me.bestDistance = myValue;

                // 내 기록이 들어갈 자리
                int position = entries.Count;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].Value(target) < myValue)
                    {
                        position = i;
                        break;
                    }
                }

                if (position < entries.Count || entries.Count < manager.TopCount)
                {
                    entries.Insert(position, me);
                    LeaderboardManager.AssignRanks(entries, target);
                }
                else
                {
                    entries.Add(me);   // 상위 목록 밖 : 맨 아래에 내 줄 (순위는 따로 조회)
                }
            }
        }

        SetupCard(rank01, entries, 0, target, !loaded, me);
        SetupCard(rank02, entries, 1, target, !loaded, me);
        SetupCard(rank03, entries, 2, target, !loaded, me);

        UpdateScrollList(entries, target, me);
        UpdateMyRecord(target, request, myNickname, myValue, me != null ? me.rank : 0);
    }

    private static string MyNickname()
    {
        if (SaveManager.Instance != null &&
            SaveManager.Instance.CurrentUser != null &&
            !string.IsNullOrEmpty(SaveManager.Instance.CurrentUser.nickname))
        {
            return SaveManager.Instance.CurrentUser.nickname;
        }

        return "Player";
    }

    private static void SetupCard(TopRankCard card, List<LeaderboardEntry> entries, int index, LeaderboardMode target,
        bool loading, LeaderboardEntry me)
    {
        if (card == null)
            return;

        if (index < entries.Count)
        {
            card.SetupText(entries[index].nickname, entries[index].ValueText(target));
            card.SetHighlight(entries[index] == me, Primary);
        }
        else
        {
            card.SetupText(loading ? "불러오는 중..." : "-", "");
            card.SetHighlight(false, Primary);
        }
    }

    private void UpdateScrollList(List<LeaderboardEntry> entries, LeaderboardMode target, LeaderboardEntry me)
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
        myListRow = null;

        // 4번째부터 생성 (1~3번째는 위 카드)
        for (int i = 3; i < entries.Count; i++)
        {
            LeaderboardEntry entry = entries[i];

            RankRow newRow = Instantiate(rowTemplate, content);
            newRow.gameObject.SetActive(true);
            newRow.transform.SetAsLastSibling();
            newRow.SetDataText(entry.rank, entry.nickname, entry.ValueText(target), null);

            if (entry == me)
            {
                myListRow = newRow;
                newRow.SetHighlight(Primary);

                if (entry.rank <= 0)
                    newRow.SetRankLabel("…");
            }

            generatedRows.Add(newRow);
        }
    }

    /// <summary>아래 고정 "내 기록" 줄. knownRank가 0이면 순위를 따로 조회한다.</summary>
    private void UpdateMyRecord(LeaderboardMode target, int request, string nickname, float myValue, int knownRank)
    {
        if (myRecordRow == null)
        {
            Debug.LogError("LeaderboardSceneUI : MyRecordRow가 연결되지 않았습니다.");
            return;
        }

        string valueText = LeaderboardQueries.Format(target, myValue);

        // 1) 내 기록은 바로 표시
        myRecordRow.SetDataText(knownRank, nickname, valueText, null);

        if (myValue <= 0f || knownRank > 0)
            return;

        // 2) 순위 (불러오는 동안 "…")
        myRecordRow.SetRankLabel("…");

        bool answered = false;

        LeaderboardManager.Instance.LoadMyRank(target, (rank, value) =>
        {
            answered = true;

            if (request != showRequest || myRecordRow == null)
                return;

            myRecordRow.SetDataText(rank, nickname, valueText, null);

            if (myListRow != null)
                myListRow.SetRankLabel(rank > 0 ? rank.ToString() : "-");
        });

        // 3) 응답이 너무 늦으면 "-"
        if (!answered)
            StartCoroutine(RankTimeout(request, () => answered));
    }

    private IEnumerator RankTimeout(int request, System.Func<bool> answered)
    {
        yield return new WaitForSecondsRealtime(8f);

        if (!answered() && request == showRequest)
        {
            if (myRecordRow != null)
                myRecordRow.SetRankLabel("-");

            if (myListRow != null)
                myListRow.SetRankLabel("-");
        }
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
