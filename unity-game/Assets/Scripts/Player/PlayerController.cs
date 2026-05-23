using UnityEngine;

/// <summary>
/// 玩家小猫控制器 — Undertale 像素RPG风格
/// 支持键盘 WASD + 虚拟摇杆（移动端）
/// 像素移动（4方向）+ ♥ 灵魂之心标记
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    #region 属性
    [Header("移动")]
    public float moveSpeed = 5f;
    public Vector2 mapBounds = new(50f, 50f);

    [Header("发现设置")]
    public float discoveryRadius = 3f;
    public LayerMask catSpotLayer;

    [Header("精灵动画 — 像素4方向")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] idleSprites;      // 0=down, 1=up, 2=left, 3=right
    public Sprite[] walkSprites;      // 同上排列 × 每向4帧 = 16张

    [Header("Undertale 灵魂之心")]
    public GameObject heartSoul;       // ♥ 红色灵魂标记（跟随玩家）
    #endregion

    #region 私有
    Rigidbody2D _rb;
    Vector2 _moveInput;
    Vector2 _joystickInput;
    bool _joystickActive;
    bool _isMoving;
    string _facingDir = "down";
    string _nearbyCatId;
    float _animTimer;
    int _animFrame;
    #endregion

    #region 生命周期
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0; // 2D 俯视图不需要重力
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        // 像素渲染
        if (spriteRenderer && spriteRenderer.sprite)
            spriteRenderer.sprite.texture.filterMode = FilterMode.Point;
    }

    void Update()
    {
        HandleInput();
        UpdateAnimation();
        CheckCatProximity();
        UpdateHeartSoul();
    }

    void FixedUpdate()
    {
        Vector2 input = _joystickActive ? _joystickInput : _moveInput;
        if (input.magnitude > 1f) input.Normalize();

        _isMoving = input.magnitude > 0.05f;
        if (_isMoving)
        {
            // Undertale 风格：像素化移动方向（优先4方向）
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                _facingDir = input.x > 0 ? "right" : "left";
            else
                _facingDir = input.y > 0 ? "up" : "down";

            _rb.linearVelocity = input * moveSpeed;
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
        }

        // 边界限制
        var pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, 0, mapBounds.x);
        pos.y = Mathf.Clamp(pos.y, 0, mapBounds.y);
        transform.position = pos;
    }
    #endregion

    #region 灵魂之心
    void UpdateHeartSoul()
    {
        if (!heartSoul) return;
        // ♥ 跟随玩家，带微小漂浮
        heartSoul.transform.position = transform.position + Vector3.up * 1.5f;
        float floatOffset = Mathf.Sin(Time.time * 3f) * 0.15f;
        heartSoul.transform.localPosition += Vector3.up * floatOffset;
    }
    #endregion

    #region 输入处理
    void HandleInput()
    {
        _moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
    }

    // 虚拟摇杆（移动端）- 由 UI 调用
    public void OnJoystickStart(Vector2 center) { _joystickActive = true; }
    public void OnJoystickMove(Vector2 input) => _joystickInput = input;
    public void OnJoystickEnd() { _joystickActive = false; _joystickInput = Vector2.zero; }
    #endregion

    #region 猫咪检测
    void CheckCatProximity()
    {
        // 用物理重叠圆检测附近的猫咪出没点
        var hit = Physics2D.OverlapCircle(transform.position, discoveryRadius, catSpotLayer);
        var newCatId = hit?.GetComponent<CatSpotMarker>()?.catId;

        if (newCatId != _nearbyCatId)
        {
            if (!string.IsNullOrEmpty(_nearbyCatId))
                GameManager.Instance?.TriggerLeaveCat();
            if (!string.IsNullOrEmpty(newCatId))
                GameManager.Instance?.TriggerNearby(newCatId);
            _nearbyCatId = newCatId;
        }
    }

    public string NearbyCatId => _nearbyCatId;

    public void InteractWithCat()
    {
        if (!string.IsNullOrEmpty(_nearbyCatId))
        {
            GameManager.Instance?.DiscoverCat(_nearbyCatId);
            GameManager.Instance?.TriggerInteract(_nearbyCatId);
        }
    }
    #endregion

    #region 动画 — 像素4方向逐帧
    void UpdateAnimation()
    {
        if (!spriteRenderer) return;

        _animTimer += Time.deltaTime;
        if (_animTimer > 0.15f && _isMoving)
        {
            _animTimer = 0;
            _animFrame = (_animFrame + 1) % 4;
        }
        if (!_isMoving) { _animFrame = 0; _animTimer = 0; }

        int dirIndex = _facingDir switch
        {
            "down" => 0, "up" => 1, "left" => 2, "right" => 3, _ => 0
        };

        if (_isMoving && walkSprites.Length > dirIndex * 4 + _animFrame)
            spriteRenderer.sprite = walkSprites[dirIndex * 4 + _animFrame];
        else if (idleSprites.Length > dirIndex)
            spriteRenderer.sprite = idleSprites[dirIndex];
    }
    #endregion
}

/// <summary>
/// 猫咪出没点标记组件（挂载到地图猫点触发器上）
/// </summary>
public class CatSpotMarker : MonoBehaviour
{
    public string catId;
}
