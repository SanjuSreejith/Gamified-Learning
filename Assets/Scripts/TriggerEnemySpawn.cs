using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Collider2D))]
public class EnableObjectAndCameraTrigger2D : MonoBehaviour
{
    [Header("Target")]
    public GameObject objectToEnable;

    [Header("Cinemachine (Unity 6)")]
    public CinemachineCamera cinematicCamera;

    public int activePriority = 30;
    public int normalPriority = 0;
    public float cameraHoldTime = 3f;

    [Header("Settings")]
    public bool triggerOnce = true;

    bool triggered;
    Coroutine camRoutine;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered && triggerOnce) return;
        if (!other.CompareTag("Player")) return;

        // Enable object
        if (objectToEnable != null)
            objectToEnable.SetActive(true);

        // Camera priority switch
        if (cinematicCamera != null)
        {
            if (camRoutine != null)
                StopCoroutine(camRoutine);

            camRoutine = StartCoroutine(CameraPriorityRoutine());
        }

        triggered = true;
    }

    IEnumerator CameraPriorityRoutine()
    {
        cinematicCamera.Priority = activePriority;

        yield return new WaitForSeconds(cameraHoldTime);

        cinematicCamera.Priority = normalPriority;
        camRoutine = null;
    }
}
