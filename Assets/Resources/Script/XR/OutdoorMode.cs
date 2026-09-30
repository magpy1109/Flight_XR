using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 야외 모드 (설정씬 "플레이 설정"에서 켜고 끔. 게임 중 일시정지 설정에서는 바꿀 수 없음)
///
/// 켜면 게임씬에서
/// - 방 공간 인식(MRUK)으로 만든 벽 / 가구 충돌을 끈다. (작게 스캔한 방 밖으로 나가도 허공에서 추락하지 않게)
/// - 방 데이터가 없을 때 예시 방(프리팹)으로 대신 쓰지 않는다.
/// - World Lock(방 기준 위치 보정)을 끈다.
/// - 방 데이터가 없어도 "공간 설정을 하시겠습니까?" 창을 띄우지 않는다.
/// → 충돌은 실시간 공간 인식(PlaneEnvironmentCollision)과 트래킹 바닥으로만 판정한다.
///
/// 끄면 기존과 같다. (실내 : 방 공간 인식 벽 충돌 + 실시간 공간 인식)
/// </summary>
public static class OutdoorMode
{
    private const string Key = "Setting_OutdoorMode";

    public static bool Enabled => PlayerPrefs.GetInt(Key, 0) == 1;

    public static void Set(bool enabled)
    {
        PlayerPrefs.SetInt(Key, enabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[OutdoorMode] 야외 모드 = {enabled}");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply();
    }

    /// <summary>씬이 로드된 직후(MRUK가 방을 불러오기 전)에 설정을 바꾼다.</summary>
    private static void Apply()
    {
        if (!Enabled)
            return;

        MRUK mruk = Object.FindFirstObjectByType<MRUK>();
        if (mruk != null)
        {
            mruk.EnableWorldLock = false;

            if (mruk.SceneSettings != null)
            {
                mruk.SceneSettings.DataSource = MRUK.SceneDataSource.Device;

                // MRUK가 시작할 때 자동으로 방을 불러오면, 방 데이터가 없을 때
                // "공간 설정을 하시겠습니까?" 창을 띄운다 → 자동 불러오기를 끄고
                // 창 없이(requestSceneCaptureIfNoDataFound = false) 직접 불러온다.
                if (mruk.SceneSettings.LoadSceneOnStartup)
                {
                    mruk.SceneSettings.LoadSceneOnStartup = false;
                    mruk.StartCoroutine(LoadSceneWithoutPrompt(mruk));
                }
            }

            // 이미 불러온 방이 있거나 나중에 불러와도 충돌이 생기지 않게
            mruk.SceneLoadedEvent.AddListener(DisableRoomColliders);
        }

        foreach (EffectMesh effectMesh in Object.FindObjectsByType<EffectMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            effectMesh.Colliders = false;

        DisableRoomColliders();

        if (mruk != null)
            Debug.Log("[OutdoorMode] 야외 모드 적용 : 방 벽 충돌 끔, 예시 방 사용 안 함, World Lock 끔");
    }

    private static System.Collections.IEnumerator LoadSceneWithoutPrompt(MRUK mruk)
    {
        // MRUK 초기화(Start)가 끝난 다음 프레임에 실행
        yield return null;

        if (mruk == null)
            yield break;

        var task = mruk.LoadSceneFromDevice(requestSceneCaptureIfNoDataFound: false);

        while (!task.IsCompleted)
            yield return null;

        Debug.Log($"[OutdoorMode] 방 데이터 불러오기 (공간 설정 창 없이) : {(task.IsFaulted ? "실패" : task.Result.ToString())}");
    }

    private static void DisableRoomColliders()
    {
        foreach (EffectMesh effectMesh in Object.FindObjectsByType<EffectMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            effectMesh.Colliders = false;

            try
            {
                effectMesh.ToggleColliders = false;
            }
            catch (System.Exception)
            {
                // 아직 만들어진 메시가 없으면 무시
            }
        }

        // 방 앵커에 붙은 콜라이더도 끔 (EffectMesh 외의 방 충돌)
        foreach (MRUKAnchor anchor in Object.FindObjectsByType<MRUKAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (Collider c in anchor.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }
    }
}
