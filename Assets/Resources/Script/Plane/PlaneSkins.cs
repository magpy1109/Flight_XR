using System;

/// <summary>
/// 텍스처 모델 스킨 목록 (10번부터).
///
/// 스킨 하나 = Resources/PlaneModel/이름/ 폴더의 파일 3개
///   - 이름Mesh.bytes      : 모델 ("PPM3" 형식, 기수 +Z / 위 +Y / 가장 긴 변 1, PlaneModels.LoadTextured 참고)
///   - 이름_BaseColor.png  : 색 텍스처
///   - 이름_Card.png       : 스킨씬 버튼 그림
///
/// 번호는 Firebase(보유 / 장착 스킨)에 그대로 저장되므로 한 번 정한 번호는 바꾸지 않는다.
/// 스킨씬에는 아래 순서대로 놓인다. (1페이지 : 기본, 스텔스 폭격기 다음 칸부터 / 한 페이지 9칸)
///
/// [새 스킨을 추가하는 방법]
/// 1. Resources/PlaneModel/이름/ 폴더에 위 파일 3개를 넣는다.
/// 2. 아래 All 목록 끝에 다음 번호로 한 줄 추가한다. (종류 = PlaneCategory, 충돌 효과음 / 애니메이션이 달라짐)
///    값 순서 : 번호, 이름, 제목, 설명, 종류, 크기(m), 트레일 좌우 간격, 트레일 높이, 해금 조건
///    트레일 위치는 모델 꼬리 끝에서 좌우 두 줄기가 나오는 자리 (엔진이 가운데 모여 있으면 간격을 작게)
/// </summary>
public static class PlaneSkins
{
    public class Skin
    {
        public int id;
        public string key;         // 폴더 / 파일 이름
        public string title;       // 스킨씬에 보이는 이름
        public string info;        // 스킨씬 설명 (해금 조건 포함)
        public string category;    // PlaneCategory
        public float size;         // 게임에서 가장 긴 변 길이 (m)
        public float trailX;       // 트레일이 나오는 꼬리 위치 : 가운데에서 좌우로 떨어진 거리 (모델 길이 1 기준)
        public float trailY;       // 트레일이 나오는 꼬리 위치 : 높이 (모델 길이 1 기준, 가운데 = 0)
        public Func<UserStats, bool> unlock;   // 해금 조건

        public string MeshPath => "PlaneModel/" + key + "/" + key + "Mesh";
        public string TexturePath => "PlaneModel/" + key + "/" + key + "_BaseColor";
        public string CardPath => "PlaneModel/" + key + "/" + key + "_Card";
    }

    public const int FirstId = 10;

    public static readonly Skin[] All =
    {
        New(10, "BallisticMissile", "탄도 미사일", "부딪히면 폭발하는 미사일 (최고 점수 300점 달성 시 해금)",
            PlaneCategory.Bomber, 0.24f, 0.03f, 0.00f, s => s.best_score >= 300),

        New(11, "BlueJetPaperPlane", "블루 제트", "파란 종이로 접은 제트기 (5회 플레이 시 해금)",
            PlaneCategory.Paper, 0.20f, 0.08f, 0.00f, s => s.play_count >= 5),

        New(12, "WhiteJetPaperPlane", "화이트 제트", "하얀 종이로 접은 제트기 (누적 비행거리 100m 달성 시 해금)",
            PlaneCategory.Paper, 0.20f, 0.07f, 0.00f, s => s.total_distance >= 100f),

        New(13, "PurplePaperPlane", "퍼플 글라이더", "넓은 날개의 보라색 종이비행기 (누적 높이 30m 달성 시 해금)",
            PlaneCategory.Paper, 0.20f, 0.30f, 0.00f, s => s.total_height >= 30f),

        New(14, "DollarPaperPlane", "달러 비행기", "지폐로 접은 종이비행기 (노란 네모 누적 50개 달성 시 해금)",
            PlaneCategory.Money, 0.20f, 0.28f, 0.05f, s => s.total_cubes >= 50),

        New(15, "OrigamiJetFighter", "오리가미 전투기", "은박지로 접은 전투기 (노란 네모 10개 연속 달성 시 해금)",
            PlaneCategory.Foil, 0.22f, 0.08f, 0.00f, s => s.best_combo >= 10),

        New(16, "PassengerPlane", "여객기", "승객을 가득 태운 여객기 (20회 플레이 시 해금)",
            PlaneCategory.Airliner, 0.24f, 0.06f, 0.00f, s => s.play_count >= 20),

        New(17, "Concorde", "콩코드", "소리보다 빠른 초음속 여객기 (최고 비행거리 30m 달성 시 해금)",
            PlaneCategory.Airliner, 0.26f, 0.04f, 0.00f, s => s.best_distance >= 30f),

        New(18, "VintageAirship", "비행선", "느긋하게 떠다니는 비행선 (누적 플레이 시간 20분 달성 시 해금)",
            PlaneCategory.Airship, 0.26f, 0.05f, 0.00f, s => s.total_play_time >= 1200f),

        New(19, "TRex", "티라노사우루스", "하늘을 나는 공룡 (누적 점수 1000점 달성 시 해금)",
            PlaneCategory.Dinosaur, 0.26f, 0.03f, 0.09f, s => s.total_score >= 1000),

        New(20, "NyanCat", "냥캣", "무지개를 그리며 나는 고양이 (노란 네모 누적 200개 달성 시 해금)",
            PlaneCategory.Cat, 0.24f, 0.03f, -0.08f, s => s.total_cubes >= 200),
    };

    /// <summary>가장 큰 스킨 번호</summary>
    public static int MaxId
    {
        get
        {
            int max = FirstId - 1;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].id > max)
                    max = All[i].id;
            }
            return max;
        }
    }

    /// <summary>번호의 스킨 (텍스처 모델 스킨이 아니면 null)</summary>
    public static Skin Get(int id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].id == id)
                return All[i];
        }
        return null;
    }

    private static Skin New(int id, string key, string title, string info, string category, float size,
        float trailX, float trailY, Func<UserStats, bool> unlock)
    {
        return new Skin
        {
            id = id, key = key, title = title, info = info, category = category, size = size,
            trailX = trailX, trailY = trailY, unlock = unlock
        };
    }
}
