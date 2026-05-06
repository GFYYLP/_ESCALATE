using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PhysicsBody : MonoBehaviour
{
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
    
    protected Collider2D GetFirstOverlap(Vector2 pos)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(pos, colliderSize, 0f, groundLayer);
        foreach (var hit in hits)
            if (hit != null && hit != ownCollider)
                return hit;
        return null;
    }

    protected Vector2 GetCollisionNormal(Vector2 myPos, Vector2 otherPos, Vector2 otherSize)
    {
        Vector2 delta = myPos - otherPos;
    
        // Overlap depth on each axis
        float overlapX = (colliderSize.x + otherSize.x) * 0.5f - Mathf.Abs(delta.x);
        float overlapY = (colliderSize.y + otherSize.y) * 0.5f - Mathf.Abs(delta.y);

        // Smallest overlap axis is the collision normal
        if (overlapX < overlapY)
            return new Vector2(Mathf.Sign(delta.x), 0f);
        else
            return new Vector2(0f, Mathf.Sign(delta.y));
    }

    protected void ResolveCollision(Collider2D other, Vector2 normal)
    {
        PhysicsBody otherBody = other.GetComponent<PhysicsBody>();

        if (otherBody != null)
        {
            //  dot to check if we're moving towards the other body along the normal
            float mySpeed    = Vector2.Dot(velocity, normal);
            float theirSpeed = Vector2.Dot(otherBody.velocity, normal);
            
            //  no resolving if we arent moving into them
            if (mySpeed - theirSpeed <= 0f) return;

            float exchange   = mySpeed - theirSpeed;
            float impactSpeed = Mathf.Abs(exchange);

            //  simple 1D elastic collision along the normal
            velocity           -= normal * exchange;
            otherBody.ApplyImpulse(normal * exchange);

            OnImpact(impactSpeed, other);
        }
    else
    {
        float s = Vector2.Dot(velocity, normal);
        if (s > 0f)
        {
            float impactSpeed = Mathf.Abs(s);
            velocity -= normal * s;
            OnImpact(impactSpeed, other);  // fire even for static geometry
        }
    }
    }

    // Override in subclasses to react to impacts
    protected virtual void OnImpact(float impactSpeed, Collider2D other) { }

    public virtual void ApplyImpulse(Vector2 impulse)
    {
        velocity += impulse;
    }

    protected void MoveY(ref Vector2 pos, float amount)
    {
        if (amount == 0f) return;

        const float offset = 0.01f;
        float sign = Mathf.Sign(amount);
        float dist = Mathf.Abs(amount);

        RaycastHit2D hit = SweepCast(pos, new Vector2(0f, sign), dist);

        if (hit.collider != null)
        {
            //hit.distance to caculate how far we can actually move before hitting
            //but subtract a small offset to prevent sticking
            float allowedDist = Mathf.Max(0f, hit.distance - 0.02f - offset);
            pos.y += sign * allowedDist;
            ResolveCollision(hit.collider, hit.normal);
        }
        else
        {
            pos.y += amount;
        }
    }

    protected void MoveX(ref Vector2 pos, float amount)
    {
        if (amount == 0f) return;

        const float offset = 0.01f;
        float sign = Mathf.Sign(amount);
        float dist = Mathf.Abs(amount);

        RaycastHit2D hit = SweepCast(pos, new Vector2(sign, 0f), dist);

        if (hit.collider != null)
        {
            float allowedDist = Mathf.Max(0f, hit.distance - 0.02f - offset);
            pos.x += sign * allowedDist;
            ResolveCollision(hit.collider, hit.normal);
        }
        else
        {
            pos.x += amount;
        }
    }

    protected RaycastHit2D SweepCast(Vector2 pos, Vector2 dir, float dist)
    {
        const float skin   = 0.02f;
        const float offset = 0.01f;  // pull back slightly so we're not starting inside

        RaycastHit2D[] hits = new RaycastHit2D[8];
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;

        // Start cast slightly behind to avoid zero-distance hit normal issues
        Vector2 origin = pos - dir * offset;

        int count = Physics2D.BoxCast(
            origin,
            colliderSize - Vector2.one * skin * 2f,
            0f, dir, filter, hits, dist + skin + offset
        );
        
        //searches for the closest hit  
        RaycastHit2D closest = default;
        float minDist = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider == ownCollider) continue;
            if (hits[i].distance < minDist)
            {
                minDist  = hits[i].distance;
                closest  = hits[i];
            }
        }
        return closest;
    }


    protected bool CheckGround(Vector2 pos)
    {
        RaycastHit2D hit = SweepCast(pos, Vector2.down, groundProbe);
        return hit.collider != null && hit.normal.y > 0.7f;
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