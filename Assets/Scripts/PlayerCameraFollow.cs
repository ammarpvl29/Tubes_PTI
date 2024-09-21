using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    public float xMin;
    public float xMax;
    public float yMin;
    public float yMax;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (player != null)
        {
            // Calculate the desired position
            float x = Mathf.Clamp(player.position.x + offset.x, xMin, xMax);
            float y = Mathf.Clamp(player.position.y + offset.y, yMin, yMax);
            Vector3 desiredPosition = new Vector3(x, y, transform.position.z);

            // Smoothly move the camera towards the desired position
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
        }
    }
}