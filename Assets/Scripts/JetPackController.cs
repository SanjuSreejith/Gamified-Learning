using UnityEngine;
using System;
using System.Collections;

public class JetpackController2D : MonoBehaviour
{
    public Transform[] landingPoints;
    public float flySpeed = 6f;

    int currentPoint = 1;        // player starts at point[0]
    bool isFlying;
    float lockedY;

    PlayerJetpackAnimator2D animator;

    public Action<bool> OnFlightEnd;
    // true  = landed on checkpoint
    // false = stopped mid-air (out of energy)

    void Awake()
    {
        animator = GetComponent<PlayerJetpackAnimator2D>();
    }

    public void Equip()
    {
        currentPoint = 1;
        isFlying = false;
    }

    public bool IsFlying() => isFlying;

    public void FlyToNextPoint(float travelPercent)
    {
        if (isFlying) return;
        if (landingPoints == null || currentPoint >= landingPoints.Length) return;

        StartCoroutine(FlyRoutine(travelPercent));
    }
    public void FailFall()
    {
        StopAllCoroutines();

        isFlying = false;

        if (animator != null)
            animator.ResetMovement();

        // Notify lesson that flight failed
        OnFlightEnd?.Invoke(false);
    }


    IEnumerator FlyRoutine(float travelPercent)
    {
        isFlying = true;

        // ▶ Fly animation ON
        if (animator != null)
            animator.PlayFly();

        Vector2 start = transform.position;
        Vector2 end = landingPoints[currentPoint].position;
        lockedY = start.y;

        float t = 0f;
        float totalDistance = Vector2.Distance(start, end);

        while (t < travelPercent)
        {
            t += (Time.deltaTime * flySpeed) / totalDistance;
            t = Mathf.Min(t, travelPercent);

            Vector2 pos = Vector2.Lerp(start, end, t);
            transform.position = new Vector2(pos.x, lockedY);

            if (animator != null)
                animator.UpdateXSpeed(end.x - transform.position.x);

            yield return null;
        }

        bool success = Mathf.Approximately(travelPercent, 1f);

        if (success)
            currentPoint++; // move to next checkpoint

        // ⏹ Fly animation OFF
        if (animator != null)
            animator.ResetMovement();

        isFlying = false;
        OnFlightEnd?.Invoke(success);
    }

    public bool IsLastPointReached()
    {
        return currentPoint >= landingPoints.Length;
    }
}
