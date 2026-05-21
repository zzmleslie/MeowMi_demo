# 🐱 Git 协作指南 — el不知道取啥名的开发组

## 写在前面：最最重要的！！（！！不是双阶乘） 做好版本管理！！做好版本管理！！做好版本管理！！每次写完代码，都要 commit 一次。这样能保留修改记录，随时回滚到之前的版本，避免踩坑。and 每次 commit 写清楚这一版修改了什么内容，添加/删减了什么功能！！这个真的很重要！！
---

## 🤔 Git 是什么？

```
你的电脑                     GitHub（云端）
   │                            │
   ├── git commit  → 本地存档    │
   ├── git push    → ─────────→  ☁️ 同步到云端
   └── git pull    ← ─────────   ☁️ 拉取队友更新
```

每个项目文件夹里有个隐藏的 `.git/` 目录，存着所有版本记录。**不同项目互不影响。**

---

## 🐛 今天踩的坑 & 解决方案

### 坑 1：`remote origin already exists`

```bash
$ git remote add origin git@github.com:xxx/xxx.git
error: remote origin already exists.
```

**原因**：项目已经关联过一个远程地址了。

**修复**：
```bash
# 看当前连的是谁
git remote -v

# 改地址（不改名）
git remote set-url origin 新地址
```

---

### 坑 2：`Failed to connect to github.com port 443`

```bash
$ git push -u origin main
fatal: unable to access 'https://github.com/...':
Failed to connect to github.com port 443
```

**原因**：网络连不上 GitHub（常见于校园网/公司网）

**修复方案（按顺序试）**：

```bash
# 方案一：用 SSH 代替 HTTPS（推荐，一劳永逸）
# 1. 生成 SSH 密钥（如果没有的话）
ssh-keygen -t ed25519 -C "你的邮箱@example.com"
# 一路回车就行

# 2. 复制公钥
cat ~/.ssh/id_ed25519.pub

# 3. 去 GitHub → Settings → SSH Keys → 粘贴保存

# 4. 改用 SSH 地址
git remote set-url origin git@github.com:zzmleslie/MeowMi_demo.git
git push -u origin main
```

```bash
# 方案二：开代理（如果你有梯子）
git config --global http.proxy http://127.0.0.1:7890
git config --global https.proxy http://127.0.0.1:7890

# 取消代理
git config --global --unset http.proxy
git config --global --unset https.proxy
```

```bash
# 方案三：手机热点（最简单粗暴）
# 电脑连手机热点 → 重新 git push
```

---

### 坑 3：GitHub 给的默认指令有问题

GitHub 建完空仓库会给这些指令：

```bash
echo "# xxx" >> README.md    # ❌ 会覆盖你已有的 README！
git add README.md            # ❌ 只加了 README，漏掉其他文件
git branch -M 奇怪的分支名    # ❌ 分支名可能不对
```

**正确的首次上传**：
```bash
git init                     # 仅首次，初始化仓库
git add .                    # 加所有文件（不是只加 README）
git commit -m "first commit" # 提交
git branch -M main           # 分支改名为 main
git remote add origin 你的仓库地址
git push -u origin main
```

---

## 📋 日常工作流

### 🆕 第一次：从 GitHub 拉取项目

```bash
# 克隆到本地
git clone https://github.com/zzmleslie/MeowMi_demo.git

# 或 SSH（推荐）
git clone git@github.com:zzmleslie/MeowMi_demo.git

cd MeowMi_demo
```

### 📝 每次写代码前：同步队友更新

```bash
git pull                     # 拉取最新代码
```

> ⚠️ 养成习惯：**写代码前先 pull，避免冲突！**

### ✏️ 写完代码后：提交 + 推送

```bash
# 1. 看改了哪些文件
git status

# 2. 添加修改的文件
git add .                    # 加所有修改
# 或
git add 具体文件名           # 只加某个文件

# 3. 提交（写清楚改了什么）
git commit -m "✨ 新增猫咪NPC待机动画"
git commit -m "🐛 修复摇杆漂移bug"
git commit -m "📝 更新README"

# 4. 推送到 GitHub
git push
```

