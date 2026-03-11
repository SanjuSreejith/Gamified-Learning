using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class JetpackController2D : MonoBehaviour
{
    public enum ControlMode
    {
        Manual,
        Auto
    }

    [Header("Control Mode")]
    public ControlMode controlMode = ControlMode.Manual;
    public bool jetOnAwake = false;

    [Header("Manual Flight")]
    public float manualFlySpeed = 6f;
    public float manualAcceleration = 8f;

    [Header("Auto Flight")]
    public Transform[] landingPoints;
    public float autoFlySpeed = 6f;

    [Header("Audio")]
    public AudioSource jetpackAudio;
    public AudioClip jetpackLoopClip;

    private Rigidbody2D rb;
    private PlayerJetpackAnimator2D animator;

    private float originalGravity;
    private bool isFlying;
    private int currentPoint = 0;               // FIX: start at 0 (first landing point)

    public Action<bool> OnFlightEnd;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<PlayerJetpackAnimator2D>();

        originalGravity = rb.gravityScale;

        if (controlMode == ControlMode.Manual && jetOnAwake)
            StartManualFly();
    }

    void Update()
    {
        if (controlMode == ControlMode.Manual && isFlying)
            HandleManualFlight();
    }

    public bool IsFlying() => isFlying;

    // ================= MANUAL =================

    public void StartManualFly()
    {
        if (isFlying) return;

        isFlying = true;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        StartJetpackSound();

        if (animator != null)
        {
            animator.SetJetpack(true);
            animator.StartFlying();
        }
    }

    public void StopManualFly()
    {
        if (!isFlying) return;

        isFlying = false;

        rb.gravityScale = originalGravity;

        StopJetpackSound();

        if (animator != null)
        {
            animator.StopFlying();
            animator.SetJetpack(false);
        }
    }

    void HandleManualFlight()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = 0f;

        if (Input.GetKey(KeyCode.W))
            inputY = 1f;
        else if (Input.GetKey(KeyCode.S))
            inputY = -1f;

        Vector2 targetVelocity = new Vector2(inputX, inputY) * manualFlySpeed;

        rb.linearVelocity = Vector2.Lerp(
            rb.linearVelocity,
            targetVelocity,
            manualAcceleration * Time.deltaTime
        );

        if (animator != null)
            animator.UpdateXSpeed(rb.linearVelocity.x);
    }

    // ================= AUTO =================

    public void FlyToNextPoint(float travelPercent)
    {
        if (controlMode != ControlMode.Auto) return;
        if (isFlying) return;
        if (landingPoints == null || landingPoints.Length == 0) return;
        if (currentPoint >= landingPoints.Length) return;   // no more points

        StartCoroutine(FlyRoutine(travelPercent));
    }

    private IEnumerator FlyRoutine(float travelPercent)
    {
        isFlying = true;
        rb.gravityScale = 0f;

        StartJetpackSound();

        if (animator != null)
            animator.StartFlying();

        Vector2 start = transform.position;
        Vector2 end = landingPoints[currentPoint].position;

        float t = 0f;
        float totalDistance = Vector2.Distance(start, end);
        // Prevent division by zero if points are identical
        if (totalDistance < 0.001f) totalDistance = 0.001f;

        while (t < travelPercent)
        {
            t += (Time.deltaTime * autoFlySpeed) / totalDistance;
            t = Mathf.Min(t, travelPercent);

            Vector2 pos = Vector2.Lerp(start, end, t);
            rb.MovePosition(pos);

            yield return null;
        }

        // FIX: advance to next landing point for future flights
        currentPoint++;

        rb.gravityScale = originalGravity;

        if (animator != null)
            animator.StopFlying();

        StopJetpackSound();

        isFlying = false;
        OnFlightEnd?.Invoke(true);
    }

    // ================= SOUND =================

    private void StartJetpackSound()
    {
        if (!jetpackAudio || !jetpackLoopClip) return;

        if (!jetpackAudio.isPlaying)
        {
            jetpackAudio.clip = jetpackLoopClip;
            jetpackAudio.Play();
        }
    }

    private void StopJetpackSound()
    {
        if (jetpackAudio && jetpackAudio.isPlaying)
            jetpackAudio.Stop();
    }

    // ================= LEGACY SUPPORT =================

    // Reset jetpack state (used by lesson system)
    public void Equip()
    {
        currentPoint = 0;           // FIX: reset to first point
        isFlying = false;

        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero;

        StopJetpackSound();

        if (animator != null)
        {
            animator.StopFlying();
            animator.SetJetpack(false);
        }
    }

    // Force fall (used when energy ends or fail state)
    public void FailFall()
    {
        StopAllCoroutines();

        isFlying = false;

        rb.gravityScale = originalGravity;

        StopJetpackSound();

        if (animator != null)
        {
            animator.StopFlying();
            animator.SetJetpack(false);
        }

        OnFlightEnd?.Invoke(false);
    }
}