using UnityEngine;

/// <summary>
/// SurvivorCamera가 플레이어를 부드럽게 추적합니다.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float     _smoothTime = 0.1f;

    private Vector3 _velocity;

    public void SetTarget(Transform target) => _target = target;

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 goal = new Vector3(_target.position.x, _target.position.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref _velocity, _smoothTime);
    }
}
