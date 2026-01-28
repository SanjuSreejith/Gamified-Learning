using UnityEngine;
using System.Collections;

public class JetpackController2D : MonoBehaviour
{
    public Transform[] landingPoints;
    int currentPoint = 0;

    public float flySpeed = 5f;
    public bool equipped;

    public void Equip()
    {
        equipped = true;
    }

    public void FlyToNextPoint()
    {
        if (!equipped || currentPoint >= landingPoints.Length) return;
        StartCoroutine(FlyRoutine(landingPoints[currentPoint]));
        currentPoint++;
    }

    IEnumerator FlyRoutine(Transform target)
    {
        while (Vector2.Distance(transform.position, target.position) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                target.position,
                flySpeed * Time.deltaTime
            );
            yield return null;
        }
    }

    public void FailFall()
    {
        GetComponent<Rigidbody2D>().gravityScale = 3f;
    }
}
