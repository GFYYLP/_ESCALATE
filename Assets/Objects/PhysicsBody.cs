using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PhysicsBody : MonoBehaviour
{
    // --- Shared serialized fields ---
    [SerializeField] protected Vector2 colliderSize = new Vector2(0.9f, 1.8f);
    [SerializeField] public    LayerMask groundLayer;
    [SerializeField] protected float groundProbe  = 0.08f;
    [SerializeField] protected float gravity      = 28f;
    [SerializeField] protected float maxFallSpeed = 16f;

    protected Vector2    velocity;
    protected bool       onGround;
    protected Collider2D ownCollider;

    protected virtual void Awake()
    {
        ownCollider = GetComponent<Collider2D>();
    }

    // --- Shared collision helpers ---
    protected Collider2D GetFirstOverlap(Vector2 pos)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(pos, colliderSize, 0f, groundLayer);
        foreach (var hit in hits)
            if (hit != null && hit != ownCollider)
                return hit;
        return null;
    }

    protected Vector2 GetCollisionNormal(Vector2 myPos, Vector2 otherPos)
    {
        Vector2 delta = myPos - otherPos;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return new Vector2(Mathf.Sign(delta.x), 0f);
        else
            return new Vector2(0f, Mathf.Sign(delta.y));
    }

    protected void ResolveCollision(Collider2D other, Vector2 normal)
    {
        PhysicsBody otherBody = other.GetComponent<PhysicsBody>();

        if (otherBody != null)
        {
            float mySpeed    = Vector2.Dot(velocity, normal);
            float theirSpeed = Vector2.Dot(otherBody.velocity, normal);
            if (mySpeed - theirSpeed <= 0f) return;

            float exchange   = mySpeed - theirSpeed;
            float impactSpeed = Mathf.Abs(exchange);

            velocity           -= normal * exchange;
            otherBody.ApplyImpulse(normal * exchange);

            OnImpact(impactSpeed, other);
        }
        else
        {
            // Static geometry
            float s = Vector2.Dot(velocity, normal);
            if (s > 0f) velocity -= normal * s;
        }
    }

    // Override in subclasses to react to impacts
    protected virtual void OnImpact(float impactSpeed, Collider2D other) { }

    public virtual void ApplyImpulse(Vector2 impulse)
    {
        velocity += impulse;
    }

    protected void MoveX(ref Vector2 pos, float amount)
    {
        if (amount == 0f) return;
        pos.x += amount;

        Collider2D hit = GetFirstOverlap(pos);
        if (hit == null) return;

        pos.x -= amount;
        ResolveCollision(hit, GetCollisionNormal(pos, hit.transform.position));
    }

    protected void MoveY(ref Vector2 pos, float amount)
    {
        if (amount == 0f) return;
        pos.y += amount;

        Collider2D hit = GetFirstOverlap(pos);
        if (hit == null) return;

        float sign = Mathf.Sign(amount);
        float step = Mathf.Abs(amount);
        pos.y -= amount;
        for (int i = 0; i < 8; i++)
        {
            step  *= 0.5f;
            pos.y += sign * step;
            if (GetFirstOverlap(pos) != null)
                pos.y -= sign * step;
        }

        ResolveCollision(hit, GetCollisionNormal(pos, hit.transform.position));
    }

    protected bool CheckGround(Vector2 pos)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            pos + Vector2.down * groundProbe, colliderSize, 0f, groundLayer);
        foreach (var hit in hits)
            if (hit != null && hit != ownCollider)
                return true;
        return false;
    }

    protected void WrapPosition(ref Vector2 pos)
    {
        Camera cam  = Camera.main;
        float width = cam.orthographicSize * 2f * cam.aspect;
        float halfW = width * 0.5f;
        float camX  = cam.transform.position.x;

        if (pos.x > camX + halfW) pos.x -= width;
        else if (pos.x < camX - halfW) pos.x += width;
    }

    public Vector2 Velocity  => velocity;
    public float   Speed     => velocity.magnitude;
}