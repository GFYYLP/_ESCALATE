using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class RippleManager : MonoBehaviour
{
    public static RippleManager Instance;
    
    [SerializeField] private Material gridMaterial;
    [SerializeField] private int      maxRipples = 16;
    [SerializeField] private float    directionTriggerVal = 0.5f;
    [SerializeField] private float    pointTriggerVal = 0.3f;
    [SerializeField] private float    bloomTriggerVal = 0.1f;
    [SerializeField] private float  rippleCooldown;
    [SerializeField] private Transform rippleGrid;
    [SerializeField] private float rippleStrength = 1f;
    
    [StructLayout(LayoutKind.Sequential)]
    private struct Ripple
    {
        public Vector2 position;
        public Vector2 dir;
        public float   strength;
        public float   age;
        public int type;
        
        public float padding;
    }

    private List<Ripple> ripples = new List<Ripple>();
    private GraphicsBuffer    rippleBuffer;
    
    private float[] lifetime = { 6f, 2f, 2f, 0.3f };  //lifetime val for each ripple
    
    void Awake() => Instance = this;


    public void RespondToBody(PhysicsBody body)
    {
        rippleCooldown -= Time.deltaTime;
        if (rippleCooldown > 0f) return;

        if (body.Speed > directionTriggerVal)
        {
            AddDirRipple(body.candidatePos, body.Speed * rippleStrength, body.velocity);
            rippleCooldown = 0.2f; 
            AudioManager.Instance.DistortMusic(body.Speed * 0.1f);
        }
        if (body.accel > pointTriggerVal)
        {
            AddPointRipple(body.candidatePos, body.Speed * 4.0f);
            AudioManager.Instance.DistortMusic(body.Speed * 2f);
        }
    }

    private void Start()
    {
        rippleBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            maxRipples,
            Marshal.SizeOf<Ripple>()
        );
    }

    public void AddPointRipple(Vector2 pos, float strength)
    {
        if (ripples.Count >= maxRipples)
            ripples.RemoveAt(0); // oldest is always at index 0
        ripples.Add(new Ripple { position = pos, dir = new Vector2(), strength = strength, age = 0f, type = 0 });
    }
    
    public void AddDirRipple(Vector2 pos, float strength, Vector2 dir)
    {
        if (ripples.Count >= maxRipples)
            ripples.RemoveAt(0); // oldest is always at index 0
        ripples.Add(new Ripple { position = pos, dir = dir, strength = strength, age = 0f, type = 1 });
    }
    
    public void AddBloomRipple(Vector2 pos)
    {
        if (ripples.Count >= maxRipples)
            ripples.RemoveAt(0); // oldest is always at index 0
        ripples.Add(new Ripple { position = pos, dir = Vector2.zero, strength = 0.5f, age = 0f, type = 3 });
    }
    
    public void AddScanlineRipple(Vector2 pos)
    {
        if (ripples.Count >= maxRipples)
            ripples.RemoveAt(0); // oldest is always at index 0
        ripples.Add(new Ripple { position = pos, dir = Vector2.zero, strength = 0f, age = 0f, type = 2 });
    }
    

    void LateUpdate()
    {
        //update grid position
        Vector3 cam = Camera.main.transform.position;
        rippleGrid.transform.position = new Vector3(
            cam.x,
            cam.y,
            rippleGrid.transform.position.z
        );
        
        //age and cull ripples
        for (int i = ripples.Count - 1; i >= 0; i--)
        {
            var r = ripples[i];
            
            //if (r.type == 2) r.age += Time.deltaTime * 0.1f;
            r.age += Time.deltaTime;
            
            if (r.age > lifetime[r.type])  // ripple lifetime
            {
                ripples.RemoveAt(i); //safe removal with reverse iterating 
            }
            else
                ripples[i] = r;
        }
        
        rippleBuffer.SetData(ripples);

        gridMaterial.SetBuffer("_Ripples", rippleBuffer);
        gridMaterial.SetInt("_RippleCount", ripples.Count);
    }
    
    void OnDestroy()
    {
        rippleBuffer?.Release();  //free gpu memory, independent of GC
    }
}
