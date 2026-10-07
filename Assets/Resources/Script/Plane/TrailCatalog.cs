using UnityEngine;

/// <summary>
/// 트레일 목록 (스킨씬 트레일 탭에 이 순서대로 놓인다. 한 페이지 9칸)
///
/// 번호는 Firebase(users.equipped_trail_id)에 그대로 저장되므로 한 번 정한 번호는 바꾸지 않는다.
///   0("default")  스킨별 기본 트레일 : 지금 장착한 비행기에 어울리는 트레일을 자동으로 사용
///   100           클래식 이펙트 (예전의 0번. 기본으로 주어지는 주황 불꽃)
///   1 ~ 8         색 트레일 (Resources/PlaneModel/PlaneTrails 프리팹의 2 ~ 9번째)
///   9             폭격기 비행운
///   10 ~ 20       스킨마다 어울리게 만든 트레일 (번호 = 그 스킨 번호, TrailEffects에서 코드로 만듦)
///
/// 보유 여부는 스킨과 같은 ID를 쓴다. (9 ~ 20번 트레일 = 같은 번호의 스킨을 가지고 있으면 사용 가능)
/// 0번과 100번은 누구나 쓸 수 있다. (SaveManager.HasSkin)
///
/// [새 트레일을 추가하는 방법]
/// 1. 아래 All 목록 끝에 한 줄 추가 (번호는 겹치지 않게)
/// 2. TrailEffects.Layers 에 그 번호의 파티클 모양을 추가
/// </summary>
public static class TrailCatalog
{
    public const int AutoId = 0;
    public const int ClassicId = 100;
    public const int BomberId = 9;
    public const int MaxId = 100;

    /// <summary>누구나 쓸 수 있는 클래식 이펙트의 보유 ID</summary>
    public const string ClassicKey = "100";

    public class Trail
    {
        public int id;
        public string key;
        public string title;
        public string info;
        public int prefabIndex = -1;   // PlaneTrails 프리팹의 몇 번째 자식인지 (코드로 만드는 트레일은 -1)
        public Color iconColor;        // 스킨씬 버튼의 동그라미 색
        public string iconPath;        // 버튼 그림 (없으면 동그라미에 색만 입힘)
    }

