using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private float smooth = 0.15f;
    private Transform target;

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = new Vector3(
            target.position.x,
            target.position.y,
            -10f
        );

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            smooth
        );
    }
}
