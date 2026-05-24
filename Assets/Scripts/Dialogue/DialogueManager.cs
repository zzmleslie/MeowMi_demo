using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Undertale 风格对话管理器
/// - 黑底白字对话框（底部）
/// - 逐字浮现打字机效果
/// - * 星号旁白 / 角色名对话
/// - ▶ 多选项分支
/// - 像素音效同步
/// </summary>
public class DialogueManager : MonoBehaviour
{
    #region 单例
    public static DialogueManager Instance { get; private set; }
    #endregion

    #region UI 绑定
    [Header("对话框")]
    public GameObject dialogueBox;
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;
    public GameObject continueArrow;      // ▼ 闪烁三角形（表示可继续）

    [Header("选项")]
    public GameObject choicesContainer;
    public List<GameObject> choiceButtons; // 最多4个：FIGHT/ACT/ITEM/MERCY 风格
    public List<TMP_Text> choiceTexts;

    [Header("音效")]
    public AudioSource typewriterAudio;
    public float charsPerSecond = 30f;    // 逐字速度
    #endregion

    #region 私有状态
    DialogueScene _currentScene;
    int _currentLineIndex;
    bool _isTyping;
    bool _isWaitingForChoice;
    Coroutine _typeRoutine;
    string _pendingText;
    Action<int> _onChoiceSelected;
    #endregion

    #region 事件
    public event Action OnDialogueStart;
    public event Action OnDialogueEnd;
    public event Action<string> OnSceneTrigger; // 场景触发事件（如"找人类"）
    #endregion

    #region 生命周期
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        dialogueBox.SetActive(false);
        choicesContainer.SetActive(false);
        continueArrow.SetActive(false);
    }

    void Update()
    {
        if (!dialogueBox.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            if (_isWaitingForChoice) return;

            if (_isTyping)
            {
                // 跳过打字 → 立即显示全部
                SkipTyping();
            }
            else
            {
                // 继续下一句
                AdvanceDialogue();
            }
        }
    }
    #endregion

    #region 公共入口

    /// <summary>
    /// 开始一段对话场景
    /// </summary>
    public void StartDialogue(DialogueScene scene)
    {
        _currentScene = scene;
        _currentLineIndex = 0;
        dialogueBox.SetActive(true);
        choicesContainer.SetActive(false);
        if (GameManager.Instance)
            GameManager.Instance.SetState(GameManager.GameState.Dialogue);

        OnDialogueStart?.Invoke();
        ShowLine(_currentLineIndex);
    }

    /// <summary>
    /// 从JSON直接开始对话
    /// </summary>
    public void StartDialogueFromJson(string json)
    {
        var scene = JsonUtility.FromJson<DialogueScene>(json);
        StartDialogue(scene);
    }

    #endregion

    #region 对话推进

    void ShowLine(int index)
    {
        if (_currentScene == null || index >= _currentScene.lines.Count)
        {
            EndDialogue();
            return;
        }

        var line = _currentScene.lines[index];
        continueArrow.SetActive(false);
        choicesContainer.SetActive(false);
        _isWaitingForChoice = false;

        // 设置说话者
        if (!string.IsNullOrEmpty(line.speaker))
        {
            speakerNameText.text = line.speaker;
            speakerNameText.gameObject.SetActive(true);
        }
        else
        {
            speakerNameText.gameObject.SetActive(false);
        }

        // 处理特殊指令
        if (line.type == "trigger")
        {
            OnSceneTrigger?.Invoke(line.text);
            _currentLineIndex++;
            ShowLine(_currentLineIndex);
            return;
        }

        if (line.type == "choice")
        {
            ShowChoices(line);
            return;
        }

        // 普通对话 / 旁白 —— 逐字打字
        string prefix = line.type == "narration" ? "* " : "";
        _pendingText = prefix + line.text;
        _isTyping = true;
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        _typeRoutine = StartCoroutine(TypeText(_pendingText));
    }

    IEnumerator TypeText(string fullText)
    {
        dialogueText.text = "";
        float charDelay = 1f / charsPerSecond;

        for (int i = 0; i < fullText.Length; i++)
        {
            dialogueText.text += fullText[i];
            // 播放打字音效（每2个字符一次，避免太密集）
            if (i % 2 == 0 && typewriterAudio && !typewriterAudio.isPlaying)
                typewriterAudio.PlayOneShot(typewriterAudio.clip);

            yield return new WaitForSeconds(charDelay);
        }

        _isTyping = false;
        continueArrow.SetActive(true);
    }

    void SkipTyping()
    {
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        dialogueText.text = _pendingText;
        _isTyping = false;
        continueArrow.SetActive(true);
    }

    void AdvanceDialogue()
    {
        _currentLineIndex++;
        ShowLine(_currentLineIndex);
    }

    void ShowChoices(DialogueLine line)
    {
        _isWaitingForChoice = true;
        choicesContainer.SetActive(true);
        dialogueText.text = ""; // 清空
        continueArrow.SetActive(false);

        var choices = line.choices;
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (i < choices.Count)
            {
                choiceButtons[i].SetActive(true);
                choiceTexts[i].text = $"▶ {choices[i].label}";
                int capturedIndex = i;
                choiceButtons[i].GetComponent<Button>().onClick.RemoveAllListeners();
                choiceButtons[i].GetComponent<Button>().onClick.AddListener(() =>
                {
                    OnChoiceSelected(capturedIndex, choices[capturedIndex].nextLineIndex);
                });
            }
            else
            {
                choiceButtons[i].SetActive(false);
            }
        }
    }

    void OnChoiceSelected(int choiceIndex, int nextLineIndex)
    {
        _isWaitingForChoice = false;
        choicesContainer.SetActive(false);
        _currentLineIndex = nextLineIndex >= 0 ? nextLineIndex : _currentLineIndex + 1;
        ShowLine(_currentLineIndex);
    }

    void EndDialogue()
    {
        dialogueBox.SetActive(false);
        choicesContainer.SetActive(false);
        if (GameManager.Instance)
            GameManager.Instance.SetState(GameManager.GameState.Exploring);

        OnDialogueEnd?.Invoke();
    }

    /// <summary>
    /// 强制结束（用于剧情中断）
    /// </summary>
    public void ForceEnd()
    {
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        EndDialogue();
    }

    public bool IsDialogueActive => dialogueBox.activeSelf;
    #endregion
}

#region 数据结构

/// <summary>
/// 一个对话场景
/// </summary>
[System.Serializable]
public class DialogueScene
{
    public string sceneId;
    public string chapterId;
    public List<DialogueLine> lines;
}

/// <summary>
/// 单句对话
/// </summary>
[System.Serializable]
public class DialogueLine
{
    public string type;     // "dialogue" | "narration" | "choice" | "trigger"
    public string speaker;  // 说话者名字
    public string text;     // 文本内容
    public List<DialogueChoice> choices; // type=choice时
}

/// <summary>
/// 选项
/// </summary>
[System.Serializable]
public class DialogueChoice
{
    public string label;        // 选项文本
    public int nextLineIndex;   // 跳转到哪一行（-1=继续下一行）
}

#endregion
