using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 猫咪信息弹窗 — Undertale 对话框风格
/// 黑色背景 + 白色边框 + 像素字体 + ♥ 红色点缀
/// </summary>
public class CatInfoPanel : MonoBehaviour
{
    #region UI 绑定
    [Header("面板")]
    public GameObject panelRoot;
    public GameObject maskBg;

    [Header("猫咪信息")]
    public Image catPhoto;
    public TMP_Text nameText;
    public TMP_Text nicknameText;
    public TMP_Text genderText;
    public TMP_Text colorText;
    public TMP_Text neuteredText;
    public TMP_Text adoptedText;
    public TMP_Text descText;
    public TMP_Text hangoutText;
    public TMP_Text firstSeenText;
    public Transform tagsContainer;
    public GameObject tagPrefab;
    public Transform relationsContainer;
    public GameObject relationPrefab;

    [Header("Undertale 按钮")]
    public Button closeBtn;
    public Button locateBtn;       // "去地图找猫"
    public Button chatBtn;         // AI 聊天
    #endregion

    CatData _currentCat;
    Vector3 _hiddenPos = new(0, -1000, 0);
    Vector3 _shownPos = Vector3.zero;

    void Start()
    {
        panelRoot.transform.localPosition = _hiddenPos;
        panelRoot.SetActive(false);

        closeBtn?.onClick.AddListener(Hide);
        locateBtn?.onClick.AddListener(OnLocate);
        maskBg?.GetComponent<Button>()?.onClick.AddListener(Hide);

        // Undertale 风格按钮文字
        if (closeBtn) closeBtn.GetComponentInChildren<TMP_Text>().text = "* 关闭";
        if (locateBtn) locateBtn.GetComponentInChildren<TMP_Text>().text = "* 导航";
        if (chatBtn) chatBtn.GetComponentInChildren<TMP_Text>().text = "* 对话";

        GameManager.Instance.OnCatInteract += Show;
    }

    void OnDestroy()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnCatInteract -= Show;
    }

    public void Show(string catId)
    {
        _currentCat = GameManager.Instance?.GetCatById(catId);
        if (_currentCat == null) return;

        UpdateUI();
        panelRoot.SetActive(true);
        GameManager.Instance.SetState(GameManager.GameState.CatInfo);

        // Undertale 弹出动画：从下方滑入
        StopAllCoroutines();
        StartCoroutine(AnimateShow());
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateHide());
    }

    System.Collections.IEnumerator AnimateShow()
    {
        float t = 0;
        Vector3 start = _hiddenPos;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - t / 0.3f, 2);
            panelRoot.transform.localPosition = Vector3.Lerp(start, _shownPos, e);
            yield return null;
        }
    }

    System.Collections.IEnumerator AnimateHide()
    {
        float t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            panelRoot.transform.localPosition = Vector3.Lerp(_shownPos, _hiddenPos, t / 0.2f);
            yield return null;
        }
        panelRoot.SetActive(false);
        GameManager.Instance.SetState(GameManager.GameState.Exploring);
    }

    void UpdateUI()
    {
        var c = _currentCat;

        // Undertale 风格：星号前缀对话文本
        nameText.text = $"* {c.name}";
        nicknameText.text = string.IsNullOrEmpty(c.nickname) ? "" : $"「{c.nickname}」";
        nicknameText.gameObject.SetActive(!string.IsNullOrEmpty(c.nickname));
        genderText.text = $"* 性别: {c.GenderDisplay}";
        colorText.text = $"* 花色: {c.color ?? "未知"}";
        neuteredText.text = $"* 绝育: {c.NeuteredDisplay}";
        // Undertale 颜色：绿色=已绝育（HP色），黄色=未绝育（SAVE色）
        neuteredText.color = c.neutered ? new Color(0, 0.9f, 0) : new Color(1, 0.84f, 0);
        adoptedText.text = $"* 收养: {c.AdoptedDisplay}";
        descText.text = $"* {c.description ?? "暂无描述~"}";
        hangoutText.text = string.IsNullOrEmpty(c.hangout) ? "" : $"* 常出没: {c.hangout}";
        firstSeenText.text = $"* 首次目击: {c.firstSeen ?? "未知"}";

        BuildTags(c.tags);
        BuildRelations(c.relations);
    }

    void BuildTags(List<string> tags)
    {
        foreach (Transform t in tagsContainer) Destroy(t.gameObject);
        if (tags == null) return;
        foreach (var tag in tags)
        {
            var go = Instantiate(tagPrefab, tagsContainer);
            go.GetComponentInChildren<TMP_Text>().text = $"# {tag}";
        }
    }

    void BuildRelations(List<CatRelation> relations)
    {
        foreach (Transform t in relationsContainer) Destroy(t.gameObject);
        if (relations == null) return;
        foreach (var r in relations)
        {
            var go = Instantiate(relationPrefab, relationsContainer);
            var txt = go.GetComponent<TMP_Text>();
            if (txt) txt.text = $"♥ {r.relation}：{r.name}";
        }
    }

    void OnLocate()
    {
        GameManager.Instance?.TriggerToast($"* 正在导航到 {_currentCat.name} 的位置...");
        Hide();
    }
}
