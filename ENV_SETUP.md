# 🛠️开发环境搭建指南

> 从零开始，把电脑变成能跑起整个项目的开发机 (｡･ω･｡)ﾉ♡

---

## 📋 总览：需要装什么？

| 工具 | 版本要求 | 用途 | 大小 | 必需? |
|------|----------|------|------|:---:|
| 🐙 **Git** | 最新版 | 版本管理 + 推送到 GitHub | ~50MB | ✅ 必须 |
| 🎮 **Unity Hub** | 最新版 | 管理 Unity 版本 | ~300MB | ✅ 必须 |
| 🎮 **Unity Editor** | **2022.3 LTS** | 游戏引擎本体 | ~5GB | ✅ 必须 |
| 🐍 **Python** | 3.9+ | 猫咪数据爬取 | ~100MB | 🟡 可选 |
| 🟢 **Node.js** | 16+ | 照片→地图预处理 | ~80MB | 🟡 可选 |
| 📝 **VS Code** | 最新版 | 写代码 + AI 辅助 | ~100MB | ✅ 必须 |


## 🗺️ 安装顺序推荐

```
第 1 步：Git              ← 最先装，装完就能拉代码
第 2 步：VS Code          ← 马上能看代码、写文档
第 3 步：GitHub 学生认证   ← 申请 Copilot（审核要 1-3 天，尽早搞）
第 4 步：Python           ← 需要时再装
第 5 步：Node.js          ← 需要时再装
第 6 步：Unity Hub        ← 下载 Unity 的入口
第 7 步：Unity Editor     ← 最大最慢，放着下载
```

> 💡 Git + VS Code 先装好，同时去申请学生认证，边等审核边等 Unity 下载～

---

## 1️⃣ Git — 版本管理

### 安装

