using UnityEngine;

public class Ring : MonoBehaviour
{
    private bool passed;

    private void OnTriggerEnter(Collider other)
    {
        if (passed)
            return;

        if (!other.CompareTag("Plane"))
            return;

        passed = true;

        Debug.Log("링 통과!");

        GameManager.Instance.AddScore(100);

        Destroy(gameObject);
    }
}