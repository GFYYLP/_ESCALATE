using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class a : MonoBehaviour
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
    // No special bounce state — just a strong horizontal kick on jump near wall.
    [SerializeField] private float wallJumpHSpeed    = 13f;
    [SerializeField] private float wallJumpVSpeed    = 11f;
    [SerializeField] private float wallCheckDist     = 0.08f;
    [SerializeField] private float wallCoyoteTime    = 0.1f;

    // --- Gravity ---
    [SerializeField] private float gravity         = 28f;
    [SerializeField] private float maxFallSpeed    = 16f;
    [SerializeField] private float fastFallGravity = 40f;
    [SerializeField] private float maxFastFall     = 24f;

    // --- Dash ---
    // Horizontal speed is never zeroed on landing — only downward velocity is.
    // This means a horizontal dash that lands gives you full dash speed carried forward,
    // which is what makes wavedash/hyperdash emerge without being explicitly coded.
    [SerializeField] private float dashSpeed      = 24f;
    [SerializeField] private float dashDuration   = 0.25f;
    // On dash end, horizontal speed decays toward moveSpeed naturally via friction.
    // We only hard-cap it to prevent indefinite hyperspeed.
    [SerializeField] private float dashEndHCap    = 20f;

    // --- Collision ---
    [SerializeField] private Vector2 colliderSize = new Vector2(0.9f, 1.8f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float stepHeight  = 0.15f;
    [SerializeField] private float groundProbe = 0.08f;

    // --- State ---
    private Vector2 velocity;
    private bool    onGround;
    private bool    isDashing;
    private bool    dashUsed;
    private bool    isJumping;
    private float   dashTimer;
    private float   coyoteTimer;
    private float   jumpBufferTimer;

    // Wall state
    private int     wallDir;         // -1, 0, +1
    private int     lastWallDir;
    private float   wallCoyoteTimer;

    // -------------------------------------------------------------------------
    void Start()
    {
        velocity = Vector2.zero;
    }

    public float Speed      => velocity.magnitude;
    public bool IsDashing   => isDashing;

    void Update()
    {
        float dt = Time.deltaTime;

        // Ground/wall state is already set from last frame's MoveAndCollide.
        // This means friction and jump both see the correct grounded state
        // without waiting an extra frame.
        UpdateTimers(dt);
        TryDash();
        ApplyHorizontal(dt);   // friction runs here using last frame's onGround
        ApplyVertical(dt);     // jump fires here — before this frame's landing
        MoveAndCollide(dt);    // landing updates onGround for next frame
    }

    // -------------------------------------------------------------------------
    // Collision helpers
    // -------------------------------------------------------------------------

    bool Overlaps(Vector2 pos)
    {
        return Physics2D.OverlapBox(pos, colliderSize, 0f, groundLayer) != null;
    }

    bool CheckGround(Vector2 pos)
    {
        return Physics2D.OverlapBox(pos + Vector2.down * groundProbe, colliderSize, 0f, groundLayer) != null;
    }

    int CheckWall(Vector2 pos)
    {
        if (Physics2D.OverlapBox(pos + Vector2.right * wallCheckDist, colliderSize, 0f, groundLayer) != null) return  1;
        if (Physics2D.OverlapBox(pos + Vector2.left  * wallCheckDist, colliderSize, 0f, groundLayer) != null) return -1;
        return 0;
    }

    void WrapPosition()
    {
        Camera cam = Camera.main;

        float height = cam.orthographicSize * 2f;  //*2 to get full height from orthographicSize, which is half-height
        float width = height * cam.aspect;  // width is derived from height and aspect ratio to maintain correct proportions regardless of screen size

        Vector2 center = cam.transform.position;
        float left = center.x - width / 2f;
        float right = center.x + width / 2f;

        Vector3 pos = transform.position;
        if (pos.x > right)
            pos.x = left;
        else if (pos.x < left)
            pos.x = right;

        transform.position = pos;
    }

    // -------------------------------------------------------------------------
    // Timers
    // -------------------------------------------------------------------------

    void UpdateTimers(float dt)
    {
        //register jump input, count down if not used this frame
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= dt;

        // Refresh coyote while grounded, count down when airborne
        if (onGround)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= dt;

        if (onGround)
            dashUsed = false;

        if (wallDir != 0)
        {
            wallCoyoteTimer = wallCoyoteTime;
            lastWallDir     = wallDir;
        }
        else
        {
            wallCoyoteTimer -= dt;
        }

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
        isDashing = false;

        // Cap horizontal speed but don't zero it — retained speed is what
        // makes wavedash/hyperdash emerge on landing.
        velocity.x = Mathf.Clamp(velocity.x, -dashEndHCap, dashEndHCap);

        // Kill upward velocity so you don't float after an upward dash,
        // but preserve downward so diagonal-down dashes keep their arc.
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
            float target = input * moveSpeed;


            //if we're moving faster than our target in the same direction, apply friction instead of acceleration to avoid overshooting and oscillating around the target speed
            // if (Mathf.Sign(velocity.x) == Mathf.Sign(input) && Mathf.Abs(velocity.x) > Mathf.Abs(target))
            //     velocity.x = Mathf.MoveTowards(velocity.x, target, (onGround ? groundFriction : airFriction) * dt);
            // else
                velocity.x = Mathf.MoveTowards(velocity.x, target, accel * dt);
        }
        else
        {
            float friction = onGround ? groundFriction : airFriction;

            // Apply friction toward zero when there's no input
            // enabling things like wavedash by dashing, then holding opposite direction to slow down quickly on landing.
            velocity.x     = Mathf.MoveTowards(velocity.x, 0f, friction * dt);
        }
    }

    // -------------------------------------------------------------------------
    // Vertical — wall jump lives here, no special state needed
    // -------------------------------------------------------------------------

    void ApplyVertical(float dt)
    {
        if (isDashing) return;

        if (jumpBufferTimer > 0f)
        {
            // Wall jump — airborne, near a wall, not on ground
            // bool nearWall = wallDir != 0 || wallCoyoteTimer > 0f;
            // if (!onGround && nearWall)
            // {
            //     int pushDir    = wallDir != 0 ? -wallDir : -lastWallDir;
            //     velocity.x     += pushDir * wallJumpHSpeed;
            //     velocity.y     += wallJumpVSpeed;
            //     jumpBufferTimer = 0f;
            //     wallCoyoteTimer = 0f;
            //     isJumping       = true;
            //     // Refund dash — in Celeste wall jumps refund your dash
            //     dashUsed        = false;
            //     return;
            // }

            // Normal jump
            if (coyoteTimer > 0f)
            {
                velocity.y      += jumpForce;

                // Reset timers and flags so that you can hold jump and have it cut off at the right height, 
                // and so that you can buffer another jump immediately after this one if you want to chain them.
                jumpBufferTimer = 0f;
                coyoteTimer     = 0f;
                isJumping       = true;
            }
        }

        // Variable jump height
        if (isJumping && Input.GetKeyUp(KeyCode.Space) && velocity.y > 0f)
        {
            velocity.y *= jumpCutMultiplier;
            isJumping   = false;
        }

        // Gravity
        bool fastFall  = Input.GetKey(KeyCode.S) && velocity.y < 0f;
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

        // Don't reset onGround here — it's needed by ApplyHorizontal/ApplyVertical
        // earlier in the frame. Instead just re-derive it after moving.
        //wallDir = 0;

        MoveX(ref pos, velocity.x * dt);
        MoveY(ref pos, velocity.y * dt);

        bool wasOnGround = onGround;
        onGround = CheckGround(pos);
        //wallDir  = CheckWall(pos);

        // Landing resets vertical velocity and state, but doesn't zero horizontal 
        // this allows for things like wavedash where you retain horizontal speed after a dash when you land.
        if (onGround && velocity.y < 0f)
        {
            //if (isDashing) EndDash();
            velocity.y = 0f;
            isJumping  = false;
    // immediately allow jump this frame
    if (jumpBufferTimer > 0f)
    {
        velocity.y = jumpForce;
        jumpBufferTimer = 0f;
    }

    // This is the only thing missing:
    // if (isDashing)
    // {
    //     EndDash();
    //     dashUsed = false;   // refund so the next dash is available mid-air after the jump
    // }
        }

        // If we walked off a ledge this frame, start coyote from here
        if (wasOnGround && !onGround && !isDashing)
            coyoteTimer = coyoteTime;

        transform.position = pos;
    }

    void MoveX(ref Vector2 pos, float amount)
    {
        if (amount == 0f) return;
        pos.x += amount;
        if (!Overlaps(pos)) return;

        // bool resolved = false;
        // float stepInc = stepHeight / 5f;

        // If we hit a horizontal obstacle, try stepping up in increments to see if we can "climb" it.
        // for (float offset = stepInc; offset <= stepHeight; offset += stepInc)
        // {
        //     if (!Overlaps(pos + Vector2.up * offset))
        //     {
        //         pos.y   += offset;
        //         resolved = true;
        //         break;
        //     }
        // }

        // // If we couldn't resolve the collision by stepping up, step back and zero horizontal velocity.
        // if (!resolved)
        // {
            pos.x      -= amount;
            velocity.x  = 0f;
        //}

        WrapPosition(); //warp on world borders
    }

    void MoveY(ref Vector2 pos, float amount)
    {
        if (amount == 0f) return;
        pos.y += amount;
        if (!Overlaps(pos)) return;

        pos.y      -= amount;
        velocity.y  = 0f;
    }
}