using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : PhysicsBody
{
    public SpriteRenderer sr;

    protected override void Awake()
    {
        base.Awake();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        velocity = Vector2.zero;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        velocity.y -= gravity * dt;
        if (velocity.y < -maxFallSpeed) velocity.y = -maxFallSpeed;

        Vector2 pos = transform.position;
        MoveX(ref pos, velocity.x * dt);
        MoveY(ref pos, velocity.y * dt);

        bool wasOnGround = onGround;
        onGround = CheckGround(pos);
        if (onGround && velocity.y < 0f)
            velocity.y = 0f;

        WrapPosition(ref pos);
        transform.position = pos;

        if (transform.position.y < -10f)
            Destroy(gameObject);
    }
}