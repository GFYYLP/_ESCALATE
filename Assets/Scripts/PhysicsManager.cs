using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    public static PhysicsManager Instance;
    private List<PhysicsBody> bodies = new List<PhysicsBody>();

    void Awake() => Instance = this;

    public void Register(PhysicsBody b)   => bodies.Add(b);
    public void Unregister(PhysicsBody b) => bodies.Remove(b);

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        //let each body update its own velocity THEN move candidate positions
        foreach (var b in bodies) b.UpdateVelocity(dt);
        foreach (var b in bodies) b.candidatePos = b.candidatePos + b.velocity * dt;

        //broad phase to collect overlapping pairs
        var pairs = new List<(PhysicsBody, PhysicsBody)>();
        for (int i = 0; i < bodies.Count; i++)
            //starts at i+1 to avoid double-checking pairs and self-collision
            for (int j = i + 1; j < bodies.Count; j++)
                if (AABBOverlap(bodies[i], bodies[j]))
                    pairs.Add((bodies[i], bodies[j]));

        //narrow phase + resolve (iterate a few times for stability)
        for (int iter = 0; iter < 3; iter++)
        {
            foreach (var (a, b) in pairs)
                ResolveOverlap(a, b);
        }

        //commit position
        foreach (var b in bodies)
        {
            b.UpdateGroundState(bodies);
            if (b.onGround && b.velocity.y < 0f) b.velocity.y = 0f;
            b.transform.position = b.candidatePos;
        }
        
        //commit deletion
        var toDestroy = bodies.FindAll(b => b.pendingDestroy);

        //remove from list before destroying
        foreach (var b in toDestroy)
            bodies.Remove(b);

        //now destroy safely, OnDisable fires but list is already clean
        foreach (var b in toDestroy)
            Destroy(b.gameObject);
    }

    bool AABBOverlap(PhysicsBody a, PhysicsBody b)
    {
        Vector2 delta  = a.candidatePos - b.candidatePos;
        float overlapX = (a.size.x + b.size.x) * 0.5f - Mathf.Abs(delta.x);
        float overlapY = (a.size.y + b.size.y) * 0.5f - Mathf.Abs(delta.y);

        // Proximity window — within 5 pixels (0.3 units at your scale)
        float proximityThreshold = 0.3f;
        if (overlapX > -proximityThreshold && overlapY > -proximityThreshold)
        {
            // Derive actual contact normal from overlap axes
            Vector2 normal;
            if (Mathf.Abs(overlapX) < Mathf.Abs(overlapY))
                normal = new Vector2(Mathf.Sign(delta.x), 0f);
            else
                normal = new Vector2(0f, Mathf.Sign(delta.y));

            a.nearBlock  = true;
            a.nearNormal = normal;
            b.nearBlock  = true;
            b.nearNormal = -normal;
        }

        bool result = overlapX > 0f && overlapY > 0f;
        
        //check if either PhysicsBodies is Player
        if (result == true)
        {
            bool  isSide   = overlapX < overlapY;
            if (a is Player) a.TryLatch(b, isSide);
            else if (b is Player) b.TryLatch(a, isSide);
        }


        return result;
    }

    void ResolveOverlap(PhysicsBody a, PhysicsBody b)
    {
        Vector2 delta    = a.candidatePos - b.candidatePos;
        float overlapX   = (a.size.x + b.size.x) * 0.5f - Mathf.Abs(delta.x);
        float overlapY   = (a.size.y + b.size.y) * 0.5f - Mathf.Abs(delta.y);

        //resolve normal along axis of least penetration
        Vector2 normal;
        float   penetration;
        if (overlapX < overlapY)
        {
            normal      = new Vector2(Mathf.Sign(delta.x), 0f);
            penetration = overlapX;
        }
        else
        {
            normal      = new Vector2(0f, Mathf.Sign(delta.y));
            penetration = overlapY;
        }

        // Push apart by half each (equal mass assumption)
        // Kinematic bodies (immovable) get zero push share
        float totalInvMass = (a.isKinematic ? 0f : 1f) + (b.isKinematic ? 0f : 1f);
        if (totalInvMass == 0f) return;

        float aShare = a.isKinematic ? 0f : 1f / totalInvMass;
        float bShare = b.isKinematic ? 0f : 1f / totalInvMass;

        //position correction for immediate geometric fix
        a.candidatePos += normal *  penetration * aShare;
        b.candidatePos -= normal *  penetration * bShare;

        // Velocity exchange along normal
        float aSpeed = Vector2.Dot(a.velocity, normal);
        float bSpeed = Vector2.Dot(b.velocity, normal);

        float impactSpeed = aSpeed - bSpeed;
        if (impactSpeed <= 0f) return;  // already separating
        
        //TODO: leverage gravity temporarily upon collision
        a.velocity.y = 0;
        b.velocity.y = 0;

        if (!a.isKinematic) a.velocity -= normal * impactSpeed * (1.0f + a.Weight) * aShare;
        if (!b.isKinematic) b.velocity += normal * impactSpeed * (1.0f + b.Weight) * bShare;
        

        // Notify both sides
        a.OnImpact(impactSpeed, b);
        b.OnImpact(impactSpeed, a);
    }
}