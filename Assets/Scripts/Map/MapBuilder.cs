using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 校园地图构建器 — Undertale 像素RPG风格
/// 根据 MapData 动态生成像素艺术地图：纯色建筑+粗黑边框、几何树、像素路径
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

    [Header("像素风设置")]
    public bool pixelArtMode = true;        // 启用像素渲染模式
    public int pixelsPerUnit = 16;          // 像素密度

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
        Debug.Log($"🗺️ 开始构建像素地图: {data.mapName}");

        // 设置地图边界
        var mapCollider = gameObject.AddComponent<BoxCollider2D>();
        mapCollider.size = new Vector2(data.mapSize.width, data.mapSize.height);
        mapCollider.offset = mapCollider.size / 2f;

        BuildPaths(data.paths);
        BuildBuildings(data.buildings);
        BuildTrees(data.trees);
        BuildCatSpots(data.catSpots);

        Debug.Log("✅ 像素地图构建完成");
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
            lr.startWidth = p.width + 4f;  // 黑色底层（描边）
            lr.endWidth = p.width + 4f;
            lr.useWorldSpace = true;
            lr.startColor = Color.black;
            lr.endColor = Color.black;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 0;

            for (int i = 0; i < p.points.Count; i++)
                lr.SetPosition(i, new Vector3(p.points[i].x, p.points[i].y, -1));

            // 上层土黄色路径（略窄）
            var goTop = new GameObject($"Path_{p.name}_Top");
            goTop.transform.SetParent(go.transform);
            var lrTop = goTop.AddComponent<LineRenderer>();
            lrTop.positionCount = p.points.Count;
            lrTop.startWidth = p.width;
            lrTop.endWidth = p.width;
            lrTop.useWorldSpace = true;
            // Undertale 大地图路径色
            lrTop.startColor = new Color(0.78f, 0.63f, 0.25f, 1f);
            lrTop.endColor = new Color(0.78f, 0.63f, 0.25f, 1f);
            lrTop.material = new Material(Shader.Find("Sprites/Default"));
            lrTop.sortingOrder = 1;

            for (int i = 0; i < p.points.Count; i++)
                lrTop.SetPosition(i, new Vector3(p.points[i].x, p.points[i].y, -0.9f));
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
                // 像素纯色纹理（Undertale风格：纯色填充 + 粗黑边框）
                var tex = CreatePixelBuildingTexture((int)b.w, (int)b.h,
                    ParseColor(b.color));
                tex.filterMode = FilterMode.Point;  // ★ 关键：像素硬边
                sr.sprite = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), pixelsPerUnit);
            }

            // 黑色阴影子对象（3px右下偏移）
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(go.transform);
            shadow.transform.localPosition = new Vector3(3f / pixelsPerUnit, -3f / pixelsPerUnit, -0.1f);
            shadow.transform.localScale = Vector3.one;
            var shadowSr = shadow.AddComponent<SpriteRenderer>();
            var shadowTex = CreateSolidTexture((int)b.w, (int)b.h,
                new Color(0, 0, 0, 1f));
            shadowTex.filterMode = FilterMode.Point;
            shadowSr.sprite = Sprite.Create(shadowTex,
                new Rect(0, 0, shadowTex.width, shadowTex.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);
            shadowSr.sortingOrder = -1;

            // 白色高光线（顶部+左侧 1px）
            var highlight = new GameObject("Highlight");
            highlight.transform.SetParent(go.transform);
            highlight.transform.localPosition = Vector3.zero;
            highlight.transform.localScale = Vector3.one;
            var hlSr = highlight.AddComponent<SpriteRenderer>();
            var hlTex = CreateHighlightTexture((int)b.w, (int)b.h);
            hlTex.filterMode = FilterMode.Point;
            hlSr.sprite = Sprite.Create(hlTex,
                new Rect(0, 0, hlTex.width, hlTex.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);
            hlSr.sortingOrder = 2;
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

            // Undertale 风格像素树颜色
            Color crownColor = t.type == "wutong"
                ? new Color(0.12f, 0.36f, 0.12f, 1f)  // 梧桐深绿
                : new Color(0.78f, 0.39f, 0.47f, 1f);  // 樱花粉红

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var tex = CreatePixelTreeTexture(64, crownColor);
                tex.filterMode = FilterMode.Point;
                sr.sprite = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), pixelsPerUnit);
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

    #region 像素纹理生成工具

    /// <summary>
    /// 像素建筑纹理：纯色填充 + 3px黑色边框 + 像素窗户
    /// </summary>
    Texture2D CreatePixelBuildingTexture(int w, int h, Color fill)
    {
        w = Mathf.Max(w, 8); h = Mathf.Max(h, 8);
        var tex = new Texture2D(w, h);
        var pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < x; x++)
            {
                // 3px黑色边框
                bool isBorder = x < 3 || x >= w - 3 || y < 3 || y >= h - 3;
                if (isBorder)
                {
                    pixels[y * w + x] = Color.black;
                }
                else
                {
                    // 纯色填充
                    pixels[y * w + x] = fill;

                    // 顶部和左侧1px白色高光
                    if (y == 3 || x == 3)
                        pixels[y * w + x] = Color.Lerp(fill, Color.white, 0.2f);
                }
            }
        }

        // 像素窗户（白色小方块）
        int winW = 4, winH = 5;
        int marginX = 8, marginY = 8;
        int gapX = 14, gapY = 16;
        int cols = Mathf.Max(1, (w - marginX * 2) / gapX);
        int rows = Mathf.Max(1, (h - marginY * 2) / gapY);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int wx = marginX + col * gapX;
                int wy = marginY + row * gapY;
                for (int dy = 0; dy < winH && wy + dy < h - 3; dy++)
                    for (int dx = 0; dx < winW && wx + dx < w - 3; dx++)
                        pixels[(wy + dy) * w + (wx + dx)] = Color.white;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// 像素树纹理：三角形树冠 + 粗树干
    /// </summary>
    Texture2D CreatePixelTreeTexture(int size, Color crownColor)
    {
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        // 初始透明
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        int centerX = size / 2;
        int crownTop = size / 5;
        int crownBottom = size * 3 / 5;
        int crownHalfW = size / 3;

        // 树冠三角形
        for (int y = crownTop; y < crownBottom; y++)
        {
            float t = (float)(y - crownTop) / (crownBottom - crownTop);
            int halfW = Mathf.RoundToInt(crownHalfW * t + crownHalfW * 0.3f);
            for (int x = centerX - halfW; x <= centerX + halfW; x++)
            {
                if (x >= 2 && x < size - 2)
                {
                    bool isEdge = (x == centerX - halfW || x == centerX + halfW || y == crownTop || y == crownBottom - 1);
                    pixels[y * size + x] = isEdge ? Color.black : crownColor;
                }
            }
        }

        // 树干
        int trunkW = size / 8;
        int trunkH = size / 3;
        for (int y = crownBottom; y < crownBottom + trunkH && y < size - 2; y++)
        {
            for (int x = centerX - trunkW; x <= centerX + trunkW; x++)
            {
                if (x >= 2 && x < size - 2)
                {
                    bool isEdge = (x == centerX - trunkW || x == centerX + trunkW || y == crownBottom + trunkH - 1);
                    pixels[y * size + x] = isEdge ? Color.black : new Color(0.42f, 0.26f, 0.15f);
                }
            }
        }

        tex.SetPixels(pixels);
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

    /// <summary>
    /// 白色高光纹理：仅顶部和左侧1px白线
    /// </summary>
    Texture2D CreateHighlightTexture(int w, int h)
    {
        var tex = new Texture2D(Mathf.Max(w, 1), Mathf.Max(h, 1));
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        // 顶部白线
        for (int x = 3; x < w - 3; x++) pixels[3 * w + x] = new Color(1, 1, 1, 0.3f);
        // 左侧白线
        for (int y = 3; y < h - 3; y++) pixels[y * w + 3] = new Color(1, 1, 1, 0.3f);

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    Color ParseColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
        return new Color(0.5f, 0.5f, 0.5f);
    }
    #endregion
}
