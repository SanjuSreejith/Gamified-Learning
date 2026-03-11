//using UnityEngine;

//public class TowerButtonTrigger : MonoBehaviour
//{
//    public Tower tower;

//    private bool playerInside;
//    private bool isHolding;

//    // Reference to terminal controller
//    private TowerTerminalController terminalController;

//    void Awake()
//    {
//        terminalController = GetComponent<TowerTerminalController>();
//    }

//    void Update()
//    {
//        // Don't process if tower is not a button type
//        if (tower == null || tower.towerType != Tower.TowerType.Button)
//            return;

//        // Don't process if terminal is editing (handled by terminal controller)
//        if (terminalController != null && terminalController.enabled == false)
//            return;

//        if (!playerInside || tower.machine == null)
//            return;

//        // PRESS & HOLD → ACTIVATE
//        if (Input.GetKeyDown(KeyCode.E) && !isHolding)
//        {
//            isHolding = true;
//            Debug.Log("Device signal ON");
//            tower.machine.ActivateMachine();
//        }

//        // RELEASE → DEACTIVATE
//        if (Input.GetKeyUp(KeyCode.E) && isHolding)
//        {
//            isHolding = false;
//            Debug.Log("Device signal OFF");
//            tower.machine.DeactivateMachine();
//        }
//    }

//    void OnTriggerEnter2D(Collider2D other)
//    {
//        if (!other.CompareTag("Player")) return;

//        playerInside = true;

//        // Show different message based on tower type
//        if (tower != null)
//        {
//            if (tower.towerType == Tower.TowerType.Button)
//            {
//                Debug.Log("Hold E to send device signal");
//            }
//            else
//            {
//                Debug.Log("Press E to access terminal");
//            }
//        }
//    }

//    void OnTriggerExit2D(Collider2D other)
//    {
//        if (!other.CompareTag("Player")) return;

//        playerInside = false;

//        // Safety: stop machine if player walks away (only for button towers)
//        if (isHolding && tower != null && tower.towerType == Tower.TowerType.Button)
//        {
//            isHolding = false;
//            tower.machine.DeactivateMachine();
//            Debug.Log("Signal lost (player left)");
//        }
//    }
//}