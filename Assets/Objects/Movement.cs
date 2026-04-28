using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movemenbtr2p31ldegt22q1swdcv : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //hrizontal movement
        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.left * Time.deltaTime * 5);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(Vector3.right * Time.deltaTime * 5);
        }

        if (Input.GetKey(KeyCode.Space))
        {
            transform.Translate(Vector3.up * Time.deltaTime * 5);
        }

        //dash
        if (Input.GetKeyDown(KeyCode.E))
        {
            transform.Translate(Vector3.forward * Time.deltaTime * 5);
        }
    }
}
