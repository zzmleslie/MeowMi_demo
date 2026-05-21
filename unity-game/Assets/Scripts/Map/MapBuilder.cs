using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 校园地图构建器
/// 根据 MapData 动态生成地图中的建筑、道路、树木、猫咪出没点
/// </summary>
public class MapBuilder : MonoBehaviour
{
    [Header("预制件")]
    public GameObject buildingPrefab;       // 建筑（含 SpriteRenderer）
    public GameObject treePrefab;           // 树木
    public GameObject catSpotPrefab;        // 猫咪标记点（含 CatSpotMarker + CircleCollider2D）
    public GameObject pathSegmentPrefab;    // 道路段

    [Header("容器")]
    public Transform buildingsContainer;
    public Transform treesContainer;
    public Transform catSpotsContainer;
    public Transform pathsContainer;

    [Header("材质")]
    public Material watercolorMaterial;     // 水彩 Shader 材质

    void Start()
    {
        if (GameManager.Instance?.MapData != null)
            BuildMap(GameManager.Instance.MapData);
        else
            GameManager.Instance.OnStateChanged += WaitAndBuild;
    }

    void WaitAndBuild(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Menu &&
            GameManager.Instance.MapData != null)
        {
            BuildMap(GameManager.Instance.MapData);
            GameManager.Instance.OnStateChanged -= WaitAndBuild;
        }
    }

    public void BuildMap(MapData data)
    {
        Debug.Log($"🗺️ 开始构建地图: {data.mapName}");

        // 设置地图边界
        var mapCollider = gameObject.AddComponent<BoxCollider2D>();
        mapCollider.size = new Vector2(data.mapSize.width, data.mapSize.height);
        mapCollider.offset = mapCollider.size / 2f;

        BuildPaths(data.paths);
        BuildBuildings(data.buildings);
        BuildTrees(data.trees);
        BuildCatSpots(data.catSpots);

        Debug.Log("✅ 地图构建完成");
    }

    void BuildPaths(List<PathData> paths)
    {
        if (paths == null) return;
        foreach (var p in paths)
        {
            if (p.points.Count < 2) continue;
            var go = new GameObject($"Path_{p.name}");
            go.transform.SetParent(pathsContainer);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = p.points.Count;
            lr.startWidth = p.width;
            lr.endWidth = p.width;
            lr.useWorldSpace = true;
            lr.startColor = new Color(0.83f, 0.77f, 0.73f, 0.4f);
            lr.endColor = new Color(0.83f, 0.77f, 0.73f, 0.4f);
            lr.material = new Material(Shader.Find("Sprites/Default"));

            for (int i = 0; i < p.points.Count; i++)
                lr.SetPosition(i, new Vector3(p.points[i].x, p.points[i].y, -1));
        }
    }

    void BuildBuildings(List<BuildingData> buildings)
    {
        if (buildings == null) return;
        foreach (var b in buildings)
        {
            var go = Instantiate(buildingPrefab, buildingsContainer);
            go.name = b.name;
            go.transform.position = new Vector3(b.x + b.w / 2f, b.y + b.h / 2f, 0);
            go.transform.localScale = new Vector3(b.w, b.h, 1);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // 创建纯色纹理模拟水彩建筑
                var tex = CreateWatercolorTexture((int)b.w, (int)b.h,
                    ParseColor(b.color), ParseColor(b.strokeColor));
                sr.sprite = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
                if (watercolorMaterial) sr.material = watercolorMaterial;
            }

            // 阴影子对象
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(go.transform);
            shadow.transform.localPosition = new Vector3(0.1f, -0.1f, -0.1f);
            shadow.transform.localScale = Vector3.one;
            var shadowSr = shadow.AddComponent<SpriteRenderer>();
            var shadowTex = CreateSolidTexture((int)b.w, (int)b.h,
                new Color(0, 0, 0, 0.15f));
            shadowSr.sprite = Sprite.Create(shadowTex,
                new Rect(0, 0, shadowTex.width, shadowTex.height),
                new Vector2(0.5f, 0.5f));
            shadowSr.sortingOrder = -1;
        }
    }

    void BuildTrees(List<TreeData> trees)
    {
        if (trees == null) return;
        foreach (var t in trees)
        {
            var go = Instantiate(treePrefab, treesContainer);
            go.transform.position = new Vector3(t.x, t.y, 0);
            var r = t.radius;
            go.transform.localScale = new Vector3(r * 2, r * 2, 1);

            var color = t.type == "wutong"
                ? new Color(0.48f, 0.63f, 0.48f, 0.8f)
                : new Color(0.72f, 0.79f, 0.66f, 0.8f);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var tex = CreateCircleTexture(64, color);
                sr.sprite = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
            }
        }
    }

    void BuildCatSpots(List<CatSpotData> spots)
    {
        if (spots == null) return;
        foreach (var s in spots)
        {
            var go = Instantiate(catSpotPrefab, catSpotsContainer);
            go.transform.position = new Vector3(s.x, s.y, -0.5f);
            go.name = $"CatSpot_{s.catId}";

            var marker = go.GetComponent<CatSpotMarker>();
            if (marker) marker.catId = s.catId;

            var col = go.GetComponent<CircleCollider2D>();
            if (col)
            {
                col.radius = 3f;
                col.isTrigger = true;
            }
        }
    }

    #region 纹理生成工具
    Texture2D CreateWatercolorTexture(int w, int h, Color fill, Color stroke)
    {
        var tex = new Texture2D(Mathf.Max(w, 1), Mathf.Max(h, 1));
        var pixels = tex.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            float noise = Random.Range(0.9f, 1f);
            pixels[i] = new Color(
                fill.r * noise + Random.Range(-0.03f, 0.03f),
                fill.g * noise + Random.Range(-0.03f, 0.03f),
                fill.b * noise + Random.Range(-0.02f, 0.02f),
                0.85f
            );
        }
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return tex;
    }

    Texture2D CreateSolidTexture(int w, int h, Color color)
    {
        var tex = new Texture2D(Mathf.Max(w, 1), Mathf.Max(h, 1));
        var pixels = new Color[tex.width * tex.height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    Texture2D CreateCircleTexture(int size, Color color)
    {
        var tex = new Texture2D(size, size);
        var center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(dist / radius);
                alpha = Mathf.Pow(alpha, 1.5f);
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha * color.a));
            }
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }

    Color ParseColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
        return new Color(0.83f, 0.77f, 0.73f);
    }
    #endregion
}
