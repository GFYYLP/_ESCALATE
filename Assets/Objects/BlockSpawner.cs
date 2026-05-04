using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;

    [SerializeField] private Block blockPrefab;

    [SerializeField] private List<Sprite> sprites;


    // Start is called before the first frame update
    void Start()
    {
        player.onHighCollision += spawnBlock;
    }

    void spawnBlock()
    {
        var block = Instantiate(
            blockPrefab,
            new Vector3(player.CollisionPos, transform.position.y, 0f),
            Quaternion.identity
        );
        block.gameObject.layer = LayerMask.NameToLayer("Ground");
        block.sr.sprite = sprites[Random.Range(0, sprites.Count)];
    }

    // Update is called once per frame
    void Update()
    {

    }
}