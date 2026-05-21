# 🐱 南雍猫札 Unity 项目 - 安装指南

## 📋 环境要求

| 软件 | 版本 | 说明 |
|------|------|------|
| Unity Hub | 最新版 | 管理 Unity 版本 |
| Unity Editor | **2022.3 LTS** | 建议 LTS 长期支持版 |
| Visual Studio | 2022 | 或 VS Code + C# 扩展 |
| .NET SDK | 6.0+ | Unity 自带 |

## 🚀 安装步骤

### 第一步：安装 Unity

1. 下载 [Unity Hub](https://unity.com/download)
2. 在 Unity Hub → Installs → Install Editor
3. 选择 **Unity 2022.3 LTS**（重要！）
4. 模块勾选：
   - ✅ Microsoft Visual Studio（或已有 VS）
   - ✅ WebGL Build Support（导出 Web 版）
   - ✅ Windows Build Support（本地测试）

### 第二步：创建项目

1. Unity Hub → New Project
2. 模板选择：**2D Core**
3. 项目名：`NanyangCatNotes`
4. 路径：选你的工作目录

### 第三步：导入代码

```bash
# 把本项目 unity-game/Assets/ 下的所有文件
# 复制到 Unity 项目的 Assets/ 目录中
```

或者直接在 Unity 中：
- 把 `unity-game/Assets/Scripts/` 拖入 Unity 的 Assets 窗口
- 把 `unity-game/Assets/Resources/` 也拖入

### 第四步：安装依赖包

Unity 菜单 → Window → Package Manager

安装以下包：
- **TextMeshPro**（UI 文字 - 通常已内置）
- **2D Sprite**（2D 精灵 - 通常已内置）
- **Newtonsoft Json**（JSON 解析）

或在 `Packages/manifest.json` 中添加：
```json
{
  "dependencies": {
    "com.unity.nuget.newtonsoft-json": "3.2.1",
    "com.unity.textmeshpro": "3.0.6",
    "com.unity.2d.sprite": "1.0.0"
  }
}
```

### 第五步：设置场景

1. 创建空场景：File → New Scene → 2D
2. 在 Hierarchy 中创建：

```
Scene
├── GameManager          ← 挂载 GameManager.cs
├── Map                  ← 挂载 MapBuilder.cs
│   ├── Buildings        ← 空 GameObject
│   ├── Trees            ← 空 GameObject
│   ├── CatSpots         ← 空 GameObject（Layer 设为 CatSpot）
│   └── Paths            ← 空 GameObject
├── Player               ← 挂载 PlayerController.cs + Rigidbody2D + SpriteRenderer
├── Main Camera          ← 挂载 CameraFollow.cs
├── UI Canvas
│   ├── CatInfoPanel     ← 挂载 CatInfoPanel.cs
│   ├── CommunityPanel   ← 挂载 CommunityPanel.cs
│   ├── Joystick         ← 虚拟摇杆（移动端）
│   └── HUD              ← 发现计数、状态栏
└── EventSystem          ← 自动创建
```

### 第六步：导入数据

把以下文件放入 `Assets/Resources/Data/`：
- `cats.json`（从项目根目录 `data/cats.json` 复制）
- `map-data.json`（从项目根目录 `data/map-data.json` 复制）

### 第七步：设置物理层

1. Edit → Project Settings → Tags and Layers
2. 添加 Layer：**CatSpot**（用于猫咪检测）
3. 将 CatSpots 下的对象 Layer 设为 CatSpot
4. PlayerController 的 `catSpotLayer` 选 CatSpot

### 第八步：运行！

点击 Unity 顶部的 ▶️ Play 按钮即可测试！

### 第九步：导出 WebGL（发布）

1. File → Build Settings
2. Platform 选 WebGL → Switch Platform
3. Player Settings → Resolution → 设置合适分辨率
4. Build → 输出到一个文件夹
5. 把输出的文件夹丢到任何静态服务器上就能玩！

---

## 📁 项目文件说明

```
unity-game/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/GameManager.cs        ← 🎯 全局管理 + 数据类型
│   │   ├── Player/PlayerController.cs ← 🐱 玩家小猫操控
│   │   ├── NPC/CatNPC.cs              ← 🐈 猫咪 NPC 交互
│   │   ├── Map/MapBuilder.cs          ← 🗺️ 地图动态生成
│   │   ├── UI/CatInfoPanel.cs         ← 📱 猫咪信息弹窗
│   │   ├── UI/CommunityPanel.cs       ← 💬 社区面板
│   │   └── Camera/CameraFollow.cs     ← 📷 相机跟随
│   └── Resources/
│       └── Data/                      ← JSON 数据文件
└── Packages/
    └── manifest.json                  ← 依赖配置
```

---

## 🐍 猫咪数据获取

在运行游戏前，先用 Python 爬虫获取真实猫咪数据：

```bash
cd scripts
pip install -r requirements.txt
python scraper.py --gui
# 或命令行模式：
python scraper.py --json your_data.json --download
```

---

## ❓ 常见问题

**Q: 找不到 Newtonsoft.Json？**
A: Package Manager → Add package by name → `com.unity.nuget.newtonsoft-json`

**Q: Text (TMP) 显示粉色方块？**
A: Window → TextMeshPro → Import TMP Essential Resources

**Q: 地图不显示？**
A: 检查 Resources/Data/ 下有 cats.json 和 map-data.json

**Q: 无法移动？**
A: 检查 Input Manager（Edit → Project Settings → Input Manager）
   确保 Horizontal/Vertical 轴存在
