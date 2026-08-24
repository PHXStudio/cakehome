# CakeHome Git 协作规范

> 🔴 本文件为团队强制规范。违反会导致**凭据泄漏**或**提交作者混乱**，请熟读并遵守。

---

## 1. 🔴 严禁把凭据写入任何会进入仓库/共享的地方

**禁止**：
- 在 `git remote` URL 里嵌入 token（`https://oauth2:<TOKEN>@github.com/...`）
- 把 `.env`、`config.py`、`config.json`、密钥文件、`*.pem` 提交进 git
- 在代码/配置文件里硬编码密码、API Key、token（应从环境变量、服务器配置或 Unity Resources 配置读取）

**正确认证方式**（二选一）：
```bash
# 方式一：GitHub CLI（推荐）
gh auth login -h github.com

# 方式二：Git Credential Manager（Windows 自带，凭据存系统）
# 首次 push/pull 时弹出登录框，自动保存
```

远程 URL 应该是**无凭据**的纯地址：
```bash
git remote set-url origin https://github.com/PHXStudio/cakehome.git
```

## 2. 敏感信息防护（已内置）

仓库已配置 `.githooks/pre-commit`，会在提交时扫描 token/密钥，命中即阻止。

**每个开发者 clone 后启用一次**：
```bash
git config core.hooksPath .githooks
```

**验证是否生效**：
```bash
echo 'fake_token = "sk-ABC1234567890abcdefghijklmnopqrstuvwxyz"' > _hook_test.txt
git add -f _hook_test.txt && git commit -m "test"   # 应被 pre-commit 拦截
rm -f _hook_test.txt
```

敏感文件（`.env`、密钥等）建议加入 `.gitignore`；**强制防线是上面的 pre-commit hook**——无论 `.gitignore` 是否覆盖，hook 都会扫描本次暂存内容并阻止提交。

**临时绕过**（确认内容安全才可用）：
```bash
git commit --no-verify
```

## 3. 提交作者身份规范（防止"都显示成一个人"）

**规则：每个提交必须使用提交者本人的 Git 身份。**

- 本机全局身份设置为你自己的账号：
  ```bash
  git config --global user.name "你的名字"
  git config --global user.email "you@example.com"
  ```
- **共用同一台开发机**时，提交前先确认身份：
  ```bash
  git config user.name && git config user.email   # 查看当前生效身份
  ```
  每次提交都应是本人身份。单人临时借用他人机器时，用单次覆盖：
  ```bash
  git -c user.name="你的名字" -c user.email="you@example.com" commit -m "msg"
  ```
- **严禁**修改/抹除他人已提交记录的作者（除非走团队评审的 rebase 流程）。

## 4. 分支与提交建议

- 功能开发开分支，`main` 仅接受评审后合入
- 提交信息用中文，前缀标注模块，如 `商店：修复保鲜折扣计算`
- 涉及数据库/API 契约改动，先与后端同步评估影响

## 5. 违规处理

- 已泄漏的 token：**立即在 GitHub 吊销并重新生成**（Settings → Developer settings → Personal access tokens）
- 已误提交的敏感文件：参考 Git 官方改写历史流程（`git filter-repo`），并吊销对应凭据
