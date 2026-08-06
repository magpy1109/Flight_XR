using System.Collections;
using UnityEngine;

public class CountdownManager : MonoBehaviour
{
    public static CountdownManager Instance;

    private bool isCounting;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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

        Debug.Log("3");
        yield return new WaitForSeconds(1);

        Debug.Log("2");
        yield return new WaitForSeconds(1);

        Debug.Log("1");
        yield return new WaitForSeconds(1);

        Debug.Log("GO!");

        onFinish?.Invoke();

        isCounting = false;
    }
}