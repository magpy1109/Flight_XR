using System.Collections.Generic;

/// <summary>
/// 비행기 종류(카테고리). 카테고리마다 충돌 효과음과 충돌 애니메이션이 달라진다.
///
/// - 효과음 : Resources/Sound/Crash/카테고리이름/ 폴더 안의 소리 (여러 개면 무작위, 없으면 종이 소리)
/// - 애니메이션 (PlaneCrashEffect / CrashEffects)
///     paper    종이비행기        : 종이처럼 구겨져 떨어짐
///     money    달러 비행기       : 구겨지면서 동전 / 지폐가 흩어짐
///     foil     은박 오리가미     : 구겨지면서 은빛 반짝이가 튐
///     bomber   폭격기 / 미사일   : 폭발하고 사라짐
///     airliner 여객기 / 콩코드   : 불꽃과 연기를 내며 찌그러져 튕김
///     airship  비행선            : 바람이 빠지며 이리저리 날아다니다 쪼그라들어 떨어짐
///     dinosaur 공룡              : 울부짖으며 흙먼지를 일으키고 나뒹굼
///     cat      냥캣              : 무지개 별을 터뜨리며 뿅 하고 사라짐
///
/// 스킨 0번(클래식 화이트)은 paper, 9번(스텔스 폭격기)은 bomber.
/// 10번부터의 텍스처 모델 스킨은 PlaneSkins 목록에 적힌 카테고리를 쓴다.
///
/// [새 종류를 추가하는 방법]
/// 1. 아래에 카테고리 이름을 추가하고 PlaneSkins 목록의 스킨에 적는다.
/// 2. Resources/Sound/Crash/이름/ 폴더에 소리 파일 추가
/// 3. 따로 정하지 않으면 구겨지지 않고 찌그러지듯 튕기며 떨어진다.
/// </summary>
public static class PlaneCategory
{
    public const string Paper = "paper";
    public const string Dinosaur = "dinosaur";
    public const string Bomber = "bomber";
    public const string Money = "money";
    public const string Foil = "foil";
    public const string Airliner = "airliner";
    public const string Airship = "airship";
    public const string Cat = "cat";

    // 스킨 번호 → 카테고리 (등록되지 않은 번호는 모두 종이비행기)
    private static readonly Dictionary<int, string> SkinCategories = new Dictionary<int, string>
    {
        { 9, Bomber },
    };

    /// <summary>스킨 번호의 카테고리</summary>
    public static string OfSkin(int skinIndex)
    {
        PlaneSkins.Skin skin = PlaneSkins.Get(skinIndex);
        if (skin != null && !string.IsNullOrEmpty(skin.category))
            return skin.category;

        string category;
        return SkinCategories.TryGetValue(skinIndex, out category) ? category : Paper;
    }

    /// <summary>부딪혔을 때 폭발하는 종류인지</summary>
    public static bool Explodes(string category)
    {
        return category == Bomber;
    }

    /// <summary>부딪혔을 때 종이처럼 구겨지는 종류인지</summary>
    public static bool Crumples(string category)
    {
        return string.IsNullOrEmpty(category) || category == Paper || category == Money || category == Foil;
    }

    /// <summary>부딪혔을 때 바람이 빠지며 날아다니는 종류인지 (비행선)</summary>
    public static bool Deflates(string category)
    {
        return category == Airship;
    }

    /// <summary>부딪혔을 때 별을 터뜨리며 사라지는 종류인지 (냥캣)</summary>
    public static bool Poofs(string category)
    {
        return category == Cat;
    }
}
