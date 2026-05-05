using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovement : PhysicsBody
{
    // --- Movement ---
    [SerializeField] private float moveSpeed      = 9f;
    [SerializeField] private float groundAccel    = 100f;
    [SerializeField] private float airAccel       = 65f;
    [SerializeField] private float groundFriction = 80f;
    [SerializeField] private float airFriction    = 40f;

    // --- Jump ---
    [SerializeField] private float jumpForce         = 11f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime        = 0.15f;
    [SerializeField] private float jumpBufferTime    = 0.2f;

    // --- Wall jump ---
    [SerializeField] private float wallJumpHSpeed = 13f;
    [SerializeField] private float wallJumpVSpeed = 11f;
    [SerializeField] private float wallCheckDist  = 0.08f;
    [SerializeField] private float wallCoyoteTime = 0.1f;

    // --- Gravity ---
    // gravity and maxFallSpeed inherited from PhysicsBody
    [SerializeField] private float fastFallGravity = 40f;
    [SerializeField] private float maxFastFall     = 24f;

    // --- Dash ---
    [SerializeField] private float dashSpeed    = 24f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashEndHCap  = 20f;

    // --- Collision trigger ---
    [SerializeField] private float highCollideVal = 5f;
    public event Action<Vector2> onHighCollision;

    // --- State ---
    private bool  isDashing;
    private bool  dashUsed;
    private bool  isJumping;
    private float dashTimer;
    private float coyoteTimer;
    private float jumpBufferTimer;

    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        velocity = Vector2.zero;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        UpdateTimers(dt);
        TryDash();
        ApplyHorizontal(dt);
        ApplyVertical(dt);
        MoveAndCollide(dt);
    }

    // -------------------------------------------------------------------------
    // Timers
    // -------------------------------------------------------------------------

    void UpdateTimers(float dt)
    {
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= dt;

        if (onGround)
        {
            coyoteTimer = coyoteTime;
            dashUsed    = false;
        }
        else
            coyoteTimer -= dt;

        if (isDashing)
        {
            dashTimer -= dt;
            if (dashTimer <= 0f)
                EndDash();
        }
    }

    // -------------------------------------------------------------------------
    // Dash
    // -------------------------------------------------------------------------

    void TryDash()
    {
        if (!Input.GetMouseButtonDown(0) || isDashing || dashUsed) return;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir        = mouseWorld - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.001f) return;

        velocity  = dir.normalized * dashSpeed;
        isDashing = true;
        dashUsed  = true;
        dashTimer = dashDuration;
        isJumping = false;
    }

    void EndDash()
    {
        isDashing  = false;
        velocity.x = Mathf.Clamp(velocity.x, -dashEndHCap, dashEndHCap);
        if (velocity.y > 0f) velocity.y = 0f;
    }

    // -------------------------------------------------------------------------
    // Horizontal
    // -------------------------------------------------------------------------

    void ApplyHorizontal(float dt)
    {
        if (isDashing) return;

        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input -= 1f;
        if (Input.GetKey(KeyCode.D)) input += 1f;

        if (input != 0f)
        {
            float accel  = onGround ? groundAccel : airAccel;
            velocity.x   = Mathf.MoveTowards(velocity.x, input * moveSpeed, accel * dt);
        }
        else
        {
            float friction = onGround ? groundFriction : airFriction;
            velocity.x     = Mathf.MoveTowards(velocity.x, 0f, friction * dt);
        }
    }

    // -------------------------------------------------------------------------
    // Vertical
    // -------------------------------------------------------------------------

    void ApplyVertical(float dt)
    {
        if (isDashing && !onGround) return;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y      = jumpForce;
            jumpBufferTimer = 0f;
            coyoteTimer     = 0f;
            isJumping       = true;
        }

        if (isJumping && Input.GetKeyUp(KeyCode.Space) && velocity.y > 0f)
        {
            velocity.y *= jumpCutMultiplier;
            isJumping   = false;
        }

        bool  fastFall = Input.GetKey(KeyCode.S) && velocity.y < 0f;
        float grav     = fastFall ? fastFallGravity : gravity;
        float cap      = fastFall ? maxFastFall     : maxFallSpeed;

        velocity.y -= grav * dt;
        if (velocity.y < -cap) velocity.y = -cap;
    }

    // -------------------------------------------------------------------------
    // Move & collide
    // -------------------------------------------------------------------------

    void MoveAndCollide(float dt)
    {
        Vector2 pos = transform.position;

        MoveX(ref pos, velocity.x * dt);
        MoveY(ref pos, velocity.y * dt);

        bool wasOnGround = onGround;
        onGround = CheckGround(pos);

        if (onGround && velocity.y < 0f)
        {
            velocity.y = 0f;
            isJumping  = false;

            if (isDashing)
            {
                EndDash();
                dashUsed = false;
            }
        }

        if (wasOnGround && !onGround && !isDashing)
            coyoteTimer = coyoteTime;

        WrapPosition(ref pos);
        transform.position = pos;
    }

    // -------------------------------------------------------------------------
    // Impact callback from base class
    // -------------------------------------------------------------------------

    protected override void OnImpact(float impactSpeed, Collider2D other)
    {
        if (impactSpeed > highCollideVal)
            onHighCollision?.Invoke(transform.position);
    }

    public override void ApplyImpulse(Vector2 impulse)
    {
        base.ApplyImpulse(impulse);
        isJumping = false;
    }

    // -------------------------------------------------------------------------
    // Public data
    // -------------------------------------------------------------------------
    public bool IsDashing => isDashing;

    int CheckWall(Vector2 pos)
    {
        if (Physics2D.OverlapBox(pos + Vector2.right * wallCheckDist, colliderSize, 0f, groundLayer) != null) return  1;
        if (Physics2D.OverlapBox(pos + Vector2.left  * wallCheckDist, colliderSize, 0f, groundLayer) != null) return -1;
        return 0;
    }
}