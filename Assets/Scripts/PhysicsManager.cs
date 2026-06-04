using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    public static PhysicsManager Instance;
    public event Action<Vector2> onHighCollision;
    
    private RippleManager rippleManager;
    private List<PhysicsBody> bodies = new List<PhysicsBody>();
    [SerializeField] public float highCollideVal = 0.1f;
    [SerializeField] private float stabilizeRate = 10f;
    
    public float highestImpactSpeed = 0f;
    public float systemStability = 0f;
    public float corruptScore = 0f;
    
    void Awake()
    {
        Instance = this;
        rippleManager = GetComponent<RippleManager>();
    }

    void Start()
    {
        onHighCollision += rippleManager.AddScanlineRipple;
        onHighCollision += HandleHit;
    }

    public void Register(PhysicsBody b)   => bodies.Add(b);
    public void Unregister(PhysicsBody b) => bodies.Remove(b);

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        systemStability += corruptScore * stabilizeRate * dt;
        
        //let each body update its own velocity THEN move candidate positions
        foreach (var b in bodies)
        {
            b.UpdateVelocity(dt);
            
            if (b is Player && systemStability > 0f) systemStability -= b.Velocity.magnitude * dt;
        }
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
            b.accel = b.prevVelocity.magnitude - b.velocity.magnitude;
            b.prevVelocity = b.velocity;
            
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

    private bool paused = false;

    void Update()
    {
        //ripple manipulation
        foreach (var b in bodies)
        {
            rippleManager.RespondToBody(b);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
        }
    }

    bool AABBOverlap(PhysicsBody a, PhysicsBody b)
    {
        if (a.isKinematic && b is Block) return false;
        if (b.isKinematic && a is Block) return false;
        
        Vector2 delta  = a.candidatePos - b.candidatePos;
        float overlapX = (a.size.x + b.size.x) * 0.5f - Mathf.Abs(delta.x);
        float overlapY = (a.size.y + b.size.y) * 0.5f - Mathf.Abs(delta.y);

        //proximity handling
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
        
        //update per-body states
        if (result)
        {
            bool  isSide   = overlapX < overlapY;
            a.collidedBody = b;
            a.collidedToSide = isSide;
            b.collidedToSide = isSide;
            b.collidedBody = a;
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

        // Velocity exchange along normal
        float aSpeed      = Vector2.Dot(a.velocity, normal);
        float bSpeed      = Vector2.Dot(b.velocity, normal);
        float impactSpeed = aSpeed - bSpeed;
        if (b is Player) highestImpactSpeed = impactSpeed;
        
        if (a.isKinematic && b is Block) return;
        if (b.isKinematic && a is Block) return;
        
        float aShare = a.isKinematic ? 0f : 1f / totalInvMass;
        float bShare = b.isKinematic ? 0f : 1f / totalInvMass;
        
        //position correction for immediate geometric fix
        a.candidatePos += normal *  penetration * aShare;
        b.candidatePos -= normal *  penetration * bShare;
        
        //high collision handling
        if (Mathf.Abs(impactSpeed) > highCollideVal)
            onHighCollision?.Invoke(a.candidatePos);  //either a or b should work given collision proximity
        corruptScore += Mathf.Abs(impactSpeed) * Camera.main.transform.position.y * 0.5f;
        
        // aSpeed - bSpeed > 0 means a and b moves in opposite directions
        // We want to resolve when a is moving TOWARD b, i.e. impactSpeed < 0
        if (impactSpeed >= 0f) return;
        
        // Scale vertical impulse transfer down significantly
        float verticalDamp = Mathf.Abs(normal.y) > 0.5f ? 0.2f : 1f;

        if (!a.isKinematic) a.Velocity += normal * Mathf.Abs(impactSpeed) * a.weight * aShare;  // push a away from b
        if (!b.isKinematic) b.Velocity -= normal * Mathf.Abs(impactSpeed) * b.weight * bShare * verticalDamp;  // push b away from a
    }
    
    void HandleHit(Vector2 param = new Vector2())
    {
        //StartCoroutine(HitStop(0.01f));
    }
    
    IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
    }
}