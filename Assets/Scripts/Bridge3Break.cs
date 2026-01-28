
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BridgeBreak3Controller2D : MonoBehaviour
{
    [Header("Bridge Parts")]
    public Rigidbody2D[] bridgePlanks;

    [Header("Break Effects")]
    public ParticleSystem breakParticles;
    public Transform particleSpawnPoint;

    [Header("Runtime")]
    public int peopleCount;
    public bool isBroken;

    /* ================= CINEMATIC CAMERA ================= */

    [Header("Cinematic Camera")]
    public CinemachineCamera bridgeCinematicCam; // UNIQUE per bridge
    public CinemachineCamera playerCam;           // shared player cam
    public float cinematicDuration = 2.5f;

    bool cinematicPlaying;

    /* ================= INTERNAL ================= */

    HashSet<GameObject> peopleOnBridge = new HashSet<GameObject>();

    string storedIfLine;
    bool ruleSet;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        LockBridge();

        if (bridgeCinematicCam)
            bridgeCinematicCam.Priority = 1;

        if (playerCam)
            playerCam.Priority = 20;

        Debug.Log("[Bridge3] Bridge initialized & locked.");
    }

    /* ================= LOCK ================= */

    void LockBridge()
    {
        foreach (var rb in bridgePlanks)
        {
            if (!rb) continue;

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    /* ================= PEOPLE TRACKING ================= */

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken) return;
        if (!IsPerson(other)) return;

        GameObject root = other.attachedRigidbody
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (peopleOnBridge.Add(root))
        {
            Debug.Log($"[Bridge3] ENTER: {root.name}");
            UpdateCount();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isBroken) return;
        if (!IsPerson(other)) return;

        GameObject root = other.attachedRigidbody
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (peopleOnBridge.Remove(root))
        {
            Debug.Log($"[Bridge3] EXIT: {root.name}");
            UpdateCount();
        }
    }

    void UpdateCount()
    {
        peopleCount = peopleOnBridge.Count;
        Debug.Log($"[Bridge3] people_count = {peopleCount}");

        CheckCondition();
    }

    bool IsPerson(Collider2D col)
    {
        return col.CompareTag("Player")
            || col.CompareTag("NPC")
            || col.CompareTag("Enemy");
    }

    /* ================= RULE ================= */

    public void SetCondition(string ifLine)
    {
        if (string.IsNullOrWhiteSpace(ifLine))
        {
            Debug.LogWarning("[Bridge3] Rule rejected: empty input.");
            return;
        }

        storedIfLine = ifLine.Trim();
        ruleSet = true;

        Debug.Log($"[Bridge3] RULE SET: {storedIfLine}");

        CheckCondition();
    }

    void CheckCondition()
    {
        if (!ruleSet || isBroken) return;

        Debug.Log("[Bridge3] Checking condition...");
        EvaluateCondition(storedIfLine);
    }

    /* ================= IF EVALUATION ================= */

    void EvaluateCondition(string ifLine)
    {
        if (!ifLine.StartsWith("if") || !ifLine.EndsWith(":"))
        {
            Debug.LogWarning("[Bridge3] Invalid IF syntax.");
            return;
        }

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

        bool result = Compare(peopleCount, op, value);

        Debug.Log($"[Bridge3] EVAL: {peopleCount} {op} {value} → {result}");

        if (result)
            BreakBridge();
    }

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

        Debug.Log("🔥 [Bridge3] BRIDGE BREAK TRIGGERED!");

        if (!cinematicPlaying && bridgeCinematicCam && playerCam)
            StartCoroutine(PlayCinematic());

        if (breakParticles)
        {
            breakParticles.transform.position =
                particleSpawnPoint ? particleSpawnPoint.position : transform.position;
            breakParticles.Play();
        }

        foreach (var rb in bridgePlanks)
        {
            if (!rb) continue;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 2f;
            rb.AddForce(
                new Vector2(Random.Range(-1f, 1f), Random.Range(1f, 1.8f)),
                ForceMode2D.Impulse
            );
            rb.AddTorque(Random.Range(-20f, 20f), ForceMode2D.Impulse);
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
