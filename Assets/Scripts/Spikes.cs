using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class SpikeTrap : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private string activeParameter = "Active";

    [Header("Damage")]
    public float damage = 1f;
    public float damageInterval = 2f;

    [Header("Enemy Counter")]
    public EnemyCounter enemyCounter;
    public int enemiesKilledPerTick = 1;
    public bool reduceCounterOnlyWhenEnemyKilled = true;

    [Header("Sound")]
    public AudioSource spikeSound;
    public bool playSoundLoop = true;
    public float soundFadeOutTime = 0.5f;

    private Animator anim;
    private Collider2D damageCollider;
    private bool isActive;
    private Coroutine damageCoroutine;
    private EnemyArmyUnit currentEnemy;
    private Coroutine fadeOutCoroutine;

    // Public property to check if trap is active
    public bool IsActive => isActive;
    public bool IsDamaging => damageCoroutine != null;

    void Awake()
    {
        anim = GetComponent<Animator>();
        damageCollider = GetComponent<Collider2D>();

        anim.SetBool(activeParameter, false);
        damageCollider.enabled = false;

        if (spikeSound != null)
        {
            spikeSound.loop = playSoundLoop;
            spikeSound.playOnAwake = false;
        }
    }

    public void Activate()
    {
        if (isActive) return;

        isActive = true;
        anim.SetBool(activeParameter, true);

        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = null;
        }

        if (spikeSound != null)
        {
            spikeSound.volume = 1f;
            if (!spikeSound.isPlaying)
                spikeSound.Play();
        }

        Debug.Log($"[SpikeTrap] Activated - {gameObject.name}");
    }

    public void Deactivate()
    {
        if (!isActive) return;

        isActive = false;
        anim.SetBool(activeParameter, false);

        if (soundFadeOutTime > 0 && spikeSound != null && spikeSound.isPlaying)
        {
            fadeOutCoroutine = StartCoroutine(FadeOutSound());
        }
        else
        {
            StopSound();
        }

        StopAllDamage();
        Debug.Log($"[SpikeTrap] Deactivated - {gameObject.name}");
    }

    public void EmergencyStop()
    {
        isActive = false;
        anim.SetBool(activeParameter, false);
        StopSound();
        StopAllDamage();
        Debug.Log($"[SpikeTrap] Emergency stopped - {gameObject.name}");
    }

    void PlaySound()
    {
        if (spikeSound != null && !spikeSound.isPlaying)
        {
            spikeSound.Play();
        }
    }

    void StopSound()
    {
        if (spikeSound != null && spikeSound.isPlaying)
        {
            spikeSound.Stop();
        }
    }

    IEnumerator FadeOutSound()
    {
        if (spikeSound == null) yield break;

        float startVolume = spikeSound.volume;
        float elapsed = 0f;

        while (elapsed < soundFadeOutTime)
        {
            elapsed += Time.deltaTime;
            spikeSound.volume = Mathf.Lerp(startVolume, 0f, elapsed / soundFadeOutTime);
            yield return null;
        }

        spikeSound.Stop();
        spikeSound.volume = startVolume;
        fadeOutCoroutine = null;
    }

    void ReduceEnemyCounter()
    {
        if (enemyCounter != null)
        {
            enemyCounter.RealEnemyKilled(enemiesKilledPerTick);
            Debug.Log($"[SpikeTrap] Counter reduced by {enemiesKilledPerTick}");
        }
    }

    void StopAllDamage()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
        currentEnemy = null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!damageCollider.enabled || !isActive) return;
        StartDamage(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!damageCollider.enabled || !isActive) return;

        if (damageCoroutine == null)
        {
            StartDamage(other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        StopDamage(other);
    }

    void StartDamage(Collider2D other)
    {
        if (!isActive) return;

        EnemyArmyUnit enemy = other.GetComponent<EnemyArmyUnit>();
        if (enemy && enemy.isAlive)
        {
            if (currentEnemy == enemy) return;

            StopAllDamage();

            currentEnemy = enemy;
            damageCoroutine = StartCoroutine(DamageEnemyOverTime(enemy));
        }
    }

    void StopDamage(Collider2D other)
    {
        EnemyArmyUnit enemy = other.GetComponent<EnemyArmyUnit>();
        if (enemy && enemy == currentEnemy)
        {
            StopAllDamage();
        }
    }

    IEnumerator DamageEnemyOverTime(EnemyArmyUnit enemy)
    {
        while (isActive && damageCollider.enabled && enemy != null && enemy.isAlive)
        {
            enemy.TakeDamage(Mathf.RoundToInt(damage));

            if (!enemy.isAlive && reduceCounterOnlyWhenEnemyKilled)
            {
                ReduceEnemyCounter();
            }
            else if (!reduceCounterOnlyWhenEnemyKilled)
            {
                ReduceEnemyCounter();
            }

            Debug.Log($"[SpikeTrap] Damaged enemy: {damage} damage. Enemy alive: {enemy.isAlive}");
            yield return new WaitForSeconds(damageInterval);
        }

        damageCoroutine = null;
        currentEnemy = null;
    }

    // Animation Events
    public void EnableDamage()
    {
        damageCollider.enabled = true;
        Debug.Log($"[SpikeTrap] Damage enabled - {gameObject.name}");
    }

    public void DisableDamage()
    {
        damageCollider.enabled = false;
        StopAllDamage();
        Debug.Log($"[SpikeTrap] Damage disabled - {gameObject.name}");
    }

    void OnDestroy()
    {
        StopSound();
    }

    void OnDisable()
    {
        StopSound();
    }

    public EnemyArmyUnit GetCurrentEnemy()
    {
        return currentEnemy;
    }
}