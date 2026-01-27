
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class EnableObjectAndCameraTrigger2D : MonoBehaviour
{
    [Header("Target")]
    public GameObject objectToEnable;

    [Header("Cinemachine")]
    public CinemachineVirtualCamera virtualCamera;
    public int activePriority = 30;
    public int normalPriority = 0;
    public float cameraHoldTime = 3f;

    [Header("Settings")]
    public bool triggerOnce = true;

    bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered && triggerOnce) return;

        if (other.CompareTag("Player"))
        {
            if (objectToEnable != null)
                objectToEnable.SetActive(true);

            if (virtualCamera != null)
                StartCoroutine(CameraPriorityRoutine());

            triggered = true;
        }
    }

    IEnumerator CameraPriorityRoutine()
    {
        virtualCamera.Priority = activePriority;

        yield return new WaitForSeconds(cameraHoldTime);

        virtualCamera.Priority = normalPriority;
    }
}
