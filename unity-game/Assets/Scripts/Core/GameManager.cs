using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Newtonsoft.Json;

/// <summary>
/// 南雍猫札 - 游戏全局管理器
/// 单例模式，管理游戏状态、数据加载、事件系统
/// 挂载：场景中的 GameManager 对象上
/// </summary>
public class GameManager : MonoBehaviour
{
    #region 单例
    public static GameManager Instance { get; private set; }
    #endregion

    #region 游戏状态
    public enum GameState { Loading, Menu, Exploring, CatInfo, Community }
    public GameState CurrentState { get; private set; } = GameState.Loading;
    #endregion

    #region 数据
    [Header("数据文件路径（Resources 目录下）")]
    public string catsDataPath = "Data/cats";
    public string mapDataPath = "Data/map-data";

    public List<CatData> AllCats { get; private set; } = new();
    public MapData MapData { get; private set; }
    public HashSet<string> DiscoveredCats { get; private set; } = new();
    #endregion

    #region 事件系统
    public event Action<string> OnCatDiscovered;        // 发现猫咪
    public event Action<string> OnCatNearby;            // 靠近猫咪
    public event Action OnCatLeave;                     // 离开猫咪范围
    public event Action<string> OnCatInteract;          // 与猫咪互动
    public event Action OnAllCatsDiscovered;            // 全部发现
    public event Action<GameState> OnStateChanged;      // 状态切换
    public event Action<string> OnShowToast;            // 显示提示
    #endregion

    #region 生命周期
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        await LoadAllData();
        SetState(GameState.Menu);
    }
    #endregion

    #region 数据加载
    async Task LoadAllData()
    {
        await LoadCatData();
        await LoadMapData();
        LoadDiscoveredFromPrefs();
        Debug.Log($"✅ 数据加载完成: {AllCats.Count} 只猫咪");
    }

    async Task LoadCatData()
    {
        var textAsset = Resources.Load<TextAsset>(catsDataPath);
        if (textAsset != null)
        {
            var wrapper = JsonConvert.DeserializeObject<CatDataWrapper>(textAsset.text);
            AllCats = wrapper?.cats ?? new List<CatData>();
        }
        else
        {
            // 尝试从外部 URL 加载（用于动态更新数据）
            await LoadCatDataExternal();
        }
    }

    async Task LoadCatDataExternal()
    {
        try
        {
            using var www = UnityEngine.Networking.UnityWebRequest.Get("/api/cats");
            await www.SendWebRequest();
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var wrapper = JsonConvert.DeserializeObject<CatDataWrapper>(www.downloadHandler.text);
                AllCats = wrapper?.cats ?? new List<CatData>();
            }
        }
        catch (Exception e) { Debug.LogWarning($"外部数据加载失败: {e.Message}"); }
    }

    async Task LoadMapData()
    {
        var textAsset = Resources.Load<TextAsset>(mapDataPath);
        if (textAsset != null)
            MapData = JsonConvert.DeserializeObject<MapData>(textAsset.text);
    }

    void LoadDiscoveredFromPrefs()
    {
        var saved = PlayerPrefs.GetString("DiscoveredCats", "");
        if (!string.IsNullOrEmpty(saved))
        {
            var ids = JsonConvert.DeserializeObject<List<string>>(saved);
            DiscoveredCats = new HashSet<string>(ids);
        }
    }

    #endregion

    #region 公共方法
    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void DiscoverCat(string catId)
    {
        if (DiscoveredCats.Contains(catId)) return;
        DiscoveredCats.Add(catId);
        SaveDiscovered();
        OnCatDiscovered?.Invoke(catId);
        if (DiscoveredCats.Count >= AllCats.Count)
            OnAllCatsDiscovered?.Invoke();
    }

    public bool IsDiscovered(string catId) => DiscoveredCats.Contains(catId);

    public CatData GetCatById(string id) => AllCats.Find(c => c.id == id);

    public (int found, int total) DiscoveryProgress =>
        (DiscoveredCats.Count, AllCats.Count);

    void SaveDiscovered()
    {
        var json = JsonConvert.SerializeObject(new List<string>(DiscoveredCats));
        PlayerPrefs.SetString("DiscoveredCats", json);
        PlayerPrefs.Save();
    }

    // 事件触发（内部使用）
    public void TriggerNearby(string catId) => OnCatNearby?.Invoke(catId);
    public void TriggerLeaveCat() => OnCatLeave?.Invoke();
    public void TriggerInteract(string catId) => OnCatInteract?.Invoke(catId);
    public void TriggerToast(string msg) => OnShowToast?.Invoke(msg);
    #endregion

    #region AI 智能体接口（预留）
    /// <summary>
    /// 与猫咪 AI 助手对话
    /// </summary>
    public async Task<string> QueryCatAI(string catId, string question)
    {
        try
        {
            using var www = UnityEngine.Networking.UnityWebRequest.PostWwwForm(
                "/api/ai/cat-chat",
                JsonConvert.SerializeObject(new { catId, question })
            );
            await www.SendWebRequest();
            var resp = JsonConvert.DeserializeObject<AIResponse>(www.downloadHandler.text);
            return resp?.reply ?? "喵~这个问题我还在学习中...";
        }
        catch { return "喵呜~AI助手暂时不在线，请稍后再试~"; }
    }

    public async Task<List<string>> GetRecommendedRoute()
    {
        try
        {
            using var www = UnityEngine.Networking.UnityWebRequest.Get("/api/ai/route");
            await www.SendWebRequest();
            var resp = JsonConvert.DeserializeObject<RouteResponse>(www.downloadHandler.text);
            return resp?.route ?? new List<string>();
        }
        catch { return new List<string>(); }
    }
    #endregion
}

#region 数据类型定义
[System.Serializable]
public class CatDataWrapper { public List<CatData> cats; }

[System.Serializable]
public class CatData
{
    public string id, name, nickname, color, breed, description, hangout;
    public string photo, photoUrl, firstSeen, lastSeen, campusArea;
    public string gender; // "male" / "female" / "unknown"
    public bool neutered, adopted;
    public List<string> tags;
    public List<CatRelation> relations;

    public string GenderDisplay => gender switch
    {
        "male" => "♂ 男孩",
        "female" => "♀ 女孩",
        _ => "未知"
    };
    public string NeuteredDisplay => neutered ? "✅ 已绝育" : "⚠️ 未绝育";
    public string AdoptedDisplay => adopted ? "🏠 已收养" : "🏫 校园自由猫";
}

[System.Serializable]
public class CatRelation
{
    public string catId, relation, name;
}

[System.Serializable]
public class MapData
{
    public string mapName;
    public MapSize mapSize;
    public List<BuildingData> buildings;
    public List<TreeData> trees;
    public List<PathData> paths;
    public List<CatSpotData> catSpots;
}

[System.Serializable]
public class MapSize { public int width, height; }

[System.Serializable]
public class BuildingData
{
    public string id, name, color, strokeColor, texturePath;
    public float x, y, w, h;
    public List<Vector2> contour;
}

[System.Serializable]
public class TreeData
{
    public float x, y, radius;
    public string type;
}

[System.Serializable]
public class PathData
{
    public List<Vector2> points;
    public float width;
    public string name;
}

[System.Serializable]
public class CatSpotData
{
    public string id, catId, description;
    public float x, y;
}

[System.Serializable]
public class AIResponse { public string reply; }

[System.Serializable]
public class RouteResponse { public List<string> route; }
#endregion
