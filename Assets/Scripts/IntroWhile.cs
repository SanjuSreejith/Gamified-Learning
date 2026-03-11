using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraIntroManager : MonoBehaviour
{
    public CinemachineCamera introCamera;

    public float startDelay = 3f;   // delay before intro camera activates
    public float introDuration = 3f;

    void Start()
    {
        StartCoroutine(CameraSequence());
    }

    IEnumerator CameraSequence()
    {
        // Wait before switching to intro camera
        yield return new WaitForSeconds(startDelay);

        // Intro camera active
        introCamera.Priority = 41;

        // Wait for intro duration
        yield return new WaitForSeconds(introDuration);

        // Return control back
        introCamera.Priority = 0;
    }
}