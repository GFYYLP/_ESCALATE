// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
//
// public class PhysicsManager : MonoBehaviour
// {
//     public static PhysicsManager Instance;
//     private List<PhysicsBody> bodies = new List<PhysicsBody>();
//
//     void Awake() => Instance = this;
//
//     public void Register(PhysicsBody b)   => bodies.Add(b);
//     public void Unregister(PhysicsBody b) => bodies.Remove(b);
//
//     void FixedUpdate()
//     {
//         float dt = Time.fixedDeltaTime;
//
//         //let each body update its own velocity THEN move candidate positions
//         foreach (var b in bodies) b.UpdateVelocity(dt);
//         foreach (var b in bodies) b.candidatePos = b.candidatePos + b.velocity * dt;
//
//         //broad phase to collect overlapping pairs
//         var pairs = new List<(PhysicsBody, PhysicsBody)>();
//         for (int i = 0; i < bodies.Count; i++)
//             //starts at i+1 to avoid double-checking pairs and self-collision
//             for (int j = i + 1; j < bodies.Count; j++)
//                 if (AABBOverlap(bodies[i], bodies[j]))
//                     pairs.Add((bodies[i], bodies[j]));
//
//         //narrow phase + resolve (iterate a few times for stability)
//         for (int iter = 0; iter < 3; iter++)
//         {
//             foreach (var (a, b) in pairs)
//                 ResolveOverlap(a, b);
//         }
//
//         //commit position
//         foreach (var b in bodies)
//         {
//             b.UpdateGroundState(bodies);
//             if (b.onGround && b.velocity.y < 0f) b.velocity.y = 0f;
//             
//             WrapPosition(b); //wrap around screen edges
//             b.transform.position = b.candidatePos;
//         }
//         
//         //commit deletion
//         bodies.RemoveAll(b =>
//         {
//             if (b.pendingDestroy)
//             {
//                 Destroy(b.gameObject);
//                 return true;
//             }
//             return false;
//         });
//     }
//
//     bool AABBOverlap(PhysicsBody a, PhysicsBody b)
//     {
//         //distance between bodies' centers 
//         Vector2 delta = a.candidatePos - b.candidatePos;
//         
//         //combined half-widths  minus actual center distance
//         float overlapX = (a.size.x + b.size.x) * 0.5f - Mathf.Abs(delta.x);
//         float overlapY = (a.size.y + b.size.y) * 0.5f - Mathf.Abs(delta.y);
//         
//         //if combined widths exceed separation, overlap occurs
//         return overlapX > 0f && overlapY > 0f;
//     }
//
//     void ResolveOverlap(PhysicsBody a, PhysicsBody b)
//     {
//         Vector2 delta    = a.candidatePos - b.candidatePos;
//         float overlapX   = (a.size.x + b.size.x) * 0.5f - Mathf.Abs(delta.x);
//         float overlapY   = (a.size.y + b.size.y) * 0.5f - Mathf.Abs(delta.y);
//
//         // Resolve normal along axis of least penetration
//         Vector2 normal;
//         float   penetration;  //penetration is 
//         if (overlapX < overlapY)
//         {
//             normal      = new Vector2(Mathf.Sign(delta.x), 0f);
//             penetration = overlapX;
//         }
//         else
//         {
//             normal      = new Vector2(0f, Mathf.Sign(delta.y));
//             penetration = overlapY;
//         }
//
//         // Push apart by half each (equal mass assumption)
//         // Kinematic bodies (immovable) get zero push share
//         float totalInvMass = (a.isKinematic ? 0f : 1f) + (b.isKinematic ? 0f : 1f);
//         if (totalInvMass == 0f) return;
//
//         float aShare = a.isKinematic ? 0f : 1f / totalInvMass;
//         float bShare = b.isKinematic ? 0f : 1f / totalInvMass;
//
//         //position correction for immediate geometric fix
//         a.candidatePos += normal *  penetration * aShare;
//         b.candidatePos -= normal *  penetration * bShare;
//
//         // Velocity exchange along normal
//         float aSpeed = Vector2.Dot(a.velocity, normal);
//         float bSpeed = Vector2.Dot(b.velocity, normal);
//         if (aSpeed - bSpeed <= 0f) return;  // already separating
//
//         float impactSpeed = aSpeed - bSpeed;
//
//         if (!a.isKinematic) a.velocity -= normal * impactSpeed * aShare;
//         if (!b.isKinematic) b.velocity += normal * impactSpeed * bShare;
//
//         // Notify both sides
//         a.OnImpact(impactSpeed, b);
//         b.OnImpact(impactSpeed, a);
//     }
//
//     void WrapPosition(PhysicsBody b)
//     {
//         Camera cam   = Camera.main;
//         float width  = cam.orthographicSize * 2f * cam.aspect;
//         float halfW  = width * 0.5f;
//         float camX   = cam.transform.position.x;
//
//         if (b.candidatePos.x > camX + halfW) b.candidatePos.x -= width;
//         else if (b.candidatePos.x < camX - halfW) b.candidatePos.x += width;
//     }
// }


// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
//
// public class Player : PhysicsBody
// {
//
//     // --- Movement ---
//     [SerializeField] private float moveSpeed      = 9f;
//     [SerializeField] private float groundAccel    = 100f;
//     [SerializeField] private float airAccel       = 65f;
//     [SerializeField] private float groundFriction = 80f;
//     [SerializeField] private float airFriction    = 40f;
//
//     // --- Jump ---
//     [SerializeField] private float jumpForce         = 11f;
//     [SerializeField] private float jumpCutMultiplier = 0.5f;
//     [SerializeField] private float coyoteTime        = 0.15f;
//     [SerializeField] private float jumpBufferTime    = 0.2f;
//
//     // --- Wall jump ---
//     [SerializeField] private float wallJumpHSpeed = 13f;
//     [SerializeField] private float wallJumpVSpeed = 11f;
//     [SerializeField] private float wallCheckDist  = 0.08f;
//     [SerializeField] private float wallCoyoteTime = 0.1f;
//
//     // --- Gravity ---
//     // gravity and maxFallSpeed inherited from PhysicsBody
//     [SerializeField] private float fastFallGravity = 40f;
//     [SerializeField] private float maxFastFall     = 24f;
//     [SerializeField] private float gravity     = 28f;
//     [SerializeField] private float maxFallSpeed = 16f;
//
//     // --- Dash ---
//     [SerializeField] private float dashSpeed    = 24f;
//     [SerializeField] private float dashDuration = 0.25f;
//     [SerializeField] private float dashEndHCap  = 20f;
//
//     // --- Collision trigger ---
//     [SerializeField] private float highCollideVal = 5f;
//     [SerializeField] public  float bounceThreshold = 8f;
//     public event Action<Vector2> onHighCollision;
//
//     // --- State ---
//     private bool  isDashing;
//     private bool  dashUsed;
//     private bool  isJumping;
//     private float dashTimer;
//     private float coyoteTimer;
//     private float jumpBufferTimer;
//     private float lastDir = 1f;  // default facing right
//     private HashSet<Block> currentContacts = new HashSet<Block>();
//     
//     protected override void Awake()
//     {
//         base.Awake();
//         PhysicsManager.Instance.RegisterPlayer(this);
//     }
//
//     public void Step(float dt)
//     {
//         currentContacts.Clear();
//         UpdateTimers(dt);
//         TryDash();
//         ApplyHorizontal(dt);
//         ApplyVertical(dt);
//         candidatePos += velocity * dt;
//         
//         if (Input.GetKey(KeyCode.R))
//         {
//             candidatePos = new Vector2(0f, 3f);
//         }
//
//         if (Input.GetKey(KeyCode.S))
//         {
//             onHighCollision?.Invoke(candidatePos);
//         }
//     }
//
//     public override void OnImpact(float impactSpeed, Block other)
//     {
//         if (impactSpeed > highCollideVal && !currentContacts.Contains(other))
//         {
//             onHighCollision?.Invoke(candidatePos);
//             currentContacts.Add(other);
//         }
//     }
//
//     public override void ApplyImpulse(Vector2 impulse)
//     {
//         velocity += impulse;
//         receivedImpulseThisFrame = true;
//         if (impulse.y < 0f) isJumping = false;
//     }
//
//     // -------------------------------------------------------------------------
//     // Timers
//     // -------------------------------------------------------------------------
//
//     void UpdateTimers(float dt)
//     {
//         if (Input.GetKeyDown(KeyCode.Z))
//             jumpBufferTimer = jumpBufferTime;
//         else
//             jumpBufferTimer -= dt;
//
//         if (onGround)
//         {
//             coyoteTimer = coyoteTime;
//             dashUsed    = false;
//         }
//         else
//             coyoteTimer -= dt;
//
//         if (isDashing)
//         {
//             dashTimer -= dt;
//             if (dashTimer <= 0f)
//                 EndDash();
//         }
//     }
//
//     // -------------------------------------------------------------------------
//     // Dash
//     // -------------------------------------------------------------------------
//
//     void TryDash()
//     {
//         if (!Input.GetKeyDown(KeyCode.X) || isDashing || dashUsed) return;
//
//         // Read 8-directional input from arrow keys or WASD
//         float x = 0f, y = 0f;
//         if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
//         if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  x -= 1f;
//         if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    y += 1f;
//         if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  y -= 1f;
//
//         // Default to last horizontal direction if no input
//         if (x == 0f && y == 0f)
//             x = lastDir;
//
//         Vector2 dir = new Vector2(x, y).normalized;
//
//         // Skew diagonal to favour horizontal
//         dir.y   *= 0.75f;
//         velocity = dir.normalized * dashSpeed;
//
//         isDashing = true;
//         dashUsed  = true;
//         dashTimer = dashDuration;
//         isJumping = false;
//     }
//
//     void EndDash()
//     {
//         isDashing  = false;
//         velocity.x = Mathf.Clamp(velocity.x, -dashEndHCap, dashEndHCap);
//         if (velocity.y > 0f) velocity.y = 0f;
//     }
//     void ApplyHorizontal(float dt)
//     {
//         if (isDashing) return;
//
//         float input = 0f;
//         if (Input.GetKey(KeyCode.LeftArrow)) input -= 1f;
//         if (Input.GetKey(KeyCode.RightArrow)) input += 1f;
//
//         if (input != 0f)
//         {
//             lastDir    = Mathf.Sign(input);
//             float accel  = onGround ? groundAccel : airAccel;
//             velocity.x   = Mathf.MoveTowards(velocity.x, input * moveSpeed, accel * dt);
//         }
//         else
//         {
//             float friction = onGround ? groundFriction : airFriction;
//             velocity.x     = Mathf.MoveTowards(velocity.x, 0f, friction * dt);
//         }
//     }
//
//     // -------------------------------------------------------------------------
//     // Vertical
//     // -------------------------------------------------------------------------
//
//     void ApplyVertical(float dt)
//     {
//         if (isDashing && !onGround) return;
//
//         if (jumpBufferTimer > 0f && coyoteTimer > 0f)
//         {
//             velocity.y      = jumpForce;
//             jumpBufferTimer = 0f;
//             coyoteTimer     = 0f;
//             isJumping       = true;
//         }
//
//         if (isJumping && Input.GetKeyUp(KeyCode.Z) && velocity.y > 0f)
//         {
//             velocity.y *= jumpCutMultiplier;
//             isJumping   = false;
//         }
//
//         bool  fastFall = Input.GetKey(KeyCode.DownArrow) && velocity.y < 0f;
//         float grav     = fastFall ? fastFallGravity : gravity;
//         float cap      = fastFall ? maxFastFall     : maxFallSpeed;
//
//         velocity.y -= grav * dt;
//         if (velocity.y < -cap) velocity.y = -cap;
//     }
//     
//     // -------------------------------------------------------------------------
//     // Public data
//     // -------------------------------------------------------------------------
//     public bool IsDashing => isDashing;
// }
