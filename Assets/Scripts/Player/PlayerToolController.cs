using Unity.VisualScripting;
using UnityEngine;

public class PlayerToolController : MonoBehaviour
{
    PlayerPickup playerPickup;
    void Start()
    {
        if (playerPickup == null) 
        {
            playerPickup = GetComponent<PlayerPickup>(); 
        }
    }
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Axe axeTool = playerPickup.HeldObject.GetComponent<Axe>();
            
            if (axeTool != null)
            {
                Debug.Log("Нажата кнопка атаки. Топор в руке.");
            }
        }
    }
}
