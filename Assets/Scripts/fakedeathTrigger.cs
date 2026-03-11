using UnityEngine;
using System.Collections;

public class FakeDeathTrigger : MonoBehaviour
{
    /* ================= REFERENCES ================= */
    [Header("References")]
    [SerializeField] private EnemyCounter enemyCounter;

    /* ================= FAKE DEATH AMOUNT ================= */
    [Header("Fake Death Amount")]
    [SerializeField] private int fakeDeathAmount = 5;
    [SerializeField] private float delayBetweenEachFakeDeath = 0.5f;

    /* ================= HOLD R ================= */
    [Header("Hold R Settings")]
    [SerializeField] private bool enableHoldR = true;
    [SerializeField] private float holdTimeRequired = 2f;
    [SerializeField] private bool holdRKillOnlyOnce = false;

    /* ================= STAY INSIDE TIMER ================= */
    [Header("Stay Inside Settings")]
    [SerializeField] private bool enableStayInsideKill = false;
    [SerializeField] private float stayInsideDuration = 5f;

    /* ================= AUTO INTERVAL ================= */
    [Header("Auto Interval Settings")]
    [SerializeField] private bool enableAutoFakeDeath = false;
    [SerializeField] private float autoFakeDeathInterval = 4f;

    /* ================= LIMIT ================= */
    [Header("Limits")]
    [SerializeField] private int maxFakeDeathsInThisArea = 20;

    /* ================= STATE ================= */
    private bool playerInside = false;
    private bool holdUsed = false;

    private int totalFakeDeathsUsed = 0;

    private Coroutine holdRoutine;
    private Coroutine stayRoutine;
    private Coroutine autoRoutine;
    private Coroutine fakeDeathRoutine;

    /* ================= FAKE DEATH ================= */
    void TriggerFakeDeath()
    {
        if (enemyCounter == null)
            return;

        if (totalFakeDeathsUsed >= maxFakeDeathsInThisArea)
            return;

        if (fakeDeathRoutine == null)
            fakeDeathRoutine = StartCoroutine(FakeDeathRoutine());
    }

    IEnumerator FakeDeathRoutine()
    {
        int remaining = fakeDeathAmount;

        while (remaining > 0 && playerInside && totalFakeDeathsUsed < maxFakeDeathsInThisArea)
        {
            enemyCounter.FakeEnemyKilled(1);

            totalFakeDeathsUsed++;
            remaining--;

            yield return new WaitForSeconds(delayBetweenEachFakeDeath);
        }

        fakeDeathRoutine = null;
    }

    /* ================= HOLD R ================= */
    IEnumerator HoldRoutine()
    {
        float timer = 0f;

        while (playerInside && (!holdUsed || !holdRKillOnlyOnce))
        {
            if (Input.GetKey(KeyCode.R))
            {
                timer += Time.deltaTime;

                if (timer >= holdTimeRequired)
                {
                    TriggerFakeDeath();

                    if (holdRKillOnlyOnce)
                        holdUsed = true;

                    timer = 0f;
                }
            }
            else
            {
                timer = 0f;
            }

            yield return null;
        }
    }

    /* ================= STAY INSIDE ================= */
    IEnumerator StayRoutine()
    {
        float timer = 0f;

        while (playerInside && timer < stayInsideDuration)
        {
            timer += Time.deltaTime;

            TriggerFakeDeath();

            yield return new WaitForSeconds(delayBetweenEachFakeDeath);
        }
    }

    /* ================= AUTO INTERVAL ================= */
    IEnumerator AutoRoutine()
    {
        while (playerInside)
        {
            yield return new WaitForSeconds(autoFakeDeathInterval);
            TriggerFakeDeath();
        }
    }

    /* ================= TRIGGER ================= */
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;

        if (enableHoldR)
            holdRoutine = StartCoroutine(HoldRoutine());

        if (enableStayInsideKill)
            stayRoutine = StartCoroutine(StayRoutine());

        if (enableAutoFakeDeath)
            autoRoutine = StartCoroutine(AutoRoutine());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        if (holdRoutine != null) StopCoroutine(holdRoutine);
        if (stayRoutine != null) StopCoroutine(stayRoutine);
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        if (fakeDeathRoutine != null) StopCoroutine(fakeDeathRoutine);

        holdRoutine = null;
        stayRoutine = null;
        autoRoutine = null;
        fakeDeathRoutine = null;
    }
}