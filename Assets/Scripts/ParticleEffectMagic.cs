using UnityEngine;
using System.Collections;

public class MagicParticleEffect : MonoBehaviour
{
    [Header("Magic Effect Settings")]
    public float startSpeed = 15f;
    public float endSpeed = 2f;
    public float effectDuration = 1.5f;
    public float fadeOutDuration = 0.5f;

    [Header("Color Settings")]
    public Color startColor = Color.cyan;
    public Color endColor = Color.white;
    public Gradient colorOverLifetime;

    [Header("Size Settings")]
    public float startSize = 0.5f;
    public float endSize = 1.2f;

    [Header("Emission Settings")]
    public int burstCount = 50;
    public float emissionRate = 100f;

    [Header("Movement Settings")]
    public float turbulenceStrength = 1f;
    public float swirlStrength = 2f;
    public float upwardForce = 1f;

    [Header("Visibility")]
    public bool startHidden = true;
    public float appearDuration = 0.5f;
    public float disappearDuration = 0.3f;

    ParticleSystem particles;
    ParticleSystem.MainModule mainModule;
    ParticleSystem.EmissionModule emissionModule;
    ParticleSystem.VelocityOverLifetimeModule velocityModule;
    ParticleSystem.ColorOverLifetimeModule colorModule;
    ParticleSystem.SizeOverLifetimeModule sizeModule;
    ParticleSystemRenderer particleRenderer;

    bool isPlaying;
    float playTime;
    bool isVisible;

    void Awake()
    {
        particles = GetComponent<ParticleSystem>();
        if (!particles)
        {
            Debug.LogError("MagicParticleEffect requires ParticleSystem!");
            enabled = false;
            return;
        }

        mainModule = particles.main;
        emissionModule = particles.emission;
        velocityModule = particles.velocityOverLifetime;
        colorModule = particles.colorOverLifetime;
        sizeModule = particles.sizeOverLifetime;
        particleRenderer = particles.GetComponent<ParticleSystemRenderer>();

        SetupParticleSystem();
    }

    void Start()
    {
        if (startHidden)
            HideImmediate();
    }

    void SetupParticleSystem()
    {
        mainModule.playOnAwake = false;
        mainModule.startSpeed = startSpeed;
        mainModule.startSize = startSize;
        mainModule.startColor = startColor;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        mainModule.maxParticles = 1000;

        emissionModule.rateOverTime = 0f;
        emissionModule.enabled = true;

        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.Local;

        colorModule.enabled = true;
        colorModule.color = colorOverLifetime;

        sizeModule.enabled = true;
        sizeModule.size = new ParticleSystem.MinMaxCurve(
            endSize,
            new AnimationCurve(
                new Keyframe(0, startSize),
                new Keyframe(0.3f, endSize),
                new Keyframe(1, startSize * 0.5f)
            )
        );

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // ================= PLAY =================
    public void PlayMagicEffect()
    {
        StopAllCoroutines();

        if (!isVisible)
            ShowGradually();

        emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(emissionRate);
        particles.Emit(burstCount);
        particles.Play();

        isPlaying = true;
        playTime = 0f;

        StartCoroutine(FadeOutEffect());
    }

    IEnumerator FadeOutEffect()
    {
        yield return new WaitForSeconds(effectDuration);

        float timer = 0f;
        float startRate = emissionModule.rateOverTime.constant;
        float startSpeedValue = mainModule.startSpeed.constant;

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeOutDuration;

            emissionModule.rateOverTime =
                new ParticleSystem.MinMaxCurve(Mathf.Lerp(startRate, 0f, t));

            mainModule.startSpeed =
                new ParticleSystem.MinMaxCurve(Mathf.Lerp(startSpeedValue, endSpeed, t));

            yield return null;
        }

        emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
        particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        isPlaying = false;
        HideGradually(); // ✅ correct call
    }

    void Update()
    {
        if (!isPlaying) return;

        playTime += Time.deltaTime;
        UpdateMagicMovement();
        UpdateColorPulse();
    }

    void UpdateMagicMovement()
    {
        float swirl = playTime * swirlStrength;
        velocityModule.x = new ParticleSystem.MinMaxCurve(Mathf.Sin(swirl) * turbulenceStrength);
        velocityModule.y = new ParticleSystem.MinMaxCurve(upwardForce + Mathf.Cos(swirl));
    }

    void UpdateColorPulse()
    {
        float pulse = Mathf.Sin(playTime * 3f) * 0.2f + 0.8f;
        Color c = Color.Lerp(startColor, endColor, playTime / effectDuration) * pulse;
        mainModule.startColor = c;
    }

    // ================= VISIBILITY =================
    public void ShowGradually()
    {
        StopAllCoroutines();
        StartCoroutine(ShowEffectRoutine());
    }

    public void HideGradually()
    {
        StopAllCoroutines();
        StartCoroutine(HideEffectRoutine());
    }

    public void HideImmediate()
    {
        if (particleRenderer) particleRenderer.enabled = false;
        isVisible = false;
    }

    IEnumerator ShowEffectRoutine()
    {
        particleRenderer.enabled = true;
        isVisible = true;
        yield return null;
    }

    IEnumerator HideEffectRoutine()
    {
        yield return new WaitForSeconds(disappearDuration);
        if (particleRenderer) particleRenderer.enabled = false;
        isVisible = false;
    }

    public void StopEffect()
    {
        StopAllCoroutines();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        isPlaying = false;
    }
}
