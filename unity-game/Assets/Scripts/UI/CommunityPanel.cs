using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 社区模块 - 晒照/讨论/捐款/志愿者
/// 点击主界面图标切换到社区，再点击回到探索
/// </summary>
public class CommunityPanel : MonoBehaviour
{
    #region UI 绑定
    [Header("面板")]
    public GameObject panelRoot;

    [Header("Tab 按钮")]
    public Button photoTab;
    public Button discussTab;
    public Button donateTab;
    public Button volunteerTab;

    [Header("内容")]
    public Transform postListContent;
    public GameObject postPrefab;
    public Button createPostBtn;
    public GameObject createPostPanel;
    public TMP_InputField postInput;
    public Button submitPostBtn;
    public Button cancelPostBtn;
    #endregion

    string _currentTab = "photo";
    bool _isVisible;

    void Start()
    {
        panelRoot.SetActive(false);

        photoTab?.onClick.AddListener(() => SwitchTab("photo"));
        discussTab?.onClick.AddListener(() => SwitchTab("discussion"));
        donateTab?.onClick.AddListener(() => SwitchTab("donation"));
        volunteerTab?.onClick.AddListener(() => SwitchTab("volunteer"));
        createPostBtn?.onClick.AddListener(() => createPostPanel.SetActive(true));
        cancelPostBtn?.onClick.AddListener(() => createPostPanel.SetActive(false));
        submitPostBtn?.onClick.AddListener(SubmitPost);

        // 监听状态切换（主界面图标点击）
        GameManager.Instance.OnStateChanged += OnStateChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Community) Show();
        else if (_isVisible) Hide();
    }

    public void Toggle()
    {
        if (_isVisible) Hide(); else Show();
    }

    public void Show()
    {
        _isVisible = true;
        panelRoot.SetActive(true);
        LoadPosts();
    }

    public void Hide()
    {
        _isVisible = false;
        panelRoot.SetActive(false);
        if (GameManager.Instance.CurrentState == GameManager.GameState.Community)
            GameManager.Instance.SetState(GameManager.GameState.Exploring);
    }

    void SwitchTab(string tab)
    {
        _currentTab = tab;
        UpdateTabHighlights();
        LoadPosts();
    }

    void UpdateTabHighlights()
    {
        // 高亮当前 Tab（简化版：改变颜色）
        Color active = new(0.83f, 0.47f, 0.42f);
        Color inactive = new(0.55f, 0.49f, 0.42f);
        photoTab.targetGraphic.color = _currentTab == "photo" ? active : inactive;
        discussTab.targetGraphic.color = _currentTab == "discussion" ? active : inactive;
        donateTab.targetGraphic.color = _currentTab == "donation" ? active : inactive;
        volunteerTab.targetGraphic.color = _currentTab == "volunteer" ? active : inactive;
    }

    async void LoadPosts()
    {
        // 清空列表
        foreach (Transform t in postListContent) Destroy(t.gameObject);

        var posts = await FetchPosts(_currentTab);
        foreach (var post in posts)
        {
            var go = Instantiate(postPrefab, postListContent);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            // 简化：取第一个Text当标题，第二个当内容
            if (texts.Length > 0) texts[0].text = post.title;
            if (texts.Length > 1) texts[1].text = post.content;
        }
    }

    async Task<List<PostData>> FetchPosts(string tab)
    {
        // TODO: 对接真实 API
        await Task.Delay(100);
        return new List<PostData>
        {
            new() { title = "大黄晒太阳了！", content = "今天在北大楼看到大黄~ ☀️🐱", type = "photo" },
            new() { title = "绝育志愿者招募", content = "这周末组织绝育活动，需要3名同学帮忙！🙋", type = "volunteer" },
            new() { title = "小橘绝育捐款", content = "已筹集 ¥320 / ¥500 💰", type = "donation" }
        }.FindAll(p => p.type == _currentTab);
    }

    async void SubmitPost()
    {
        if (string.IsNullOrWhiteSpace(postInput.text)) return;
        // TODO: 发送到后端 API
        Debug.Log($"📝 发帖: {postInput.text}");
        postInput.text = "";
        createPostPanel.SetActive(false);
        GameManager.Instance?.TriggerToast("发布成功！🐱");
        LoadPosts();
    }

    [System.Serializable]
    class PostData
    {
        public string title, content, type;
    }
}