1. 下载：[https://git-scm.com/download/win](https://git-scm.com/download/win)
2. 运行安装程序，一路 Next

### 验证

打开终端（PowerShell / CMD），输入：

```bash
git --version
# → git version 2.xx.x   ✅ 成功
```

### 初始配置（仅一次）

```bash
git config --global user.name "你的名字"
git config --global user.email "你的邮箱@example.com"
```

---

## 2️⃣ Unity Hub — 项目管理器

### 安装

1. 下载：[https://unity.com/download](https://unity.com/download)
2. 运行安装程序

### 验证

- 桌面出现 Unity Hub 图标，能打开就 ✅

---

## 3️⃣ Unity Editor 2022.3 LTS — 游戏引擎

> ⚠️ **版本必须选 2022.3 LTS！** 不要选更新的版本，脚本兼容性不保证～

### 安装

```text
1. 打开 Unity Hub
2. 左侧点 「Installs」
3. 右上角点 「Install Editor」
4. 选 「2022.3 LTS」标签页 → 找一个最新的 2022.3.xx
5. 点 「Install」
```

### 必选模块

安装时勾选以下模块（其他可以不选）：

| 模块 | 必须？ | 说明 |
|------|:---:|------|
| ✅ Microsoft Visual Studio | 推荐 | 写 C# 用，已有 VS 可不选 |
| ✅ **WebGL Build Support** | ✅ 必须 | 导出网页版游戏 |
| ✅ Windows Build Support (IL2CPP) | 推荐 | 本地测试 |

### 注意：Unity 个人版免费

- 选 「Unity Personal」→ 年收入 < $100K 即可免费使用
- 需要注册 Unity 账号（用 GitHub/Google/邮箱都行）

---

## 4️⃣ Python 3.9+ — 数据爬取

### 安装

1. 下载：[https://www.python.org/downloads/](https://www.python.org/downloads/)
2. 运行安装程序
3. **⚠️ 重要：勾选底部的 「Add Python to PATH」！**

### 验证

```bash
python --version
# → Python 3.xx.x   ✅ 成功
```

### 安装项目依赖

```bash
cd 项目根目录
pip install -r scripts\requirements.txt
```

依赖只有两个小包：`requests`（网络请求） + `pillow`（图片处理），秒装完～

---

## 5️⃣ Node.js 16+ — 照片预处理

### 安装

1. 下载：[https://nodejs.org/](https://nodejs.org/)（选 LTS 版本）
2. 运行安装程序

### 验证

```bash
node --version
# → v20.xx.x   ✅ 成功

npm --version
# → 10.xx.x   ✅ 成功
```
### 安装项目依赖

```bash
cd 项目根目录
npm install
```

只装 `sharp`（图片处理库），约 10MB。

---

## 6️⃣ VS Code — 代码编辑器

全队统一用 VS Code，方便共享配置 + 利用 Copilot AI 辅助写代码。

### 安装

1. 下载：[https://code.visualstudio.com/](https://code.visualstudio.com/)

### 必备扩展

| 扩展 | 用途 |
|------|------|
| **GitHub Copilot** | 🤖 AI 代码补全 + Chat（见下方学生认证） |
| **GitHub Copilot Chat** | 💬 AI 对话式编程助手 |
| **C#** | C# 语法高亮 + 智能提示 |
| **Unity** | Unity 代码片段 + 调试 |
| **Python** | Python 支持 |
| **Markdown Preview** | 预览 .md 文档 |

---

## 7️⃣ GitHub 学生认证 → 免费 Copilot

> 🎓 用学校邮箱（@nju.edu.cn / @smail.nju.edu.cn）白嫖 GitHub Copilot！

### 有什么用？

通过 **GitHub Student Developer Pack**（学生开发包），你可以免费获得：

| 权益 | 说明 |
|------|------|
| 🤖 **GitHub Copilot** | AI 代码补全 + Chat，写 C#/Python 超快 |
| ☁️ **GitHub Pro** | 无限私有仓库 + Actions 额度 |
| 🎨 **Figma** | UI 设计工具（画游戏界面用） |
| 🗺️ **Unity Asset Store** | 部分付费资源免费 |
| …还有 100+ 开发者工具 | 详见官网 |

### 认证步骤

```text
1. 打开 https://education.github.com/pack
2. 点 「Sign up for Student Developer Pack」
3. 用 GitHub 账号登录（没有就先注册）
4. 填写学校信息：
   - 学校邮箱：xxx@nju.edu.cn 或 xxx@smail.nju.edu.cn
   - 学校名：Nanjing University
   - 用途说明：写一两句英文，比如
     "I'm a student learning game development with Unity and C#."
5. 上传学生证照片（或学信网截图）
6. 提交 → 等审核（一般 1-3 天）
```

### 认证通过后

1. 打开 VS Code → 扩展 → 搜 `GitHub Copilot` → 安装
2. 用同一个 GitHub 账号登录 VS Code
3. 写代码时 Copilot 会自动提示补全，按 `Tab` 接受
4. `Ctrl + I` 打开 Copilot Chat 对话窗口

> 💡 整个团队都应该搞！写 C# 脚本效率翻倍～


---

## ✅ 装完后验证一切正常

在项目根目录打开终端，依次跑：

```bash
# 1. Git 正常？
git --version

# 2. Python 正常？
python --version
pip install -r scripts\requirements.txt

# 3. Node.js 正常？
node --version
npm install

# 4. Unity 正常？
# 打开 Unity Hub → 创建新项目 → 选 2D Core → 点 Play ▶️
```

全部通过的话，环境就搭好啦！🎉

---

## 📏 磁盘空间估算

| 内容 | 大小 |
|------|------|
| Git | ~50 MB |
| Python | ~100 MB |
| Node.js | ~80 MB |
| VS Code | ~100 MB |
| Unity Hub | ~300 MB |
| Unity Editor 2022.3 | ~5 GB |
| **合计** | **~6 GB** |

---

> 有问题随时问喵～ (´▽`ʃ♡ƪ)
