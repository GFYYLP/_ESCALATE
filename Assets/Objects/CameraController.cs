using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Player player;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        float verticalPos = Mathf.Max(0f, player.transform.position.y);

        transform.position = new Vector3(
            transform.position.x,
            verticalPos,
            transform.position.z
        );
    }
}
