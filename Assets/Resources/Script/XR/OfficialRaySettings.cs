using Oculus.Interaction.Input;
using UnityEngine;

/// <summary>
/// 메타 공식 레이(Interaction SDK) 설정.
/// Assets/Resources/OfficialRaySettings.asset 에 저장되어 있고, OfficialRay가 실행 시 읽어간다.
/// </summary>
[CreateAssetMenu(menuName = "Flight XR/Official Ray Settings", fileName = "OfficialRaySettings")]
public class OfficialRaySettings : ScriptableObject
{
    [Tooltip("공식 레이 사용 여부. 끄면 기존 QuestUIRayInteractor 레이를 사용합니다.")]
    public bool useOfficialRay = true;

    [Tooltip("Meta Interaction SDK 기본 리그 프리팹 (OVRComprehensiveInteractionRig)")]
    public OVRCameraRigRef interactionRigPrefab;

    [Tooltip("Canvas 바깥쪽 여유 판정 영역 (드롭다운 목록이 Canvas 밖으로 나와도 누를 수 있도록)")]
    [Range(0f, 0.5f)]
    public float canvasHitPadding = 0.15f;
}
