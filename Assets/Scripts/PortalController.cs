using UnityEngine;

public class PortalController : MonoBehaviour
{
    public Transform destination;
    GameObject player;

    private void Awake()
    {
        player = GameObject.FindWithTag("Hans");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == player)
        {
            if (Vector2.Distance(player.transform.position, transform.position) > 0.3f)
            {
                player.transform.position = destination.position;
            }
        }
    }
}