using UnityEngine;

/// <summary>
/// 게임이 실행되는 첫 순간(어떤 씬이 먼저 로드되든 상관없이) 자동으로 실행되어
/// PersistentManagers 프리팹(AudioSettingsManager, BGMManager, SFXManager,
/// FrameRateSettingsManager를 담고 있음)을 생성합니다.
///
/// 이 클래스는 MonoBehaviour가 아니라 static 클래스라서
/// 어떤 GameObject에도 붙일 필요 없습니다. 파일이 프로젝트 안에 있기만 하면
/// [RuntimeInitializeOnLoadMethod] 덕분에 자동으로 호출됩니다.
///
/// [사전 준비]
/// 1. AudioSettingsManager, BGMManager, SFXManager, FrameRateSettingsManager를
///    전부 하나의 부모 오브젝트(PersistentManagers) 밑에 자식으로 배치
/// 2. 그 부모 오브젝트를 Assets/Resources/Prefab/PersistentManagers.prefab 으로 저장
/// 3. 원본은 씬에서 삭제 (프리팹만 있으면 됨)
/// </summary>
public static class GameBootstrapper
{
    private const string PREFAB_PATH = "Prefab/PersistentManagers"; // Resources 폴더 기준 상대경로

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // 이미 매니저가 존재하면(씬에 수동으로 남아있거나 이미 생성됐으면) 중복 생성 방지
        if (AudioSettingsManager.Instance != null)
        {
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(PREFAB_PATH);
        if (prefab == null)
        {
            Debug.LogError($"[GameBootstrapper] '{PREFAB_PATH}' 프리팹을 Resources 폴더에서 찾을 수 없습니다. " +
                            "Assets/Resources/Prefab/PersistentManagers.prefab 경로를 확인해주세요.");
            return;
        }

        GameObject instance = Object.Instantiate(prefab);
        instance.name = "PersistentManagers";
        Object.DontDestroyOnLoad(instance);

        Debug.Log("[GameBootstrapper] PersistentManagers 생성 완료.");
    }
}