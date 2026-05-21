using UnityEngine;

/// <summary>
/// 相机跟随系统 - 平滑跟随玩家小猫
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("目标")]
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new(0, 0, -10);

    [Header("地图边界")]
    public Vector2 mapMin = Vector2.zero;
    public Vector2 mapMax = new(50, 50);

    [Header("缩放")]
    public float minZoom = 2f;
    public float maxZoom = 10f;
    public float zoomSpeed = 0.1f;

    Camera _cam;
    float _targetZoom = 5f;
    Vector2 _lastTouchDist;

    void Start()
    {
        _cam = GetComponent<Camera>();
        if (!target)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player) target = player.transform;
        }
        _cam.orthographicSize = _targetZoom;
    }

    void LateUpdate()
    {
        if (!target) return;

        // 平滑跟随
        Vector3 targetPos = target.position + offset;
        targetPos.z = offset.z;
        transform.position = Vector3.Lerp(transform.position, targetPos,
            smoothSpeed * Time.deltaTime);

        // 边界限制
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;
        var pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, mapMin.x + halfW, mapMax.x - halfW);
        pos.y = Mathf.Clamp(pos.y, mapMin.y + halfH, mapMax.y - halfH);
        transform.position = pos;

        // 滚轮缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            _targetZoom -= scroll * zoomSpeed * 50;
            _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
        }

        // 双指缩放（移动端）
        if (Input.touchCount == 2)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            var dist = Vector2.Distance(t0.position, t1.position);
            if (_lastTouchDist.magnitude > 0)
            {
                float delta = dist / _lastTouchDist.magnitude;
                _targetZoom /= delta;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
            }
            _lastTouchDist = new Vector2(dist, dist);
        }
        else _lastTouchDist = Vector2.zero;

        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize,
            _targetZoom, smoothSpeed * Time.deltaTime);
    }

    /// <summary>聚焦到指定坐标</summary>
    public void FocusOn(Vector2 worldPos)
    {
        transform.position = new Vector3(worldPos.x, worldPos.y, offset.z);
    }
}
