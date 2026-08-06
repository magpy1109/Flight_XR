using UnityEngine;
using UnityEngine.InputSystem;

public class RingSpawner : MonoBehaviour
{
    [SerializeField] private GameObject ringPrefab;
    [SerializeField] private Transform spawnPoint;

    private GameObject currentRing;

    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            SpawnRing();
        }
    }

    public void SpawnRing()
    {
        if (currentRing != null)
            Destroy(currentRing);

        currentRing = Instantiate(
            ringPrefab,
            spawnPoint.position,
            Quaternion.identity);
    }
}