using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AfterImage : MonoBehaviour
{
    private SpriteRenderer sr;
    private float lifetime;
    private float timer;

    public void Init(Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale, Color color, float lifetime)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        sr.sprite = sprite;
        sr.color = color;

        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;

        this.lifetime = lifetime;
        timer = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifetime;

        // Fade alpha
        Color c = sr.color;
        c.a = Mathf.Lerp(1f, 0f, t);
        sr.color = c;

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}