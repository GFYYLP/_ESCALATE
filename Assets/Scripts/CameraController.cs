using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Player player;

    [SerializeField] private PhysicsManager physicsManager;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }
    
    public Material material;

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        Graphics.Blit(src, dst, material);
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
        
        material.SetFloat("_SystemStability", physicsManager.stabilityRatio);
    }
}
