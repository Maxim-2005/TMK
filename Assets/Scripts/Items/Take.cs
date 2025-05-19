using UnityEngine;

public class Take : MonoBehaviour
{
    float distance = 100;
    public Transform pos;
    private Rigidbody rb;
    private MeshCollider ms;
    private Fpc_param fpc_;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        fpc_ = GameObject.Find("Player").GetComponent<Fpc_param>();
    }

    // По нажатию ЛКМ поднимает перемещает предмет в указанную точку(руку)
    void OnMouseDown()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, distance) && fpc_.Take == false)
        {
            rb.isKinematic = true;
            fpc_.Take = true;
            rb.MovePosition(pos.position);
        }
    }


    void FixedUpdate()
    {
        // Бросок предмета на G и следования предмета за персонажем
        if (rb.isKinematic == true)
        {
            this.gameObject.transform.position = pos.position;
            if (Input.GetKeyDown(KeyCode.G))
            {
                fpc_.Take = false;
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.AddForce(Camera.main.transform.forward * 500);
            }
        }
    }
}
