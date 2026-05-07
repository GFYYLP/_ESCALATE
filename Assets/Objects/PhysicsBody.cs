using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PhysicsBody : MonoBehaviour
{
    [HideInInspector] public Vector2 candidatePos;
    [HideInInspector] public Vector2 velocity;
    [HideInInspector] public bool    onGround;
    [HideInInspector] public bool    isKinematic;  // true = immovable (your starting platform)

    public Vector2   size => GetComponent<BoxCollider2D>().size;
    public float     Speed => velocity.magnitude;

    protected virtual void OnEnable()
    {
        candidatePos = transform.position;
        PhysicsManager.Instance.Register(this);
    }

    protected virtual void OnDisable() =>
        PhysicsManager.Instance.Unregister(this);

    // Each subclass implements its own velocity update (input, gravity, dash)
    public abstract void UpdateVelocity(float dt);

    public virtual void ApplyImpulse(Vector2 impulse) =>
        velocity += impulse;

    public virtual void OnImpact(float impactSpeed, PhysicsBody other) { }

    public void UpdateGroundState(List<PhysicsBody> bodies)
    {
        onGround = false;
        const float probe = 0.08f;
        Vector2 feetPos = candidatePos + Vector2.down * probe;

        foreach (var other in bodies)
        {
            if (other == this) continue;
            Vector2 delta    = feetPos - other.candidatePos;
            float overlapX   = (size.x + other.size.x) * 0.5f - Mathf.Abs(delta.x);
            float overlapY   = (size.y + other.size.y) * 0.5f - Mathf.Abs(delta.y);
            if (overlapX > 0f && overlapY > 0f)
            {
                onGround = true;
                return;
            }
        }
    }
}