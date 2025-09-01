using UnityEngine;

public class TroopMovement : MonoBehaviour
{
    public float speed = 5f;
    public float MouseMoveSpeed = 5f;

    private Camera mainCamera;
    private bool isLeader = false;

    public void SetAsLeader(bool status)
    {
        isLeader = status;
    }

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        if (!isLeader) return;

        if (Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                float clampedX = Mathf.Clamp(hit.point.x, -13f, 13f); // Set bounds accordingly
                Vector3 newPos = new Vector3(clampedX, transform.position.y, transform.position.z);
                transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * MouseMoveSpeed);
            }
        }
    }
}
