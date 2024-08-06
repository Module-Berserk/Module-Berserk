using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallexScroll : MonoBehaviour
{
    private Vector3 startpos;
    public float parallaxFactor;
    public GameObject cam;
 
    void Start()
    {
        startpos = transform.position;
    }
 
    void Update()
    {
        Vector3 distance = cam.transform.position * parallaxFactor;
    
        Vector3 newPosition = startpos + distance;
    
        transform.position = newPosition;
    }
}
