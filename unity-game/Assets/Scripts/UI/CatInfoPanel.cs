using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 猫咪信息弹窗 - 展示完整猫咪档案
/// 弹出动画 + 照片/性别/花色/性格/亲属关系
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
    public GameObject relationPrefab;  // 预制件，包含 TMP_Text 组件

    [Header("按钮")]
    public Button closeBtn;
    public Button locateBtn;       // "去地图找猫"
    public Button chatBtn;         // AI 聊天（预留）
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

        // 弹出动画
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
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - t / 0.35f, 3); // easeOut
            panelRoot.transform.localPosition = Vector3.Lerp(start, _shownPos, e);
            panelRoot.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, e);
            yield return null;
        }
    }

    System.Collections.IEnumerator AnimateHide()
    {
        float t = 0;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            panelRoot.transform.localPosition = Vector3.Lerp(_shownPos, _hiddenPos, t / 0.25f);
            yield return null;
        }
        panelRoot.SetActive(false);
        GameManager.Instance.SetState(GameManager.GameState.Exploring);
    }

    void UpdateUI()
    {
        var c = _currentCat;

        nameText.text = c.name;
        nicknameText.text = string.IsNullOrEmpty(c.nickname) ? "" : $"「{c.nickname}」";
        nicknameText.gameObject.SetActive(!string.IsNullOrEmpty(c.nickname));
        genderText.text = c.GenderDisplay;
        colorText.text = c.color ?? "未知";
        neuteredText.text = c.NeuteredDisplay;
        neuteredText.color = c.neutered ? new Color(0.48f, 0.63f, 0.48f) : new Color(0.91f, 0.66f, 0.49f);
        adoptedText.text = c.AdoptedDisplay;
        descText.text = c.description ?? "暂无描述~";
        hangoutText.text = c.hangout ?? "";
        firstSeenText.text = c.firstSeen ?? "未知";

        // 标签
        BuildTags(c.tags);
        // 亲属关系
        BuildRelations(c.relations);
    }

    void BuildTags(List<string> tags)
    {
        foreach (Transform t in tagsContainer) Destroy(t.gameObject);
        if (tags == null) return;
        foreach (var tag in tags)
        {
            var go = Instantiate(tagPrefab, tagsContainer);
            go.GetComponentInChildren<TMP_Text>().text = tag;
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
            if (txt) txt.text = $"{r.relation}：{r.name}";
        }
    }

    void OnLocate()
    {
        // TODO: 发送地图定位事件，移动相机到猫咪出没点
        GameManager.Instance?.TriggerToast($"📍 正在导航到 {_currentCat.name} 的位置~");
        Hide();
    }
}
