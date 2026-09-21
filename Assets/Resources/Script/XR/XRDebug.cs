using UnityEngine;

public class PassthroughDebug : MonoBehaviour
{
    private void Start()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();

        if (rig == null)
        {
            Debug.LogError("OVRCameraRig 없음");
            return;
        }

        var camera = rig.centerEyeAnchor.GetComponent<Camera>();
        var layers = FindObjectsByType<OVRPassthroughLayer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Debug.Log("=== PASSTHROUGH DEBUG ===");
        Debug.Log($"OVRCameraRig = {rig.name}");
        Debug.Log($"Center Camera enabled = {camera.enabled}");
        Debug.Log($"Clear Flags = {camera.clearFlags}");
        Debug.Log($"Background = {camera.backgroundColor}");
        Debug.Log($"PassthroughLayer count = {layers.Length}");

        foreach (var layer in layers)
        {
            Debug.Log(
                $"PT Layer = {layer.name} | " +
                $"active={layer.gameObject.activeInHierarchy} | " +
                $"enabled={layer.enabled}"
            );
            Debug.Log(
                $"XROrigin active count = " +
                FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                ).Length
            );

            Debug.Log(
                $"OVRCameraRig active count = " +
                FindObjectsByType<OVRCameraRig>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                ).Length
            );

            Debug.Log(
                $"OVRManager active count = " +
                FindObjectsByType<OVRManager>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                ).Length
            );
        }
    }
}