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

    [Header("Sound Modulation")]
    public bool modulateSoundWithSpeed = true;
    public float minPitch = 0.8f;
    public float maxPitch = 1.5f;
    public float minVolume = 0.3f;
    public float maxVolume = 1f;
    public AnimationCurve speedToPitchCurve = AnimationCurve.Linear(0, 0.8f, 1, 1.5f);
    public AnimationCurve speedToVolumeCurve = AnimationCurve.Linear(0, 0.3f, 1, 1f);

    private Rigidbody2D rb;
    private PlayerJetpackAnimator2D animator;
    private SpriteRenderer sr;

    private float originalGravity;
    private bool isFlying;
    private int currentPoint = 0;

    public Action<bool> OnFlightEnd;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<PlayerJetpackAnimator2D>();
        sr = GetComponent<SpriteRenderer>();

        originalGravity = rb.gravityScale;

        if (controlMode == ControlMode.Manual && jetOnAwake)
            StartManualFly();
    }

    void Update()
    {
        if (controlMode == ControlMode.Manual && isFlying)
        {
            HandleManualFlight();

            if (modulateSoundWithSpeed)
                ModulateSoundBySpeed(rb.linearVelocity.magnitude / manualFlySpeed);
        }
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

        // Update animator
        if (animator != null)
            animator.UpdateXSpeed(rb.linearVelocity.x);

        // Flip sprite
        HandleFlip(rb.linearVelocity.x);
    }

    // ================= AUTO =================

    public void FlyToNextPoint(float travelPercent)
    {
        if (controlMode != ControlMode.Auto) return;
        if (isFlying) return;
        if (landingPoints == null || landingPoints.Length == 0) return;
        if (currentPoint >= landingPoints.Length) return;

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
        if (totalDistance < 0.001f) totalDistance = 0.001f;

        while (t < travelPercent)
        {
            t += (Time.deltaTime * autoFlySpeed) / totalDistance;
            t = Mathf.Min(t, travelPercent);

            Vector2 pos = Vector2.Lerp(start, end, t);
            rb.MovePosition(pos);

            // Flip during auto movement
            HandleFlip(end.x - transform.position.x);

            if (modulateSoundWithSpeed)
            {
                float speedFactor = autoFlySpeed / manualFlySpeed;
                ModulateSoundBySpeed(Mathf.Clamp01(speedFactor));
            }

            yield return null;
        }

        currentPoint++;

        rb.gravityScale = originalGravity;

        if (animator != null)
            animator.StopFlying();

        StopJetpackSound();

        isFlying = false;
        OnFlightEnd?.Invoke(true);
    }

    // ================= FLIP =================

    void HandleFlip(float xSpeed)
    {
        if (sr == null) return;

        if (xSpeed > 0.05f)
        {
            sr.flipX = false;
            FollowingCamera.Instance?.SetFacingDirection(true);
        }
        else if (xSpeed < -0.05f)
        {
            sr.flipX = true;
            FollowingCamera.Instance?.SetFacingDirection(false);
        }
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

    private void ModulateSoundBySpeed(float speedFactor)
    {
        if (!jetpackAudio) return;

        if (speedToPitchCurve != null && speedToPitchCurve.keys.Length > 0)
            jetpackAudio.pitch = speedToPitchCurve.Evaluate(speedFactor);
        else
            jetpackAudio.pitch = Mathf.Lerp(minPitch, maxPitch, speedFactor);

        if (speedToVolumeCurve != null && speedToVolumeCurve.keys.Length > 0)
            jetpackAudio.volume = speedToVolumeCurve.Evaluate(speedFactor);
        else
            jetpackAudio.volume = Mathf.Lerp(minVolume, maxVolume, speedFactor);
    }

    // ================= LEGACY SUPPORT =================

    public void Equip()
    {
        currentPoint = 0;
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