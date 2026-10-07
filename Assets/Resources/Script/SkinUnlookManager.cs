using UnityEngine;

public class SkinUnlockManager : MonoBehaviour
{
    public static SkinUnlockManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    public void CheckUnlocks(UserStats stats)
    {
        if (stats == null)
        {
            Debug.LogError("SkinUnlockManager : UserStats가 null입니다.");
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError("SkinUnlockManager : SaveManager가 없습니다.");
            return;
        }

        if (!SaveManager.Instance.IsLoaded)
        {
            Debug.LogWarning("SkinUnlockManager : SaveManager 데이터가 아직 로드되지 않았습니다.");
            return;
        }

        // 0번은 기본 스킨이므로 검사하지 않음
        //
        // 1~8번 : 비행기 색 스킨은 삭제됐지만 같은 번호의 트레일이 이 보유 ID를 같이 쓴다.
        //         (SaveManager.EquipTrail이 HasSkin으로 확인) → 지우면 트레일을 해금할 수 없게 된다.

        // 1. 스카이블루
        // 3회 플레이
        if (stats.play_count >= 3)
        {
            Unlock("1", "스카이블루");
        }

        // 2. 로즈레드
        // 누적 점수 100
        if (stats.total_score >= 100)
        {
            Unlock("2", "로즈레드");
        }

        // 3. 선샤인옐로우
        // 최고 비행거리 10m
        if (stats.best_distance >= 10f)
        {
            Unlock("3", "선샤인옐로우");
        }

        // 4. 스텔스블랙
        // 10회 플레이
        if (stats.play_count >= 10)
        {
            Unlock("4", "스텔스블랙");
        }

        // 5. 밀리터리카모
        // 최고 점수 200
        if (stats.best_score >= 200)
        {
            Unlock("5", "밀리터리카모");
        }

        // 6. 오로라
        // 누적 비행거리 50m
        if (stats.total_distance >= 50f)
        {
            Unlock("6", "오로라");
        }

        // 7. 크래프트
        // 누적 높이 20m
        if (stats.total_height >= 20f)
        {
            Unlock("7", "크래프트");
        }

        // 8. 갤럭시
        // 누적 플레이 시간 10분
        if (stats.total_play_time >= 600f)
        {
            Unlock("8", "갤럭시");
        }

        // 9. 스텔스 폭격기
        // 최고 점수 500 (노란 네모 점수)
        if (stats.best_score >= 500)
        {
            Unlock("9", "스텔스 폭격기");
        }

        // 10번부터 : 텍스처 모델 스킨 (해금 조건은 PlaneSkins 목록에 있음)
        foreach (PlaneSkins.Skin skin in PlaneSkins.All)
        {
            if (skin.unlock != null && skin.unlock(stats))
                Unlock(skin.id.ToString(), skin.title);
        }
    }

    private void Unlock(string skinId, string skinName)
    {
        if (SaveManager.Instance.HasSkin(skinId))
            return;

        Debug.Log(
            $"🎉 스킨 해금 조건 달성 : {skinName} ({skinId})"
        );

        SaveManager.Instance.UnlockSkin(skinId);
    }
}