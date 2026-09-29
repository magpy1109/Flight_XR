using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownManager : MonoBehaviour
{
    public static CountdownManager Instance;

    [Header("Countdown UI")]
    [SerializeField] private string gameCanvasName = "GameCanvas";
    [SerializeField] private string countdownTextName = "CountdownText";

    private TMP_Text countdownText;
    private bool isCounting;

    private Coroutine countdownRoutine;

    public bool IsCounting => isCounting;

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

        // 현재 살아있는 GameCanvas의 CountdownText 찾기
        FindCountdownText();

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void FindCountdownText()
    {
        countdownText = null;

        // 비활성 오브젝트까지 포함해서 모든 TMP_Text 검색
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (TMP_Text text in texts)
        {
            if (text.gameObject.name != countdownTextName)
                continue;

            // GameCanvas 아래에 있는 CountdownText인지 확인
            Transform current = text.transform;

            while (current != null)
            {
                if (current.name == gameCanvasName)
                {
                    countdownText = text;

                    Debug.Log(
                        "CountdownText 찾음 : " +
                        GetHierarchyPath(text.transform)
                    );

                    return;
                }

                current = current.parent;
            }
        }

        Debug.LogError(
            "현재 GameCanvas에서 CountdownText를 찾지 못했습니다."
        );
    }

    public void StartCountdown(Action onFinish)
    {
        if (isCounting)
            return;

        if (countdownText == null)
            FindCountdownText();

        if (countdownText == null)
        {
            Debug.LogError("Countdown 시작 실패 : CountdownText가 없습니다.");
            return;
        }

        countdownRoutine = StartCoroutine(CountdownCoroutine(onFinish));
    }

    public void CancelCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        isCounting = false;

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    private IEnumerator CountdownCoroutine(Action onFinish)
    {
        isCounting = true;

        countdownText.gameObject.SetActive(true);

        countdownText.text = "3";
        yield return new WaitForSeconds(1f);

        countdownText.text = "2";
        yield return new WaitForSeconds(1f);

        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);

        countdownText.gameObject.SetActive(false);

        onFinish?.Invoke();

        isCounting = false;
        countdownRoutine = null;
    }

    private string GetHierarchyPath(Transform target)
    {
        string path = target.name;

        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }
}