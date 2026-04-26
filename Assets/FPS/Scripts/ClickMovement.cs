using UnityEngine;
using UnityEngine.AI;

public class ClickMovement : MonoBehaviour
{
    public Camera cam;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        Move();
    }
    void Move() {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitPoint;

            if(Physics.Raycast(ray, out hitPoint)) {
                agent.SetDestination(hitPoint.point);
            }
        }
    }
}
