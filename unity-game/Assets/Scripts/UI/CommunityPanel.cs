using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 社区模块 — Undertale 复古终端风格
/// 晒照/讨论/捐款/志愿者
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

    // Undertale 配色
    readonly Color _activeColor = new(1f, 0f, 0f);       // 红色 ♥
    readonly Color _inactiveColor = new(0.67f, 0.67f, 0.67f); // 灰色

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

        // Undertale 风格按钮文字
        if (createPostBtn) createPostBtn.GetComponentInChildren<TMP_Text>().text = "* 发帖";
        if (submitPostBtn) submitPostBtn.GetComponentInChildren<TMP_Text>().text = "* 确定";
        if (cancelPostBtn) cancelPostBtn.GetComponentInChildren<TMP_Text>().text = "* 取消";

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
        // Undertale 风格：选中=红色♥，未选中=灰色
        photoTab.targetGraphic.color = _currentTab == "photo" ? _activeColor : _inactiveColor;
        discussTab.targetGraphic.color = _currentTab == "discussion" ? _activeColor : _inactiveColor;
        donateTab.targetGraphic.color = _currentTab == "donation" ? _activeColor : _inactiveColor;
        volunteerTab.targetGraphic.color = _currentTab == "volunteer" ? _activeColor : _inactiveColor;
    }

    async void LoadPosts()
    {
        foreach (Transform t in postListContent) Destroy(t.gameObject);

        var posts = await FetchPosts(_currentTab);
        foreach (var post in posts)
        {
            var go = Instantiate(postPrefab, postListContent);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (texts.Length > 0) texts[0].text = $"* {post.title}";
            if (texts.Length > 1) texts[1].text = $"  {post.content}";
        }
    }

    async Task<List<PostData>> FetchPosts(string tab)
    {
        await Task.Delay(100);
        return new List<PostData>
        {
            new() { title = "大黄晒太阳了！", content = "* 今天在北大楼看到大黄~ ☀️🐱", type = "photo" },
            new() { title = "绝育志愿者招募", content = "* 这周末组织绝育活动，需要3名同学帮忙！", type = "volunteer" },
            new() { title = "小橘绝育捐款", content = "* 已筹集 ¥320 / ¥500 💰", type = "donation" }
        }.FindAll(p => p.type == _currentTab);
    }

    async void SubmitPost()
    {
        if (string.IsNullOrWhiteSpace(postInput.text)) return;
        Debug.Log($"* 发帖: {postInput.text}");
        postInput.text = "";
        createPostPanel.SetActive(false);
        GameManager.Instance?.TriggerToast("* 发布成功！♪");
        LoadPosts();
    }

    [System.Serializable]
    class PostData
    {
        public string title, content, type;
    }
}
