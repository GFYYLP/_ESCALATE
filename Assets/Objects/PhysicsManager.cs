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

        // 1. Let each body update its own velocity (input, gravity, dash etc.)
        foreach (var b in bodies)
            b.UpdateVelocity(dt);

        // 2. Integrate — move candidate positions
        foreach (var b in bodies)
            b.candidatePos = b.candidatePos + b.velocity * dt;

        // 3. Broad phase — collect overlapping pairs
        var pairs = new List<(PhysicsBody, PhysicsBody)>();
        for (int i = 0; i < bodies.Count; i++)
            for (int j = i + 1; j < bodies.Count; j++)
                if (AABBOverlap(bodies[i], bodies[j]))
                    pairs.Add((bodies[i], bodies[j]));

        // 4. Narrow phase + resolve — iterate a few times for stability
        for (int iter = 0; iter < 3; iter++)
        {
            foreach (var (a, b) in pairs)
                ResolveOverlap(a, b);
        }

        // 5. Commit positions, update onGround
        foreach (var b in bodies)
        {
            b.UpdateGroundState(bodies);
            WrapPosition(b);
            b.transform.position = b.candidatePos;
        }
    }

    bool AABBOverlap(PhysicsBody a, PhysicsBody b)
    {
        Vector2 delta = a.candidatePos - b.candidatePos;
        float overlapX = (a.size.x + b.size.x) * 0.5f - Mathf.Abs(delta.x);
        float overlapY = (a.size.y + b.size.y) * 0.5f - Mathf.Abs(delta.y);
        return overlapX > 0f && overlapY > 0f;
    }

    void ResolveOverlap(PhysicsBody a, PhysicsBody b)
    {
        Vector2 delta    = a.candidatePos - b.candidatePos;
        float overlapX   = (a.size.x + b.size.x) * 0.5f - Mathf.Abs(delta.x);
        float overlapY   = (a.size.y + b.size.y) * 0.5f - Mathf.Abs(delta.y);

        // Resolve along axis of least penetration
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

        a.candidatePos += normal *  penetration * aShare;
        b.candidatePos -= normal *  penetration * bShare;

        // Velocity exchange along normal
        float aSpeed = Vector2.Dot(a.velocity, normal);
        float bSpeed = Vector2.Dot(b.velocity, normal);
        if (aSpeed - bSpeed <= 0f) return;  // already separating

        float impactSpeed = aSpeed - bSpeed;

        if (!a.isKinematic) a.velocity -= normal * impactSpeed * aShare;
        if (!b.isKinematic) b.velocity += normal * impactSpeed * bShare;

        // Notify both sides
        a.OnImpact(impactSpeed, b);
        b.OnImpact(impactSpeed, a);
    }

    void WrapPosition(PhysicsBody b)
    {
        Camera cam   = Camera.main;
        float width  = cam.orthographicSize * 2f * cam.aspect;
        float halfW  = width * 0.5f;
        float camX   = cam.transform.position.x;

        if (b.candidatePos.x > camX + halfW) b.candidatePos.x -= width;
        else if (b.candidatePos.x < camX - halfW) b.candidatePos.x += width;
    }
}