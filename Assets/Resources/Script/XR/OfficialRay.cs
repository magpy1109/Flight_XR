using System;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

/// <summary>
/// 메타 공식 레이(Interaction SDK 컨트롤러 레이)를 메인메뉴 카메라 리그에 설치한다.
///
/// - 게임 시작 시 PersistentXRRoot 아래의 OVRCameraRig에 OVRComprehensiveInteractionRig를 한 번만 붙인다.
///   (카메라 리그가 씬을 넘어가므로 레이도 모든 씬에서 유지됨)
/// - 이동(텔레포트/회전) 기능과 핸드 트래킹 데이터는 이 게임에서 쓰지 않으므로 끈다.
/// - 각 씬의 Canvas 연결은 OfficialRayRunner / IsdkCanvasProxy가 담당한다.
///
/// static 클래스라 어떤 오브젝트에도 붙일 필요 없음.
/// </summary>
public static class OfficialRay
{
    private const string SettingsPath = "OfficialRaySettings";

    /// <summary>공식 레이가 설치되어 사용 중인지 (true면 기존 QuestUIRayInteractor 레이는 숨김)</summary>
    public static bool IsActive { get; private set; }

    public static OfficialRaySettings Settings { get; private set; }

    /// <summary>설치된 리그 루트 (Canvas 자동 연결 대상에서 제외하기 위해 사용)</summary>
    public static GameObject RigInstance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsActive = false;
        Settings = null;
        RigInstance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        Settings = Resources.Load<OfficialRaySettings>(SettingsPath);

        if (Settings == null)
        {
            Debug.LogWarning($"[OfficialRay] Resources/{SettingsPath} 설정 파일이 없어 기존 레이를 사용합니다.");
            return;
        }

        if (!Settings.useOfficialRay)
        {
            Debug.Log("[OfficialRay] 설정에서 꺼져 있어 기존 레이를 사용합니다.");
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        EnsureInstalled();
        OfficialRayRunner.EnsureExists();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 설정씬 등에서 바로 실행해 메인메뉴를 늦게 거친 경우에도 설치되도록
        EnsureInstalled();
        OfficialRayRunner.EnsureExists();
        OfficialRayRunner.RequestRescan();
    }

    /// <summary>헤드셋이 연결되어 VR로 실행 중인지</summary>
    public static bool IsHmdPresent()
    {
        try
        {
            if (OVRManager.isHmdPresent)
                return true;
        }
        catch (Exception)
        {
            // OVRManager 초기화 전
        }

        return UnityEngine.XR.XRSettings.isDeviceActive;
    }

    private static void EnsureInstalled()
    {
        if (RigInstance != null)
        {
            IsActive = true;
            return;
        }

        IsActive = false;

        if (Settings == null || Settings.interactionRigPrefab == null)
        {
            Debug.LogWarning("[OfficialRay] Interaction Rig 프리팹이 지정되지 않았습니다.");
            return;
        }

        if (PersistentXRRoot.Instance == null)
            return; // 메인메뉴의 카메라 리그가 아직 없음

        OVRCameraRig cameraRig = PersistentXRRoot.Instance.GetComponentInChildren<OVRCameraRig>(true);
        if (cameraRig == null)
        {
            Debug.LogWarning("[OfficialRay] PersistentXRRoot 아래에서 OVRCameraRig를 찾지 못했습니다.");
            return;
        }

        // 비활성 부모 아래에 만들어서, 설정을 마치기 전까지 Awake/Start가 실행되지 않게 한다.
        GameObject holder = new GameObject("OfficialRayHolder");
        holder.SetActive(false);

        OVRCameraRigRef rigRef = UnityEngine.Object.Instantiate(Settings.interactionRigPrefab, holder.transform);
        GameObject rigObject = rigRef.gameObject;
        rigObject.name = "[OfficialRay] OVRInteractionRig";

        // 카메라 리그 연결 (핸드 트래킹 OVRHand가 없으므로 필수 아님으로 설정)
        rigRef.InjectAllOVRCameraRigRef(cameraRig, false);

        DisableUnusedFeatures(rigObject);

        // 카메라 리그 아래로 옮기면서 활성화
        rigObject.transform.SetParent(cameraRig.transform, false);
        rigObject.transform.localPosition = Vector3.zero;
        rigObject.transform.localRotation = Quaternion.identity;
        rigObject.transform.localScale = Vector3.one;

        UnityEngine.Object.Destroy(holder);

        RigInstance = rigObject;
        IsActive = true;

        Debug.Log("[OfficialRay] 메타 공식 컨트롤러 레이 설치 완료");
    }

    /// <summary>
    /// 이 게임에서 쓰지 않는 기능 끄기
    /// - 이동(텔레포트 / 스냅 회전 / 슬라이드) : 썸스틱으로 플레이어가 움직이는 것 방지
    /// - 핸드 트래킹 데이터 : 카메라 리그에 OVRHand가 없어 에러가 나는 것 방지
    /// </summary>
    private static void DisableUnusedFeatures(GameObject rigObject)
    {
        RayInteractor[] rays = rigObject.GetComponentsInChildren<RayInteractor>(true);
        MonoBehaviour[] behaviours = rigObject.GetComponentsInChildren<MonoBehaviour>(true);

        int disabled = 0;

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            Type type = behaviour.GetType();
            string ns = type.Namespace ?? string.Empty;

            bool isLocomotion = ns.StartsWith("Oculus.Interaction.Locomotion", StringComparison.Ordinal);
            bool isHandSource = type.Name == "FromOVRHandDataSource";

            if (!isLocomotion && !isHandSource)
                continue;

            GameObject go = behaviour.gameObject;

            // 레이가 들어있는 오브젝트나 리그 루트는 끄지 않고 컴포넌트만 끈다
            if (go == rigObject || ContainsAny(go.transform, rays))
            {
                behaviour.enabled = false;
            }
            else if (go.activeSelf)
            {
                go.SetActive(false);
            }

            disabled++;
        }

        Debug.Log($"[OfficialRay] 사용하지 않는 기능 {disabled}개 비활성화 (이동 / 핸드 트래킹)");
    }

    private static bool ContainsAny(Transform parent, RayInteractor[] rays)
    {
        foreach (RayInteractor ray in rays)
        {
            if (ray != null && ray.transform.IsChildOf(parent))
                return true;
        }

        return false;
    }
}
