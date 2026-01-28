
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BridgeBreak2Controller2D : MonoBehaviour
{
    /* ================= BRIDGE PARTS ================= */

    [Header("Bridge Parts")]
    public Rigidbody2D[] bridgePlanks;

    /* ================= EFFECTS ================= */

    [Header("Break Effects")]
    public ParticleSystem breakParticles;
    public Transform particleSpawnPoint;

    /* ================= CINEMATIC ================= */

    [Header("Cinematic Camera")]
    public CinemachineCamera bridgeCinematicCam; // unique per bridge
    public CinemachineCamera playerCam;           // shared player cam
    public float cinematicDuration = 2.5f;

    /* ================= RUNTIME ================= */

    [Header("Runtime")]
    public int peopleCount;
    public bool isBroken;

    // 🔑 STORED CONDITION
    string storedOperator;
    int storedValue;
    bool conditionArmed;

    Rigidbody2D rb;
    Collider2D col;

    bool cinematicPlaying;

    /* ================= INIT ================= */

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        col.isTrigger = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        LockBridge();

        if (bridgeCinematicCam)
            bridgeCinematicCam.Priority = 1;

        if (playerCam)
            playerCam.Priority = 20;
    }

    void LockBridge()
    {
        foreach (var plank in bridgePlanks)
        {
            if (!plank) continue;

            plank.bodyType = RigidbodyType2D.Kinematic;
            plank.gravityScale = 0f;
            plank.linearVelocity = Vector2.zero;
            plank.angularVelocity = 0f;
        }
    }

    /* ================= PEOPLE COUNT ================= */

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPerson(other)) return;

        peopleCount++;
        Debug.Log($"[Bridge] ENTER → {peopleCount}");

        CheckConditionLive();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPerson(other)) return;

        peopleCount = Mathf.Max(0, peopleCount - 1);
        Debug.Log($"[Bridge] EXIT → {peopleCount}");

        CheckConditionLive();
    }

    bool IsPerson(Collider2D col)
    {
        return col.CompareTag("Player")
            || col.CompareTag("NPC")
            || col.CompareTag("Enemy");
    }

    /* =====================================================
       CALLED FROM TERMINAL (ONLY ONCE)
    ===================================================== */

    public void EvaluateCondition(string ifLine)
    {
        if (isBroken) return;
        if (string.IsNullOrWhiteSpace(ifLine)) return;
        if (!ifLine.StartsWith("if") || !ifLine.EndsWith(":")) return;

        string condition = ifLine
            .Replace("if", "")
            .Replace(":", "")
            .Trim();

        string op = GetOperator(condition);
        if (op == null) return;

        string[] parts = condition.Split(op);
        if (parts.Length != 2) return;

        string variable = parts[0].Trim();
        string valueStr = parts[1].Trim();

        if (variable != "people_count") return;
        if (!int.TryParse(valueStr, out int value)) return;

        storedOperator = op;
        storedValue = value;
        conditionArmed = true;

        Debug.Log($"[Bridge] CONDITION ARMED → people_count {op} {value}");

        CheckConditionLive();
    }

    /* ================= LIVE CHECK ================= */

    void CheckConditionLive()
    {
        if (!conditionArmed || isBroken) return;

        bool result = Compare(peopleCount, storedOperator, storedValue);

        Debug.Log($"[Bridge] CHECK → {peopleCount} {storedOperator} {storedValue} = {result}");

        if (result)
            BreakBridge();
    }

    /* ================= OPERATORS ================= */

    string GetOperator(string condition)
    {
        if (condition.Contains(">=")) return ">=";
        if (condition.Contains("<=")) return "<=";
        if (condition.Contains("==")) return "==";
        if (condition.Contains("!=")) return "!=";
        if (condition.Contains(">")) return ">";
        if (condition.Contains("<")) return "<";
        return null;
    }

    bool Compare(int left, string op, int right)
    {
        switch (op)
        {
            case ">": return left > right;
            case "<": return left < right;
            case ">=": return left >= right;
            case "<=": return left <= right;
            case "==": return left == right;
            case "!=": return left != right;
            default: return false;
        }
    }

    /* ================= BREAK ================= */

    void BreakBridge()
    {
        if (isBroken) return;
        isBroken = true;

        Debug.Log("[Bridge] 💥 BRIDGE BROKEN");

        if (!cinematicPlaying && bridgeCinematicCam && playerCam)
            StartCoroutine(PlayCinematic());

        if (breakParticles)
        {
            breakParticles.transform.position =
                particleSpawnPoint ? particleSpawnPoint.position : transform.position;
            breakParticles.Play();
        }

        foreach (var plank in bridgePlanks)
        {
            if (!plank) continue;

            plank.bodyType = RigidbodyType2D.Dynamic;
            plank.gravityScale = 2f;

            plank.AddForce(
                new Vector2(Random.Range(-1f, 1f), Random.Range(1f, 2f)),
                ForceMode2D.Impulse
            );

            plank.AddTorque(Random.Range(-20f, 20f), ForceMode2D.Impulse);
        }
    }

    /* ================= CINEMATIC ================= */

    System.Collections.IEnumerator PlayCinematic()
    {
        cinematicPlaying = true;

        bridgeCinematicCam.Priority = 30;
        playerCam.Priority = 10;

        yield return new WaitForSeconds(cinematicDuration);

        bridgeCinematicCam.Priority = 1;
        playerCam.Priority = 30;

        cinematicPlaying = false;
    }
}
