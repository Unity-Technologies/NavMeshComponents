using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{

    NavMeshAgent navmesh;
    [SerializeField] Camera cam;
    private void Start()
    {
        navmesh = GetComponent<NavMeshAgent>();
    }
    private void FixedUpdate()
    {
        if(Input.GetMouseButton(0)) 
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray,out hit))
            {
                navmesh.SetDestination(hit.point);
            }
        
        }
    }
}
