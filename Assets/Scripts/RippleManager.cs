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
    
    void Awake() => Instance = this;


    private void Start()
    {
        rippleBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            16,
            Marshal.SizeOf<Ripple>()
        );
    }

    public void AddPointRipple(Vector2 pos, float strength)
    {
        if (ripples.Count >= maxRipples) return;
        ripples.Add(new Ripple { position = pos, dir = new Vector2(), strength = strength, age = 0f, type = 0 });
    }
    
    public void AddDirRipple(Vector2 pos, float strength, Vector2 dir)
    {
        if (ripples.Count >= maxRipples) return;
        ripples.Add(new Ripple { position = pos, dir = dir, strength = strength, age = 0f, type = 1 });
    }
    

    void Update()
    {
        //age and cull ripples
        for (int i = ripples.Count - 1; i >= 0; i--)
        {
            var r = ripples[i];
            r.age += Time.deltaTime;
            if (r.age > 2f)  // ripple lifetime
                ripples.RemoveAt(i); //safe removal with reverse iterating 
            else
                ripples[i] = r;
        }
        
        rippleBuffer.SetData(ripples);

        gridMaterial.SetBuffer("_Ripples", rippleBuffer);
        gridMaterial.SetInt("_RippleCount", ripples.Count);
    }
    
    void OnDestroy()
    {
        rippleBuffer?.Release();  //free gpu memory, independent of gc
    }
}
