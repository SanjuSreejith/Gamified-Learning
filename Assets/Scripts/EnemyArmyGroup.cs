using UnityEngine;
using System.Collections.Generic;

public class EnemyArmyGroup : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Army Settings")]
    public float moveSpeed = 2f;

    public List<EnemyArmyUnit> units = new List<EnemyArmyUnit>();

    void Start()
    {
        RegisterUnits();
    }

    void Update()
    {
        RemoveDeadUnits();
    }

    /* ================= REGISTER ================= */
    void RegisterUnits()
    {
        units.Clear();

        foreach (Transform child in transform)
        {
            EnemyArmyUnit unit = child.GetComponent<EnemyArmyUnit>();
            if (unit != null)
            {
                unit.player = player;
                unit.moveSpeed = moveSpeed;
                units.Add(unit);
            }
        }
    }

    /* ================= FRONT UNIT ================= */
    public Transform GetFrontUnit()
    {
        EnemyArmyUnit front = null;

        foreach (var unit in units)
        {
            if (unit == null || !unit.isAlive) continue;

            if (front == null || unit.GetX() > front.GetX())
                front = unit;
        }

        return front != null ? front.transform : null;
    }

    /* ================= CLEANUP ================= */
    void RemoveDeadUnits()
    {
        units.RemoveAll(u => u == null || !u.isAlive);
    }
}