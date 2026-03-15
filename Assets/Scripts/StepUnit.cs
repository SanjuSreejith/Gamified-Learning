using UnityEngine;

public class StepUnit : MonoBehaviour
{
    SpriteRenderer sr;
    Collider2D col;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    public void SetGhost()
    {
        sr.enabled = false;
        col.enabled = false;
    }

    public void SetReal()
    {
        sr.enabled = true;
        col.enabled = true;
    }
}