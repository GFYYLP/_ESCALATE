using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PhysicsBody : MonoBehaviour
{
    [HideInInspector] public Vector2 candidatePos;
    [HideInInspector] public Vector2 velocity;
    [HideInInspector] public bool    onGround;
    [HideInInspector] public bool    receivedImpulseThisFrame;

    public Vector2 size { get; protected set; }

    protected virtual void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        size         = new Vector2(
            col.size.x * transform.localScale.x,
            col.size.y * transform.localScale.y
        );
        candidatePos = transform.position;
    }

    public virtual void ApplyImpulse(Vector2 impulse) =>
        velocity += impulse;

    public virtual void OnImpact(float impactSpeed, Block other) { }

    public void Commit() =>
        transform.position = candidatePos;

    public float Speed => velocity.magnitude;
}