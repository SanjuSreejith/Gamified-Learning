using UnityEngine;

public class InfiniteParallax : MonoBehaviour
{
    public Transform cam;

    public float xMultiplier = 0.3f;
    public float yMultiplier = 0.2f;


    private Vector3 startPos;
    private float lengthX;

    void Start()
    {
        if (cam == null)
            cam = Camera.main.transform;

        startPos = transform.position;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        lengthX = sr.bounds.size.x;
    }

    void Update() // IMPORTANT
    {
        if (cam == null) return;

        float distX = cam.position.x * xMultiplier;
        float distY = cam.position.y * yMultiplier;

        transform.position = new Vector3(
            startPos.x + distX,
            startPos.y + distY,
            transform.position.z
        );
    }
}


