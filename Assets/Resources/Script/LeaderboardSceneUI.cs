using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private List<RankRow> generatedRows =
        new List<RankRow>();

    private IEnumerator Start()
    {
        while (LeaderboardManager.Instance == null)
        {
            yield return null;
        }

        while (!LeaderboardManager.Instance.IsLoaded)
        {
            yield return null;
        }

        Debug.Log(
            "LeaderboardSceneUI : 리더보드 데이터 로드 완료"
        );

        UpdateTop3();
        UpdateScrollList();
        UpdateMyRecord();
    }

    private void UpdateTop3()
    {
        List<LeaderboardEntry> entries =
            LeaderboardManager.Instance.leaderboardEntries;

        if (entries.Count > 0)
        {
            rank01.Setup(
                entries[0].nickname,
                entries[0].bestDistance
            );
        }

        if (entries.Count > 1)
        {
            rank02.Setup(
                entries[1].nickname,
                entries[1].bestDistance
            );
        }

        if (entries.Count > 2)
        {
            rank03.Setup(
                entries[2].nickname,
                entries[2].bestDistance
            );
        }
    }

    private void UpdateScrollList()
    {
        Debug.Log(
            "LeaderboardSceneUI : UpdateScrollList()"
        );

        if (content == null)
        {
            Debug.LogError(
                "LeaderboardSceneUI : Content가 없습니다."
            );
            return;
        }

        if (rowTemplate == null)
        {
            Debug.LogError(
                "LeaderboardSceneUI : Row Template이 없습니다."
            );
            return;
        }

        List<LeaderboardEntry> entries =
            LeaderboardManager.Instance.leaderboardEntries;

        Debug.Log(
            $"리더보드 데이터 수 : {entries.Count}"
        );

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
            LeaderboardEntry entry =
                entries[i];

            RankRow newRow =
                Instantiate(rowTemplate, content);

            newRow.gameObject.SetActive(true);

            newRow.transform.SetAsLastSibling();

            newRow.SetData(
                entry.rank,
                entry.nickname,
                entry.bestDistance,
                null
            );

            generatedRows.Add(newRow);

            Debug.Log(
                $"리더보드 행 생성 : " +
                $"{entry.rank}위 / " +
                $"{entry.nickname} / " +
                $"{entry.bestDistance:F1}m"
            );
        }

        Debug.Log(
            $"LeaderboardSceneUI : " +
            $"{generatedRows.Count}개 행 생성 완료"
        );
    }

    private void UpdateMyRecord()
    {
        if (myRecordRow == null)
        {
            Debug.LogError(
                "LeaderboardSceneUI : MyRecordRow가 연결되지 않았습니다."
            );
            return;
        }

        LeaderboardManager.Instance.LoadMyRank(
            (rank, distance) =>
            {
                if (rank <= 0)
                {
                    myRecordRow.SetData(
                        0,
                        "Player",
                        distance,
                        null
                    );

                    return;
                }

                string nickname = "Player";

                if (SaveManager.Instance != null &&
                    SaveManager.Instance.CurrentUser != null)
                {
                    nickname =
                        SaveManager.Instance.CurrentUser.nickname;
                }

                myRecordRow.SetData(
                    rank,
                    nickname,
                    distance,
                    null
                );

                Debug.Log(
                    $"내 기록 UI 갱신 완료 : " +
                    $"{rank}위 / {nickname} / {distance:F1}m"
                );
            }
        );
    }
}