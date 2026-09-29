using System;
using System.Collections.Generic;
using System.Reflection;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

/// <summary>
/// World Space Canvas 하나를 메타 공식 레이로 누를 수 있게 해주는 연결 오브젝트.
/// (OfficialRayRunner가 자동으로 만든다)
///
/// 구성 : BoxCollider + ColliderSurface + PointableCanvas + RayInteractable
/// - Canvas 위치/회전/크기를 매 프레임 따라간다. (Canvas 위치가 바뀌어도 동작)
/// - Canvas가 꺼져 있거나 누를 수 있는 UI(버튼, 슬라이더 등)가 하나도 없으면 레이 판정을 끈다.
///   (일시정지 메뉴가 꺼져 있을 때, 게임 중 HUD만 보일 때 레이가 걸리지 않게)
/// - Canvas 자체는 수정하지 않는다. (GraphicRaycaster가 없을 때만 추가)
/// </summary>
public class IsdkCanvasProxy : MonoBehaviour
{
    private Canvas target;
    private RectTransform targetRect;
    private BoxCollider box;
    private RayInteractable interactable;
    private float padding;
    private bool lastVisible = true;

    // 누를 수 있는 UI가 있는지 주기적으로 확인
    private const float InteractiveCheckInterval = 0.25f;
    private float nextInteractiveCheck;
    private bool hasInteractive = true;
    private readonly List<Selectable> selectableBuffer = new List<Selectable>();

    public Canvas Target => target;

    public static IsdkCanvasProxy Create(Canvas canvas, Scene scene, float padding)
    {
        if (canvas == null)
            return null;

        // PointableCanvas는 GraphicRaycaster가 필요 (GameCanvas에는 없음)
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        // 설정을 마치기 전에 Awake가 돌지 않도록 비활성 상태로 구성
        GameObject go = new GameObject("[OfficialRay] " + canvas.name);
        go.SetActive(false);

        if (scene.IsValid() && scene.isLoaded && go.scene != scene)
            SceneManager.MoveGameObjectToScene(go, scene);

        go.layer = 2; // Ignore Raycast : 게임의 물리 레이캐스트에 걸리지 않게

        BoxCollider collider = go.AddComponent<BoxCollider>();
        collider.isTrigger = true; // 비행기 등과 물리 충돌하지 않게

        ColliderSurface surface = go.AddComponent<ColliderSurface>();
        surface.InjectAllColliderSurface(collider);

        PointableCanvas pointableCanvas = go.AddComponent<PointableCanvas>();
        pointableCanvas.InjectAllPointableCanvas(canvas);

        RayInteractable rayInteractable = go.AddComponent<RayInteractable>();
        SetPrivateField(rayInteractable, "_surface", surface);
        SetPrivateField(rayInteractable, "_pointableElement", pointableCanvas);

        IsdkCanvasProxy proxy = go.AddComponent<IsdkCanvasProxy>();
        proxy.target = canvas;
        proxy.targetRect = canvas.transform as RectTransform;
        proxy.box = collider;
        proxy.interactable = rayInteractable;
        proxy.padding = padding;
        proxy.Follow();

        go.SetActive(true);
        return proxy;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Follow();
    }

    private void Follow()
    {
        if (Time.unscaledTime >= nextInteractiveCheck)
        {
            nextInteractiveCheck = Time.unscaledTime + InteractiveCheckInterval;
            hasInteractive = HasInteractiveUI();
        }

        bool visible = target.isActiveAndEnabled && hasInteractive;

        bool changed = false;

        // 위치를 먼저 맞춘 다음 판정을 켠다
        if (visible && targetRect != null)
            changed = UpdatePose();

        if (visible != lastVisible)
        {
            lastVisible = visible;
            box.enabled = visible;
            changed = true;

            if (interactable != null)
                interactable.enabled = visible;
        }

        // 일시정지 중(Time.timeScale = 0)에는 물리 갱신이 멈춰서, 옮긴 판정 위치가
        // 물리 엔진에 반영되지 않는다. (첫 일시정지 때 레이가 메뉴에 안 맞던 원인)
        // → 바뀐 경우 직접 반영
        if (changed && visible && Time.timeScale <= 0f)
            Physics.SyncTransforms();
    }

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;
    private Vector3 lastSize;
    private Vector2 lastCenter;

    /// <summary>Canvas 위치 / 크기에 판정 영역을 맞춘다. 바뀌었으면 true</summary>
    private bool UpdatePose()
    {
        Transform t = target.transform;
        Vector3 position = t.position;
        Quaternion rotation = t.rotation;
        Vector3 scale = t.lossyScale;

        // Canvas 영역 + 여유 영역 (드롭다운 목록이 Canvas 밖으로 나와도 누를 수 있게)
        Rect rect = targetRect.rect;
        float padX = rect.width * padding;
        float padY = rect.height * padding;
        Vector3 size = new Vector3(rect.width + padX * 2f, rect.height + padY * 2f, 1f);

        if (position == lastPosition && rotation == lastRotation && scale == lastScale && size == lastSize && rect.center == lastCenter)
            return false;

        lastPosition = position;
        lastRotation = rotation;
        lastScale = scale;
        lastSize = size;
        lastCenter = rect.center;

        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = scale;

        box.center = new Vector3(rect.center.x, rect.center.y, 0f);
        box.size = size;
        return true;
    }

    /// <summary>Canvas 안에 지금 누를 수 있는 UI가 있는지</summary>
    private bool HasInteractiveUI()
    {
        if (!target.isActiveAndEnabled)
            return false;

        target.GetComponentsInChildren(false, selectableBuffer);

        foreach (Selectable selectable in selectableBuffer)
        {
            if (selectable != null && selectable.isActiveAndEnabled && selectable.IsInteractable())
                return true;
        }

        return false;
    }

    /// <summary>Inspector에서 연결하는 것과 같은 효과 (Awake 전에 호출해야 함)</summary>
    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        Type type = obj.GetType();

        while (type != null)
        {
            FieldInfo field = type.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

            if (field != null)
            {
                field.SetValue(obj, value);
                return;
            }

            type = type.BaseType;
        }

        Debug.LogError($"[OfficialRay] {obj.GetType().Name}.{fieldName} 필드를 찾지 못했습니다.");
    }
}
