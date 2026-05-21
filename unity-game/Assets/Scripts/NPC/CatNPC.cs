using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// 猫咪 NPC - 地图上的猫咪角色
/// 待机动画 + 被靠近反应 + 点击互动
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class CatNPC : MonoBehaviour
{
    #region 属性
    [Header("数据")]
    public string catId;
    public bool showNameLabel = true;

    [Header("动画")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] idleFrames;
    public float idleFrameRate = 0.5f;

    [Header("特效")]
    public GameObject discoveryGlow;      // 未发现的脉冲光环
    public GameObject heartParticle;      // 被点击时的爱心粒子
    public GameObject exclamationMark;    // 玩家靠近时的感叹号
    #endregion

    #region 私有
    CatData _data;
    bool _playerNearby;
    float _animTimer;
    int _animFrame;
    Vector3 _originalScale;
    string _idleAction = "sitting";
    float _actionTimer;
    #endregion

    #region 生命周期
    void Start()
    {
        _originalScale = transform.localScale;
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        LoadData();
        RandomizeIdleAction();

        GameManager.Instance.OnCatNearby += OnPlayerApproach;
        GameManager.Instance.OnCatLeave += OnPlayerLeave;
    }

    void OnDestroy()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnCatNearby -= OnPlayerApproach;
            GameManager.Instance.OnCatLeave -= OnPlayerLeave;
        }
    }

    void Update()
    {
        UpdateIdleAnimation();
        UpdateDiscoveryGlow();
    }

    void LoadData()
    {
        _data = GameManager.Instance?.GetCatById(catId);
        if (_data != null && !string.IsNullOrEmpty(_data.photoUrl))
            StartCoroutine(LoadPhoto(_data.photoUrl));
    }

    IEnumerator LoadPhoto(string url)
    {
        using var www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.Success)
        {
            var texture = DownloadHandlerTexture.GetContent(www);
            var sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            spriteRenderer.sprite = sprite;
        }
    }
    #endregion

    #region 交互
    void OnMouseDown()
    {
        if (!_playerNearby)
        {
            GameManager.Instance?.TriggerToast("离得太远啦~走近一点互动吧！");
            return;
        }
        GameManager.Instance?.DiscoverCat(catId);
        GameManager.Instance?.TriggerInteract(catId);
        PlayClickAnimation();
    }

    void OnPlayerApproach(string id)
    {
        if (id != catId) return;
        _playerNearby = true;
        PlayNoticeAnimation();
    }

    void OnPlayerLeave()
    {
        _playerNearby = false;
        if (exclamationMark) exclamationMark.SetActive(false);
    }
    #endregion

    #region 动画
    void RandomizeIdleAction()
    {
        string[] actions = { "sitting", "licking", "sleeping", "stretching" };
        _idleAction = actions[Random.Range(0, actions.Length)];
        _actionTimer = 0;
    }

    void UpdateIdleAnimation()
    {
        _actionTimer += Time.deltaTime;
        float maxTime = _idleAction switch
        {
            "sitting" => 4f, "licking" => 2f,
            "sleeping" => 8f, "stretching" => 1.5f, _ => 3f
        };

        if (_actionTimer > maxTime + Random.Range(0f, 3f))
            RandomizeIdleAction();

        // 帧动画
        _animTimer += Time.deltaTime;
        if (_animTimer > idleFrameRate && idleFrames.Length > 0)
        {
            _animTimer = 0;
            _animFrame = (_animFrame + 1) % idleFrames.Length;
            spriteRenderer.sprite = idleFrames[_animFrame];
        }

        // 睡觉时的呼吸起伏
        if (_idleAction == "sleeping")
        {
            float breathe = 1f + Mathf.Sin(Time.time * 2f) * 0.03f;
            transform.localScale = new Vector3(breathe, breathe, 1);
        }
    }

    void PlayNoticeAnimation()
    {
        if (exclamationMark) exclamationMark.SetActive(true);
        // 弹跳效果
        StopAllCoroutines();
        StartCoroutine(BounceAnim());
    }

    IEnumerator BounceAnim()
    {
        float t = 0;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * Mathf.PI / 0.3f) * 0.1f;
            transform.localScale = _originalScale * s;
            yield return null;
        }
        transform.localScale = _originalScale;
    }

    void PlayClickAnimation()
    {
        if (heartParticle)
        {
            var heart = Instantiate(heartParticle, transform.position + Vector3.up, Quaternion.identity);
            Destroy(heart, 2f);
        }
        StopAllCoroutines();
        StartCoroutine(ClickBounce());
    }

    IEnumerator ClickBounce()
    {
        for (int i = 0; i < 3; i++)
        {
            transform.localScale = _originalScale * 1.3f;
            yield return new WaitForSeconds(0.08f);
            transform.localScale = _originalScale * 0.8f;
            yield return new WaitForSeconds(0.08f);
        }
        transform.localScale = _originalScale;
    }

    void UpdateDiscoveryGlow()
    {
        if (!discoveryGlow) return;
        bool discovered = GameManager.Instance?.IsDiscovered(catId) ?? false;
        discoveryGlow.SetActive(!discovered);
        if (!discovered)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.2f;
            discoveryGlow.transform.localScale = Vector3.one * pulse;
        }
    }
    #endregion
}
