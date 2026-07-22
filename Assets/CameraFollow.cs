using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;

    [SerializeField] private float yPosition = 0f;
    [SerializeField] private float zPosition = -10f;
    [SerializeField] private float followSpeed = 5f;

    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX = 50f;

    private void LateUpdate()
    {
        float x = Mathf.Clamp(player.position.x, minX, maxX);

        Vector3 target = new Vector3(x, yPosition, zPosition);

        transform.position = Vector3.Lerp(
            transform.position,
            target,
            followSpeed * Time.deltaTime
        );
    }
}