### 🏷️ 提交信息规范（建议）

| 前缀 | 含义 | 示例 |
|------|------|------|
| ✨ `新增` | 新功能 | `✨ 新增社区捐款模块` |
| 🐛 `修复` | 修 bug | `🐛 修复地图边界越界` |
| 🎨 `优化` | 改进代码/UI | `🎨 优化水彩渲染性能` |
| 📝 `文档` | 改 README/注释 | `📝 更新设计文档` |
| 🔧 `配置` | 改设置 | `🔧 更新 .gitignore` |
| 🚀 `发布` | 里程碑 | `🚀 v0.2 发布` |

---

## 🔧 常用命令速查

### 查看历史

```bash
git log --oneline           # 简洁版提交历史
git log --graph --oneline   # 带分支图的提交历史
git log --since="2 days ago" # 最近两天的提交
git log --author="队友名"   # 看某个人的提交
```

### 看看谁写的 — `git blame` 🔍

```bash
# 查看某文件每行是谁写的、什么时候改的
git blame Assets/Scripts/Core/GameManager.cs

# 只看某几行
git blame -L 10,50 GameManager.cs

# 忽略空白改动
git blame -w GameManager.cs
```

> 💡 超实用场景：「这行代码谁写的？为什么这么写？」→ `git blame` 找到作者 → 去问他！

### 撤销操作

```bash
# 撤销还没 add 的修改（恢复到上次 commit 状态）
git checkout -- 文件名
git checkout -- .           # 撤销所有

# 撤销已经 add 但还没 commit 的
git reset HEAD 文件名
git reset HEAD .            # 撤销所有 add

# 撤销上次 commit（保留修改）
git reset --soft HEAD~1

# 修改上次的 commit 信息
git commit --amend -m "新的提交信息"
```

### 分支操作

```bash
git branch                  # 查看所有分支
git branch 新分支名         # 创建分支
git switch 分支名           # 切换分支
git switch -c 新分支名      # 创建 + 切换
git merge 分支名            # 合并分支到当前分支
git branch -d 分支名        # 删除分支
```

### 暂存 — `git stash` 👜

```bash
# 写到一半要切分支/拉代码，但又不想提交？
git stash                   # 暂存当前修改
git stash pop               # 恢复暂存的修改
git stash list              # 查看暂存列表
```

### 同步

```bash
git pull                    # 拉取 + 自动合并
git fetch                   # 只拉取，不合并（安全）
git push                    # 推送
git push -f                 # 强制推送 ⚠️ 危险！会覆盖队友代码
```

---

## 🎯 常见场景速查

| 场景 | 命令 |
|------|------|
| 🆕 克隆项目 | `git clone 地址` |
| 📥 拉取最新 | `git pull` |
| 📤 提交推送 | `git add .` → `git commit -m "..."` → `git push` |
| 🔍 看谁写的 | `git blame 文件名` |
| ⏪ 撤销未提交 | `git checkout -- .` |
| 📦 暂存半成品 | `git stash` → 忙完 → `git stash pop` |
| 🌿 开新分支 | `git switch -c 分支名` |
| 🔗 改远程地址 | `git remote set-url origin 新地址` |

---

## ⚠️ 黄金法则

> 1. **写代码前先 `git pull`**——避免冲突
> 2. **`git push -f` 是核武器**——除非你确定知道在干嘛，否则别用
> 3. **提交信息写清楚**——三个月后的你会感谢现在的你
> 4. **一个 `.gitignore` 保平安**——不要把 node_modules、Unity 缓存传上去
> 5. **遇到冲突别慌**——叫我来帮你解决

---


> 每个 Git 高手都是从 `fatal: ` 和 `error: ` 中成长起来的，踩坑越多越强 💪

---

🐾 *Git 不可怕，可怕的是不 commit 就关机www*
