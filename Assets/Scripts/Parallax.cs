using UnityEngine;

public class Parallax : MonoBehaviour
{
    public Transform cam;
    [Range(0f, 1f)] public float parallaxStrength = 0.1f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    public float length;
    public float height;

    void LateUpdate()
    {
        float distX = cam.position.x * parallaxStrength;
        float distY = cam.position.y * parallaxStrength;
        transform.position = new Vector3(startPos.x + distX, startPos.y + distY, startPos.z);

        if (cam.position.x > startPos.x + length)
            startPos.x += length;
        
        if (cam.position.y > startPos.y + height)
            startPos.y += height;
    }

}

