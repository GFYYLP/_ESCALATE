using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    public static PhysicsManager Instance;
    private List<Block> blocks = new List<Block>();
    private Player player;

    void Awake() => Instance = this;

    public void RegisterBlock(Block b)   => blocks.Add(b);
    public void UnregisterBlock(Block b) => blocks.Remove(b);
    public void RegisterPlayer(Player p) => player = p;

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Step blocks independently
        foreach (var b in blocks) b.Step(dt);

        // Step player
        player.Step(dt);

        // Resolve player against each block
        foreach (var b in blocks)
            if (AABBOverlap(player, b))
                ResolvePlayerBlock(player, b);

        // Ground state — check player against all blocks
        player.onGround = false;
        foreach (var b in blocks)
            if (IsGroundedOn(player, b))
            {
                player.onGround = true;
                break;
            }

        // Zero downward velocity if grounded and not just bounced
        if (player.onGround && player.velocity.y < 0f && !player.receivedImpulseThisFrame)
            player.velocity.y = 0f;

        player.receivedImpulseThisFrame = false;

        // Inherit block velocity on sustained contact
        foreach (var b in blocks)
            HandleSustainedContact(player, b);

        // Commit positions
        foreach (var b in blocks) b.Commit();
        player.Commit();

        // Cleanup
        blocks.RemoveAll(b => {
            if (b.pendingDestroy) { Destroy(b.gameObject); return true; }
            return false;
        });
    }

    bool AABBOverlap(PhysicsBody a, PhysicsBody b)
    {
        Vector2 delta    = a.candidatePos - b.candidatePos;
        float overlapX   = (a.size.x + b.size.x) * 0.5f - Mathf.Abs(delta.x);
        float overlapY   = (a.size.y + b.size.y) * 0.5f - Mathf.Abs(delta.y);
        return overlapX > 0f && overlapY > 0f;
    }

    bool IsGroundedOn(PhysicsBody a, PhysicsBody b)
    {
        const float probe = 0.08f;
        Vector2 delta    = (a.candidatePos + Vector2.down * probe) - b.candidatePos;
        float overlapX   = (a.size.x + b.size.x) * 0.5f - Mathf.Abs(delta.x);
        float overlapY   = (a.size.y + b.size.y) * 0.5f - Mathf.Abs(delta.y);
        // Only grounded if contact is on top face and mostly horizontal overlap
        return overlapX > 0f && overlapY > 0f && delta.y > 0f;
    }

    void ResolvePlayerBlock(Player player, Block block)
    {
        Vector2 delta    = player.candidatePos - block.candidatePos;
        float overlapX   = (player.size.x + block.size.x) * 0.5f - Mathf.Abs(delta.x);
        float overlapY   = (player.size.y + block.size.y) * 0.5f - Mathf.Abs(delta.y);

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

        // Separate positions
        player.candidatePos += normal * penetration * 0.5f;
        block.candidatePos  -= normal * penetration * 0.5f;

        // Velocity exchange
        float playerSpeed = Vector2.Dot(player.velocity, normal);
        float blockSpeed  = Vector2.Dot(block.velocity,  normal);
        float approachSpeed = playerSpeed - blockSpeed;

        if (approachSpeed <= 0f) return;  // already separating

        if (approachSpeed > player.bounceThreshold)
        {
            // Hard impact — wallbounce
            float restitution = 0.6f;
            player.velocity    -= normal * approachSpeed * (1f + restitution) * 0.5f;
            block.ApplyImpulse(normal  * approachSpeed * (1f + restitution) * 0.5f);
            player.receivedImpulseThisFrame = true;
        }
        else
        {
            // Soft contact — will be handled by sustained contact velocity inheritance
            player.velocity   -= normal * approachSpeed * 0.5f;
            block.ApplyImpulse(normal  * approachSpeed * 0.5f);
        }

        player.OnImpact(approachSpeed, block);
    }

    void HandleSustainedContact(Player player, Block block)
    {
        if (!AABBOverlap(player, block)) return;

        Vector2 delta  = player.candidatePos - block.candidatePos;
        float overlapX = (player.size.x + block.size.x) * 0.5f - Mathf.Abs(delta.x);
        float overlapY = (player.size.y + block.size.y) * 0.5f - Mathf.Abs(delta.y));

        // Determine contact face
        if (overlapX < overlapY)
        {
            // Side contact — inherit vertical velocity
            player.velocity.y = Mathf.Lerp(player.velocity.y, block.velocity.y, 0.3f);
        }
        else
        {
            // Top/bottom contact — inherit horizontal velocity
            player.velocity.x = Mathf.Lerp(player.velocity.x, block.velocity.x, 0.3f);
        }
    }
}