using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DogModelEventPropagator : MonoBehaviour
{

    private DogMovement dogController = null; 

    // Start is called before the first frame update
    void Start()
    {
        dogController = transform.parent.GetComponent<DogMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnFinishedSniffingFloor()
    {
        dogController.OnFinishedSniffingFloor();
    }
}
