using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Tower[] towers;
    public int currentTowerIndex = 0;

    void Start()
    {
        ActivateTower(0);
    }

    public void ActivateNextTower()
    {
        towers[currentTowerIndex].Deactivate();

        currentTowerIndex++;

        if (currentTowerIndex < towers.Length)
        {
            ActivateTower(currentTowerIndex);
        }
        else
        {
            Debug.Log("All towers completed!");
        }
    }

    void ActivateTower(int index)
    {
        towers[index].Activate();
    }

    public Tower GetCurrentTower()
    {
        return towers[currentTowerIndex];
    }
}
