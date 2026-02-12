
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BridgeBreakController2D : MonoBehaviour
{
    [Header("Bridge Parts")]
    public Rigidbody2D[] bridgePlanks;

    [Header("Break Effects")]
    public ParticleSystem breakParticles;
    public Transform particleSpawnPoint;

    [Header("Runtime State")]
    public int currentPeopleCount;
    public bool isBroken;

    /* ================= CINEMATIC CAMERA ================= */

    [Header("Cinematic Camera")]
    public CinemachineCamera bridgeCinematicCam; // UNIQUE per bridge
    public CinemachineCamera playerCam;           // common player cam
    public float cinematicDuration = 2.5f;

    /* ================= INTERNAL ================= */

    bool armed;
    int conditionLimit;
    bool cinematicPlaying;
    [Header("Audio")]
    public AudioSource bridgeAudio;
    public AudioClip bridgeBreakClip;


    /* ================= INIT ================= */

    void Start()
    {
        foreach (var rb in bridgePlanks)
        {
            if (rb == null) continue;

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Ensure correct default camera
        if (bridgeCinematicCam)
            bridgeCinematicCam.Priority = 1;

        if (playerCam)
            playerCam.Priority = 20;
    }

    /* ================= PEOPLE COUNT ================= */

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidEntity(other)) return;

        currentPeopleCount++;

        if (armed && !isBroken && currentPeopleCount > conditionLimit)
            BreakBridge();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsValidEntity(other)) return;
        currentPeopleCount = Mathf.Max(0, currentPeopleCount - 1);
    }

    bool IsValidEntity(Collider2D other)
    {
        return other.CompareTag("Player")
            || other.CompareTag("NPC")
            || other.CompareTag("Enemy");
    }

    /* ================= CONDITION ================= */

    public void EvaluateCondition(int limit)
    {
        if (isBroken) return;

        conditionLimit = limit;
        armed = true;

        Debug.Log($"[Bridge] CONDITION ARMED → people_count > {conditionLimit}");
    }

    /* ================= BREAK ================= */

    void BreakBridge()
    {
        if (isBroken) return;
        isBroken = true;

        Debug.Log("💥 Bridge breaking!");

        if (bridgeAudio && bridgeBreakClip)
            bridgeAudio.PlayOneShot(bridgeBreakClip);

        if (breakParticles)
        {
            breakParticles.transform.position =
                particleSpawnPoint ? particleSpawnPoint.position : transform.position;
            breakParticles.Play();
        }

        if (!cinematicPlaying && bridgeCinematicCam && playerCam)
            StartCoroutine(BridgeBreakSequence());
        else
            ReleaseBridge();
    }
    System.Collections.IEnumerator BridgeBreakSequence()
    {
        cinematicPlaying = true;

        // 1️⃣ FREEZE TIME
        Time.timeScale = 0f;

        // 2️⃣ SWITCH CAMERA
        bridgeCinematicCam.Priority = 40;
        playerCam.Priority = 10;

        // 3️⃣ WAIT UNTIL CAMERA FINISHES BLENDING (REAL TIME)
        CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();

        while (brain != null && brain.ActiveBlend != null)
        {
            yield return null; // waits in real-time because timescale = 0
        }

        // 4️⃣ RESUME TIME
        Time.timeScale = 1f;

        // 5️⃣ RELEASE BRIDGE
        ReleaseBridge();

        // 6️⃣ LET CINEMATIC PLAY
        yield return new WaitForSeconds(cinematicDuration);

        // 7️⃣ RETURN CAMERA
        bridgeCinematicCam.Priority = 1;
        playerCam.Priority = 30;

        cinematicPlaying = false;
    }


    /* ================= CINEMATIC ================= */

    System.Collections.IEnumerator BridgeCinematic()
    {
        cinematicPlaying = true;

        bridgeCinematicCam.Priority = 30;
        playerCam.Priority = 10;

        yield return new WaitForSeconds(cinematicDuration);

        bridgeCinematicCam.Priority = 1;
        playerCam.Priority = 30;

        cinematicPlaying = false;
    }
    void ReleaseBridge()
    {
        foreach (var rb in bridgePlanks)
        {
            if (!rb) continue;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 2f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            rb.AddForce(Random.insideUnitCircle * 2f, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-15f, 15f), ForceMode2D.Impulse);
        }
    }


#if UNITY_EDITOR
    void OnGUI()
    {
        GUI.Label(
            new Rect(10, 10, 350, 20),
            $"Bridge | People: {currentPeopleCount} | Limit: {conditionLimit} | Armed: {armed}"
        );
    }
#endif
}
