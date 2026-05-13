using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : PhysicsBody
{
    [SerializeField] private float gravity     = 10f;
    [SerializeField] private float maxFallSpeed = 16f;
    public SpriteRenderer sr;

    protected override void OnEnable()
    {
        base.OnEnable();
        sr = GetComponent<SpriteRenderer>();
    }

    public override void UpdateVelocity(float dt)
    {
        if (!isKinematic)
        {
            velocity.y -= gravity * dt;
            velocity.y  = Mathf.Max(velocity.y, -maxFallSpeed);
        }

        if (candidatePos.y < -10f && velocity.y <= maxFallSpeed) pendingDestroy = true;
    }
}