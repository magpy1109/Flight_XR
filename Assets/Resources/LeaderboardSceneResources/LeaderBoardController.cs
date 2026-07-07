using UnityEngine;

public class LeaderBoardController : MonoBehaviour
{
    [Header("Fade")]
    public FadeManager fadeManager;

    [Header("Top Rank Cards")]
    public TopRankCard rank01Card;
    public TopRankCard rank02Card;
    public TopRankCard rank03Card;

    [Header("Scroll Rank")]
    public Transform content;
    public RankRow rankRowPrefab;

    [Header("My Record")]
    public RankRow myRecordRow;

    private void Start()
    {
        SetSampleTopRanks();
        CreateSampleRankRows();
        SetMyRecord();
    }

    public void GoToMainMenu()
    {
        fadeManager.LoadScene("MainMenuScene");
    }

    private void SetSampleTopRanks()
    {
        rank01Card.Setup("JeonSik", 2471.8f);
        rank02Card.Setup("GoDon", 2382.5f);
        rank03Card.Setup("BaekSoJung", 2253.4f);
    }

    private void CreateSampleRankRows()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        float distance = 1732.6f;

        for (int rank = 4; rank <= 100; rank++)
        {
            string playerName = "Player" + (rank - 3);
            CreateRankRow(rank, playerName, distance);
            distance -= Random.Range(5f, 15f);
        }

        CreateRankRow(113, "LeeDo", 851.7f);
    }

    private void CreateRankRow(int rank, string playerName, float distance)
    {
        RankRow row = Instantiate(rankRowPrefab, content);
        row.SetData(rank, playerName, distance, null);
    }

    private void SetMyRecord()
    {
        myRecordRow.SetData(113, "LeeDo", 851.7f, null);
    }
}