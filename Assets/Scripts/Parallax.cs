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

void LateUpdate()
{
    float dist = cam.position.x * parallaxStrength;
    transform.position = new Vector3(startPos.x + dist, startPos.y, startPos.z);

    if (cam.position.x > startPos.x + length)
        startPos.x += length;
}

}

