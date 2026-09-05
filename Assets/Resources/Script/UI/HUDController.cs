using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private TMP_Text ringText;
    [SerializeField] private TMP_Text gameStateText;

    [Header("Breath")]
    [SerializeField] private Image breathFill;

    private void Start()
    {
        Debug.Log("=== HUDController START ===");
    }
    private void Update()
    {
        if (GameManager.Instance != null)
        {
            scoreText.text =
                $"SCORE  {GameManager.Instance.Score}";

            distanceText.text =
                $"DISTANCE  {GameManager.Instance.Distance:F1} m";

            heightText.text =
                $"HEIGHT  {GameManager.Instance.MaxHeight:F1} m";

            ringText.text =
                $"RINGS  {GameManager.Instance.RingCount}";

            gameStateText.text =
                GameManager.Instance.IsPlaying
                    ? "FLIGHT"
                    : "READY";
        }

        if (FlightInputManager.Instance != null &&
            breathFill != null)
        {
            breathFill.fillAmount =
                Mathf.Clamp01(
                    FlightInputManager.Instance.BlowInput);
        }
    }
}