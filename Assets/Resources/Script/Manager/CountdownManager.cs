using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownManager : MonoBehaviour
{
    public static CountdownManager Instance;

    [SerializeField] private TMP_Text countdownText;

    private bool isCounting;

    private void Awake()
    {
    if (Instance == null)
    {
        Instance = this;
    }
    else
    {
        Destroy(gameObject);
        return;
    }

    if (countdownText != null)
        countdownText.gameObject.SetActive(false);
    }

    public void StartCountdown(System.Action onFinish)
    {
        if (isCounting)
            return;

        StartCoroutine(CountdownCoroutine(onFinish));
    }

    private IEnumerator CountdownCoroutine(System.Action onFinish)
    {
        isCounting = true;
        countdownText.gameObject.SetActive(true);

        countdownText.text = "3";
        yield return new WaitForSeconds(1);

        countdownText.text = "2";
        yield return new WaitForSeconds(1);

        countdownText.text = "1";
        yield return new WaitForSeconds(1);

        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);

        countdownText.gameObject.SetActive(false);

        onFinish?.Invoke();

        isCounting = false;
    }
}