using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RippleManager : MonoBehaviour
{
    [SerializeField] private Material gridMaterial;
    [SerializeField] private int      maxRipples = 16;

    private struct Ripple
    {
        public Vector2 position;
        public float   strength;
        public float   age;
    }

    private List<Ripple> ripples = new List<Ripple>();
    private Vector4[]    rippleBuffer;

    void Awake() => rippleBuffer = new Vector4[maxRipples];

    public void AddRipple(Vector2 pos, float strength)
    {
        if (ripples.Count >= maxRipples) return;
        ripples.Add(new Ripple { position = pos, strength = strength, age = 0f });
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

        //pack into Vector4 array for shader
        for (int i = 0; i < ripples.Count; i++)
            rippleBuffer[i] = new Vector4(
                ripples[i].position.x,
                ripples[i].position.y,
                ripples[i].strength,
                ripples[i].age
            );

        gridMaterial.SetVectorArray("_Ripples",    rippleBuffer);
        gridMaterial.SetInt        ("_RippleCount", ripples.Count);
    }
}
