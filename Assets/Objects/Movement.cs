using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movemenbtr2p31ldegt22q1swdcv : MonoBehaviour
{
    private Vector2 velocity;
    private bool onGround, isDashing;
    private float dashTimer, coyoteTimer, jumpBufferTimer;

    void updateTimers(float dt){
        //jump input buffer for better input responsiveness
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferTimer = 0.1f;
        else
            jumpBufferTimer -= dt;

        //ledge-jump responsiveness
        if (onGround)
            coyoteTimer = 0.1f;
        else
            coyoteTimer -= dt;

        //dash uptime
        if (isDashing){
            dashTimer -= dt;
            if (dashTimer <= 0)
                isDashing = false;
        }
    }

    void applyHorizontal(float dt)
    {
        //disables horizontal movement during dash
        if (isDashing) return;

        float input = 0;
        if (Input.GetKey(KeyCode.A)) input -= 1;
        if (Input.GetKey(KeyCode.D)) input += 1;

        float target = input * 6f;
        float accel = onGround ? 60f : 30f;

        //move velocity towards target at accel rate, multiplied by delta time for frame rate independence
        velocity.x = Mathf.MoveTowards(velocity.x, target, accel * dt);
    }

    void applyVertical(float dt)
    {
        //apply gravity
        if (!isDashing){
            velocity.y -= 20f * dt;

            //cap velocity
            if (velocity.y < -15f) velocity.y = -15f;
        }

        //jump
        if (jumpBufferTimer > 0 && coyoteTimer > 0)
        {
            velocity.y = 10f;

            //resets timers on jump register
            jumpBufferTimer = 0;
            coyoteTimer = 0;
        }
    }

    void applyDash()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isDashing)
        {
Vector2 normalized = new Vector2(
    Input.mousePosition.x / Screen.width - 0.5f,
    Input.mousePosition.y / Screen.height - 0.5f
);
            Vector2 dir = (Vector2)normalized - (Vector2)transform.position;
            dir.Normalize();

            velocity = dir * 12f;

            isDashing = true;
            dashTimer = 0.15f;
        }
    }

    void moveAndCollide(float dt)
    {
        Vector2 pos = transform.position;

        // --- MOVE X ---
        pos.x += velocity.x * dt;

        // if (Collides(pos))
        // {
        //     pos.x -= velocity.x * dt;
        //     velocity.x = 0;
        // }

        // --- MOVE Y ---
        pos.y += velocity.y * dt;

        // if (Collides(pos))
        // {
        //     pos.y -= velocity.y * dt;

        //     if (velocity.y < 0)
        //         onGround = true;

        //     velocity.y = 0;
        // }
        // else
        {
            onGround = false;
        }

        transform.position = pos;
    }

    // Update is called once per frame
    void Update(){
        float dt = Time.deltaTime;
        updateTimers(dt);

        applyDash();
        applyHorizontal(dt);
        applyVertical(dt);

        moveAndCollide(dt);
    }

    // Start is called before the first frame update
    void Start()
    {
        velocity = Vector2.zero;
        onGround = false;
        isDashing = false;
        dashTimer = 0f;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
    }
}
