using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// 猫咪 NPC — Undertale 像素风格
/// 待机动画 + 像素 ♥ 标记 + 黑色对话框交互
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

    [Header("Undertale 特效")]
    public GameObject discoveryGlow;      // 未发现：白色像素方框脉冲
    public GameObject heartMarker;        // ♥ 红色灵魂标记
    public GameObject exclamationMark;    // 玩家靠近时：白色 "!" 像素文字
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
        // 像素渲染
        if (spriteRenderer) spriteRenderer.sprite.texture.filterMode = FilterMode.Point;
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
            texture.filterMode = FilterMode.Point; // 像素化
            var sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 16);
            spriteRenderer.sprite = sprite;
        }
    }
    #endregion

    #region 交互
    void OnMouseDown()
    {
        if (!_playerNearby)
        {
            GameManager.Instance?.TriggerToast("* 离得太远啦~走近一点互动吧！");
            return;
        }

        // 记录对话 & 好感度
        GameManager.Instance?.RecordTalk(catId);
        GameManager.Instance?.AddFriendship(catId, 1);

        // 优先触发剧情对话
        var storyDialogue = GetStoryDialogue();
        if (storyDialogue != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(storyDialogue);
            return;
        }

        // 否则触发普通猫咪互动
        GameManager.Instance?.DiscoverCat(catId);
        GameManager.Instance?.TriggerInteract(catId);
        PlayClickAnimation();
    }

    /// <summary>
    /// 根据当前章节和好感度获取对应的剧情对话
    /// </summary>
    DialogueScene GetStoryDialogue()
    {
        var gm = GameManager.Instance;
        if (gm == null) return null;

        int chapter = gm.CurrentChapter;
        int friendship = gm.GetFriendship(catId);
        int talks = gm.GetTalkCount(catId);

        // 按优先级检查各章剧情
        // 第0章：与大黄初遇
        if (chapter == 0 && catId == "cat_001" && !gm.CompletedScenes.Contains("ch0_prologue"))
            return gm.LoadDialogueScene("chapter0");

        // 第1章：认识所有猫
        if (chapter == 1 && !gm.CompletedScenes.Contains("ch1_meet_all"))
            return gm.LoadDialogueScene("chapter1");

        // 第3章：冬天剧情（需要先完成第1章且与大黄/小橘好感度够高）
        if (chapter >= 2 && !gm.CompletedScenes.Contains("ch3_winter") &&
            gm.GetFriendship("cat_001") >= 5 && gm.GetFriendship("cat_003") >= 3)
            return gm.LoadDialogueScene("chapter3");

        // 第4章：春天传承（需要完成冬天剧情）
        if (gm.CompletedScenes.Contains("ch3_winter") &&
            !gm.CompletedScenes.Contains("ch4_spring") && catId == "cat_001")
            return gm.LoadDialogueScene("chapter4");

        // 每日对话池（根据好感度返回不同对话）
        return GetDailyDialogue(friendship, talks);
    }

    /// <summary>
    /// 日常对话池（非剧情触发）
    /// </summary>
    DialogueScene GetDailyDialogue(int friendship, int talks)
    {
        var data = _data;
        if (data?.story?.dialogueTree == null) return null;

        var tree = data.story.dialogueTree;
        List<string> pool;

        if (friendship >= 5)
            pool = tree.deep ?? tree.greeting;
        else if (talks % 3 == 0)
            pool = tree.farewell ?? tree.greeting;
        else
            pool = tree.greeting;

        if (pool == null || pool.Count == 0) return null;

        string text = pool[UnityEngine.Random.Range(0, pool.Count)];
        return new DialogueScene
        {
            sceneId = $"daily_{catId}_{talks}",
            chapterId = "daily",
            lines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    type = "dialogue",
                    speaker = data.name,
                    text = text
                }
            }
        };
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

        // 帧动画 — 像素逐帧
        _animTimer += Time.deltaTime;
        if (_animTimer > idleFrameRate && idleFrames.Length > 0)
        {
            _animTimer = 0;
            _animFrame = (_animFrame + 1) % idleFrames.Length;
            spriteRenderer.sprite = idleFrames[_animFrame];
        }

        // 睡觉时的呼吸起伏（像素抖动感）
        if (_idleAction == "sleeping")
        {
            float breathe = 1f + Mathf.Sin(Time.time * 2f) * 0.02f; // 更微小
            transform.localScale = new Vector3(
                Mathf.Round(breathe * 16) / 16f,  // 像素量化
                Mathf.Round(breathe * 16) / 16f,
                1);
        }
    }

    void PlayNoticeAnimation()
    {
        if (exclamationMark) exclamationMark.SetActive(true);
        // Undertale 风格：白色 "!" 弹跳
        StopAllCoroutines();
        StartCoroutine(BounceAnim());
    }

    IEnumerator BounceAnim()
    {
        float t = 0;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * Mathf.PI / 0.3f) * 0.15f;
            // 像素量化缩放
            s = Mathf.Round(s * 8) / 8f;
            transform.localScale = _originalScale * s;
            yield return null;
        }
        transform.localScale = _originalScale;
    }

    void PlayClickAnimation()
    {
        // Undertale 互动：弹出红色像素爱心 ♥
        if (heartMarker)
        {
            var heart = Instantiate(heartMarker, transform.position + Vector3.up * 2f, Quaternion.identity);
            heart.transform.localScale = Vector3.one * 1.5f;
            Destroy(heart, 1.5f);
        }
        StopAllCoroutines();
        StartCoroutine(ClickBounce());
    }

    IEnumerator ClickBounce()
    {
        for (int i = 0; i < 3; i++)
        {
            transform.localScale = _originalScale * 1.2f;
            yield return new WaitForSeconds(0.08f);
            transform.localScale = _originalScale * 0.9f;
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
            // Undertale 风格：白色方框脉冲
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.15f;
            pulse = Mathf.Round(pulse * 8) / 8f;
            discoveryGlow.transform.localScale = Vector3.one * pulse;
        }
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
