using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class Compass : MonoBehaviour
{

    public Transform target;
    public float speed = 1.0F;

    private float startTime;
    private float journeyLength;
    private Vector3 updatedPos;

    private void Start()
    {
        startTime = Time.time;
    }


    // Update is called once per frame
    void Update()
    {
        Vector3 targetPosition = new Vector3(target.position.x,
                                       this.transform.position.y,
                                       target.position.z);
        transform.LookAt(targetPosition);
        //transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);

        
        updatedPos = new Vector3(0, transform.localEulerAngles.y, 0);
        journeyLength = Vector3.Distance(this.transform.position, updatedPos);

        float distCovered = (Time.time - startTime) * speed;
        float fractionOfJourney = distCovered / journeyLength;
        transform.localEulerAngles = Vector3.Lerp(this.transform.position, updatedPos, fractionOfJourney);


    }
}
