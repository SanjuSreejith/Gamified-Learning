using System.Collections.Generic;
using UnityEngine;

public class StepGen : MonoBehaviour
{
    public GameObject stepPrefab;
    public Transform player;

    public float xGap = 1f;
    public float yGap = 0.5f;
    public int poolSize = 30;
    public float maxHeight = 20f;

    List<GameObject> pool = new List<GameObject>();

    bool waitNum = false;
    string buffer = "";
    bool locked = false;

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject s = Instantiate(stepPrefab);
            s.GetComponent<StepUnit>().SetGhost();
            pool.Add(s);
        }
    }

    void Update()
    {
        if (!locked)
            FollowPlayerPreview();

        if (Input.GetKeyDown(KeyCode.G))
        {
            waitNum = true;
            buffer = "";
            Time.timeScale = 0f; // pause
            Debug.Log("Step mode...");
        }

        if (waitNum)
            ReadInput();
    }

    void FollowPlayerPreview()
    {
        Vector3 start = player.position;

        for (int i = 0; i < pool.Count; i++)
        {
            Vector3 pos = new Vector3(
                start.x + (i + 1) * xGap,
                start.y + (i + 1) * yGap,
                0
            );

            pool[i].transform.position = pos;
        }
    }

    void ReadInput()
    {
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                buffer += i.ToString();
                Debug.Log("Input: " + buffer);
            }
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (buffer != "")
            {
                int count = int.Parse(buffer);
                Materialize(count);
            }

            waitNum = false;
            Time.timeScale = 1f;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            waitNum = false;
            Time.timeScale = 1f;
        }
    }

    void Materialize(int count)
    {
        locked = true; // stop following player

        for (int i = 0; i < pool.Count; i++)
        {
            if (i < count && pool[i].transform.position.y <= maxHeight)
                pool[i].GetComponent<StepUnit>().SetReal();
            else
                pool[i].SetActive(false);
        }

        Debug.Log("Steps locked in world.");
    }
}