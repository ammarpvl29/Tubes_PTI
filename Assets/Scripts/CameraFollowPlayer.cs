using UnityEngine;
using System.Collections.Generic;

public class CameraFollowPlayer : MonoBehaviour
{
    [Header("Following Settings")]
    public Transform target;
    public float followSpeed = 2f;
    public float yOffset = 1f;

    [Header("Camera Settings")]
    public float defaultSize = 5f;
    public float maxSize = 10f;
    public float padding = 2f;
    public float zoomSpeed = 3f;

    [Header("Layer Settings")]
    public LayerMask enemyLayer;
    public float checkRadius = 10f;

    private Camera cam;
    private List<Transform> visibleEnemies = new List<Transform>();
    private Vector3 targetPosition;
    private float targetSize;

    private void Start()
    {
        cam = GetComponent<Camera>();
        targetSize = defaultSize;
    }

    private void LateUpdate()
    {
        UpdateVisibleEnemies();
        UpdateCameraPosition();
        UpdateCameraSize();
    }

    private void UpdateVisibleEnemies()
    {
        visibleEnemies.Clear();
        Collider2D[] enemies = Physics2D.OverlapCircleAll(target.position, checkRadius, enemyLayer);

        foreach (Collider2D enemy in enemies)
        {
            Vector3 viewPos = cam.WorldToViewportPoint(enemy.transform.position);
            if (IsInCameraView(viewPos))
            {
                visibleEnemies.Add(enemy.transform);
            }
        }
    }

    private bool IsInCameraView(Vector3 viewPos)
    {
        return viewPos.x >= -0.1f && viewPos.x <= 1.1f &&
               viewPos.y >= -0.1f && viewPos.y <= 1.1f &&
               viewPos.z > 0;
    }

    private void UpdateCameraPosition()
    {
        if (visibleEnemies.Count > 0)
        {
            Bounds bounds = new Bounds(target.position, Vector3.zero);
            foreach (Transform enemy in visibleEnemies)
            {
                bounds.Encapsulate(enemy.position);
            }

            targetPosition = new Vector3(
                bounds.center.x,
                target.position.y + yOffset,
                transform.position.z
            );
        }
        else
        {
            targetPosition = new Vector3(
                target.position.x,
                target.position.y + yOffset,
                transform.position.z
            );
        }

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );
    }

    private void UpdateCameraSize()
    {
        if (visibleEnemies.Count > 0)
        {
            float maxDistance = 0f;
            foreach (Transform enemy in visibleEnemies)
            {
                float distance = Vector2.Distance(
                    new Vector2(target.position.x, target.position.y),
                    new Vector2(enemy.position.x, enemy.position.y)
                );
                maxDistance = Mathf.Max(maxDistance, distance);
            }

            targetSize = Mathf.Clamp(
                maxDistance / 2f + padding,
                defaultSize,
                maxSize
            );
        }
        else
        {
            targetSize = defaultSize;
        }

        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetSize,
            zoomSpeed * Time.deltaTime
        );
    }
}