
using UnityEngine;

public class TopDownRpgCamera : MonoBehaviour
{
    public Transform player;

    [Range(5f, 20f)]
    public float distance = 10f;
    [Range(20f, 80f)]
    public float angle = 45f;
    public float smoothSpeed = 5f;
    public float scrollSpeed = 5f;

    private Vector3 currentRotation;

    void Start()
    {
        currentRotation = new Vector3(angle, 0, 0);
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Zoom in/out
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * scrollSpeed;
        distance = Mathf.Clamp(distance, 5f, 20f);

        // Calculate target position and rotation
        Quaternion rotation = Quaternion.Euler(currentRotation);
        Vector3 targetPosition = player.position - (rotation * Vector3.forward * distance);

        // Smoothly move the camera
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, smoothSpeed * Time.deltaTime);
    }
} 