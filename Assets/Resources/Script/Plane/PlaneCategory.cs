using System.Collections.Generic;

/// <summary>
/// 비행기 종류(카테고리).
///
/// 카테고리마다 충돌 효과음과 충돌 애니메이션이 달라진다.
/// - 효과음 : Resources/Sound/Crash/카테고리이름/ 폴더 안의 소리 (여러 개면 무작위)
///   예) paper    → Resources/Sound/Crash/paper/PaperCrumple.wav (종이 구겨지는 소리)
///       dinosaur → Resources/Sound/Crash/dinosaur/ 에 공룡 울음소리 파일을 넣으면 됨
/// - 애니메이션 : Crumples(카테고리)가 true면 종이처럼 구겨짐, false면 찌그러지듯 튕기고 굴러감
///
/// 스킨 0~8(클래식 화이트 ~ 갤럭시)은 종이비행기(paper), 9번은 스텔스 폭격기(bomber : 폭발음 + 폭발).
///
/// [새 종류를 추가하는 방법]
/// 1. 아래 SkinCategories에 스킨 번호와 카테고리 이름을 등록  예) { 9, Dinosaur }
/// 2. Resources/Sound/Crash/dinosaur/ 폴더에 소리 파일 추가
/// 3. 구겨지면 안 되는 종류(공룡 등)는 Crumples에서 false가 되도록 그대로 두면 됨
/// (스킨 번호를 9개 넘게 쓰려면 PlaneSkinState의 색 목록도 함께 늘려야 함)
/// </summary>
public static class PlaneCategory
{
    public const string Paper = "paper";
    public const string Dinosaur = "dinosaur";
    public const string Bomber = "bomber";      // 스텔스 폭격기 (부딪히면 폭발, 소리 : Sound/Crash/bomber/)

    // 스킨 번호 → 카테고리 (등록되지 않은 번호는 모두 종이비행기)
    private static readonly Dictionary<int, string> SkinCategories = new Dictionary<int, string>
    {
        { 9, Bomber },
    };

    /// <summary>스킨 번호의 카테고리</summary>
    public static string OfSkin(int skinIndex)
    {
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
        return string.IsNullOrEmpty(category) || category == Paper;
    }
}
