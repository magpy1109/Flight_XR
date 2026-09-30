using System.Collections;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// 컨트롤러 진동.
/// - GameOver() : 게임오버 때 양손 컨트롤러에 "쿵-쿵" 진동
///
/// Meta OVRInput으로 진동을 주고, 안 되는 환경에서는 Unity XR 진동으로 한 번 더 시도한다.
/// 일시정지(Time.timeScale = 0)와 상관없이 실제 시간 기준으로 동작한다.
/// </summary>
public class ControllerHaptics : MonoBehaviour
{
    private static ControllerHaptics runner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        runner = null;
    }

    /// <summary>게임오버 진동 : 강하게 0.35초 → 잠깐 쉬고 → 약하게 0.15초</summary>
    public static void GameOver()
    {
        Run(GameOverPattern());
    }

    private static IEnumerator GameOverPattern()
    {
        yield return Pulse(1f, 0.35f);
        yield return new WaitForSecondsRealtime(0.08f);
        yield return Pulse(0.5f, 0.15f);
    }

    private static IEnumerator Pulse(float amplitude, float duration)
    {
        SetVibration(amplitude);
        SendUnityXRImpulse(amplitude, duration);

        yield return new WaitForSecondsRealtime(duration);

        SetVibration(0f);
    }

    private static void Run(IEnumerator routine)
    {
        if (runner == null)
        {
            GameObject go = new GameObject("[ControllerHaptics]");
            DontDestroyOnLoad(go);
            runner = go.AddComponent<ControllerHaptics>();
        }

        runner.StopAllCoroutines();
        runner.StartCoroutine(routine);
    }

    private static void SetVibration(float amplitude)
    {
        try
        {
            float frequency = amplitude > 0f ? 0.6f : 0f;
            OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.RTouch);
            OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.LTouch);
        }
        catch (System.Exception)
        {
            // OVR 입력을 쓸 수 없는 환경 (에디터 등)
        }
    }

    private static void SendUnityXRImpulse(float amplitude, float duration)
    {
        SendImpulse(XRNode.RightHand, amplitude, duration);
        SendImpulse(XRNode.LeftHand, amplitude, duration);
    }

    private static void SendImpulse(XRNode node, float amplitude, float duration)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return;

        if (device.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
            device.SendHapticImpulse(0, amplitude, duration);
    }

    private void OnDisable()
    {
        SetVibration(0f);
    }
}
