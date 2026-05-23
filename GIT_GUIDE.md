# 🐱 Git 协作指南 — 喵喵喵喵~开发组

## 写在前面：最最重要的！！（！！不是双阶乘） 做好版本管理！！做好版本管理！！做好版本管理！！每次写完代码，都要 commit 一次。这样能保留修改记录，随时回滚到之前的版本，避免踩坑。and 每次 commit 都要在 -m 后面的 “ ” 里面写清楚这一版修改了什么内容，添加/删减了什么功能！！这个真的很重要！！(一个小小的提醒，在git cli中用ctrl + insert 复制 ctrl + c 不是复制，而是中断命令，所以如果要复制粘贴命令，记得用鼠标右键复制或者用shift + insert 粘贴)
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
git remote set-url origin git@github.com:zzmleslie/meowmeowmeowmeow.git
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
git clone https://github.com/zzmleslie/meowmeowmeowmeow.git

# 或 SSH（推荐）
git clone git@github.com:zzmleslie/meowmeowmeowmeow.git

cd meowmeowmeowmeow
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
| 🎨 `优化` | 改进代码/UI | `🎨 优化像素渲染性能` |
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

### 🔄 日常协作流程

```bash
# ① 写代码前拉一下（防止冲突）
git pull

# ② 写代码...

# ③ 提交 + 推送
git add .
git commit -m "✨ 干了什么"
git push
```

> ⚠️ 如果 push 报错说"远程有更新"，说明队友刚 push 了 → 再 `git pull` 一次 → 再 `git push`

---

### 🔀 冲突解决（偶尔发生，别怕）

你和队友同时改了同一个文件的同一行时发生：

```bash
git pull
# Git 提示 CONFLICT，打开冲突文件，找到：
#    <<<<<<< HEAD
#    你的代码
#    =======
#    队友的代码
#    >>>>>>>

# 手动编辑 → 保留正确的 → 删掉 <<<< ==== >>>> 标记
git add .
git commit -m "🔀 解决冲突"
git push
```

```
你和队友同时改了同一个文件的同一行：

  你的版本：  猫咪移动速度 = 200
  队友版本：  猫咪移动速度 = 150
  
  Git：(´･ω･`) 我不知道该听谁的...
```

**解决步骤**：

```bash
# 1. 先 pull
git pull

# 2. Git 提示 CONFLICT，打开冲突文件
#    你会看到：
#    <<<<<<< HEAD
#    猫咪移动速度 = 200       ← 你的
#    =======
#    猫咪移动速度 = 150       ← 队友的
#    >>>>>>> origin/dev

# 3. 手动编辑，保留正确的（或合并两者），删掉标记符号
#    猫咪移动速度 = 180       ← 折中方案

# 4. 标记已解决 + 提交
git add .
git commit -m "🔀 合并冲突：统一移动速度为180"
git push
```

> 💡 减少冲突的秘诀：**频繁 pull + 小步提交 + 分文件写**


---

## 写在最后：每个 Git 高手都是从 `fatal: ` 和 `error: ` 中成长起来的，踩坑越多越强 💪加油吧，我们共同成长捏~

---