    public static readonly Trail[] All =
    {
        new Trail { id = AutoId, key = "Auto", title = "스킨별 기본 트레일",
            info = "지금 장착한 비행기에 어울리는 트레일이 자동으로 나옵니다",
            iconColor = Color.white, iconPath = "PlaneModel/TrailIcons/Auto" },

        Prefab(ClassicId, 0, "Classic", "클래식 이펙트", "주황빛 기본 불꽃", new Color(1f, 0.667f, 0f)),
        Prefab(1, 1, "SkyBlue", "스카이 블루", "하늘색 불꽃", new Color(0.471f, 0.796f, 0.906f)),
        Prefab(2, 2, "RoseRed", "로즈 레드", "빨간 불꽃", new Color(1f, 0f, 0f)),
        Prefab(3, 3, "SunshineYellow", "선샤인 옐로우", "노란 불꽃", new Color(1f, 1f, 0f)),
        Prefab(4, 4, "StealthBlack", "스텔스 블랙", "검은 불꽃", new Color(0f, 0f, 0f)),
        Prefab(5, 5, "MillitaryCamo", "밀리터리 카모", "국방색 불꽃", new Color(0.325f, 0.388f, 0.286f)),
        Prefab(6, 6, "Orora", "오로라", "청록빛 불꽃", new Color(0.337f, 1f, 0.867f)),
        Prefab(7, 7, "Craft", "크래프트", "갈색 불꽃", new Color(0.561f, 0.459f, 0.271f)),
        Prefab(8, 8, "Galaxy", "갤럭시", "짙은 보랏빛 불꽃", new Color(0.106f, 0.008f, 0.275f)),

        Custom(9, "Contrail", "폭격기 비행운", "하얗고 긴 비행운 (스텔스 폭격기를 가지고 있으면 사용 가능)", new Color(0.86f, 0.9f, 0.95f)),
        Custom(10, "RocketFlame", "로켓 화염", "불꽃과 연기를 뿜는 로켓 (탄도 미사일을 가지고 있으면 사용 가능)", new Color(1f, 0.42f, 0.1f)),
        Custom(11, "BlueJet", "제트 불꽃", "푸른 애프터버너 (블루 제트를 가지고 있으면 사용 가능)", new Color(0.2f, 0.55f, 1f)),
        Custom(12, "Vapor", "구름 줄기", "날개 끝에서 피어나는 하얀 구름 (화이트 제트를 가지고 있으면 사용 가능)", new Color(0.8f, 0.86f, 0.94f)),
        Custom(13, "Stardust", "보랏빛 별가루", "반짝이는 보라색 별가루 (퍼플 글라이더를 가지고 있으면 사용 가능)", new Color(0.7f, 0.4f, 1f)),
        Custom(14, "GoldCoins", "황금 동전", "동전과 지폐가 흩날림 (달러 비행기를 가지고 있으면 사용 가능)", new Color(1f, 0.8f, 0.15f)),
        Custom(15, "SilverGlitter", "은빛 반짝이", "은박지처럼 반짝이는 가루 (오리가미 전투기를 가지고 있으면 사용 가능)", new Color(0.72f, 0.76f, 0.84f)),
        Custom(16, "AirlinerContrail", "여객기 비행운", "높은 하늘의 두 줄 비행운 (여객기를 가지고 있으면 사용 가능)", new Color(0.7f, 0.82f, 0.96f)),
        Custom(17, "Afterburner", "초음속 불꽃", "길게 뻗는 주황 불꽃 (콩코드를 가지고 있으면 사용 가능)", new Color(1f, 0.62f, 0.2f)),
        Custom(18, "Steam", "증기 구름", "몽글몽글 피어오르는 증기 (비행선을 가지고 있으면 사용 가능)", new Color(0.9f, 0.9f, 0.9f)),
        Custom(19, "Dust", "흙먼지", "흙먼지와 나뭇잎 (티라노사우루스를 가지고 있으면 사용 가능)", new Color(0.62f, 0.5f, 0.34f)),

        new Trail { id = 20, key = "Rainbow", title = "무지개", info = "일곱 빛깔 무지개 줄기 (냥캣을 가지고 있으면 사용 가능)",
            iconColor = Color.white, iconPath = "PlaneModel/TrailIcons/Rainbow" },
    };

    private static Trail Prefab(int id, int prefabIndex, string key, string title, string info, Color color)
    {
        return new Trail { id = id, key = key, title = title, info = info, prefabIndex = prefabIndex, iconColor = color };
    }

    private static Trail Custom(int id, string key, string title, string info, Color color)
    {
        return new Trail { id = id, key = key, title = title, info = info, iconColor = color };
    }

    public static Trail Get(int id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].id == id)
                return All[i];
        }
        return null;
    }

    /// <summary>없는 번호는 스킨별 기본 트레일로 바꾼다</summary>
    public static int Normalize(int id)
    {
        return Get(id) != null ? id : AutoId;
    }

    /// <summary>그 스킨에 어울리는 트레일 번호 (스킨별 기본 트레일을 골랐을 때 실제로 쓰는 것)</summary>
    public static int ForSkin(int skinId)
    {
        if (skinId == PlaneSkinState.BomberSkin)
            return BomberId;

        if (PlaneSkins.Get(skinId) != null && Get(skinId) != null)
            return skinId;

        return ClassicId;
    }

    /// <summary>장착한 트레일 번호 → 실제로 붙일 트레일 번호</summary>
    public static int Resolve(int trailId, int skinId)
    {
        trailId = Normalize(trailId);
        return trailId == AutoId ? ForSkin(skinId) : trailId;
    }
}
