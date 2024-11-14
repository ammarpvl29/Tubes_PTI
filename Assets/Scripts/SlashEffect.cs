using UnityEngine;

public class SlashEffect : MonoBehaviour
{
    public float fadeSpeed = 2f;
    public float moveSpeed = 5f; // How fast the slash moves
    public float moveDistance = 2f; // Total distance to move
    
    private SpriteRenderer spriteRenderer;
    private float startPosX;
    private float traveledDistance = 0f;
    private int direction = 1; // Will be set based on player facing direction

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosX = transform.position.x;
        
        // Set direction based on the scale (assuming it was set by PlayerAttack)
        direction = transform.localScale.x > 0 ? 1 : -1;
    }

    void Update()
    {
        // Move the slash
        if (traveledDistance < moveDistance)
        {
            float movement = moveSpeed * Time.deltaTime * direction;
            transform.Translate(new Vector3(movement, 0, 0));
            traveledDistance += Mathf.Abs(movement);
        }

        // Fade out
        Color color = spriteRenderer.color;
        color.a -= fadeSpeed * Time.deltaTime;
        spriteRenderer.color = color;
    }
}