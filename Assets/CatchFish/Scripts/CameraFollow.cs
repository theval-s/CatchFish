using UnityEngine;

/// <summary>Smoothly moves the camera vertically to <see cref="TargetY"/>.</summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float smoothTime = 0.15f;

    private float _velocity;

    public float TargetY { get; set; }

    private void LateUpdate()
    {
        var position = transform.position;
        position.y = Mathf.SmoothDamp(position.y, TargetY, ref _velocity, smoothTime);
        transform.position = position;
    }
}
