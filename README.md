# AI Batch Renamer

一款面向 Windows 7 SP1、Windows 10、Windows 11 的批量文件重命名桌面程序。

当前版本是 MVP 原型，已实现：

- 添加文件
- 添加文件夹中的一级文件
- 拖拽文件或文件夹
- 可选递归添加子文件夹
- 移除选中文件
- 上移/下移调整文件顺序
- 按文件名、修改时间、大小排序
- 自然语言规则预览，本地启发式实现
- 多名称逐行替换
- 查找替换
- 模板命名
- DeepSeek AI 自然语言命名
- 重命名前校验非法字符、重复名称、目标冲突、路径过长
- 拦截 Windows 保留设备名，例如 `CON`、`PRN`、`AUX`、`NUL`、`COM1`、`LPT1`
- 执行前确认
- 导出重命名预览 CSV
- JSON 操作日志
- 打开操作日志目录
- 撤销上一次成功的批量重命名

## 技术栈

- C#
- WPF
- .NET Framework 4.8
- 无第三方 NuGet 依赖

选择 .NET Framework 4.8 是为了兼容 Windows 7 SP1、Windows 10 和 Windows 11。Windows 7 机器需要先安装 .NET Framework 4.8 运行时。安装包会在安装前检查 .NET Framework 4.8，缺失时会提示用户先安装运行时。

## 项目结构

```text
.
├─ AiBatchRenamer.sln
├─ src/
│  ├─ AiBatchRenamer.App/             # WPF 桌面程序
│  ├─ AiBatchRenamer.Core/            # 重命名预览、校验、规则
│  └─ AiBatchRenamer.Infrastructure/  # 文件系统执行、日志、撤销
├─ docs/
├─ installer/
└─ AI批量重命名文件程序规划.md
```

## 构建

推荐环境：

- Windows 10 或 Windows 11
- Visual Studio 2022
- .NET Framework 4.8 Developer Pack
- 打包安装包需要 Inno Setup

命令行构建：

```powershell
msbuild AiBatchRenamer.sln /p:Configuration=Release
```

或使用仓库内脚本：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-windows.ps1
```

构建并运行核心测试：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-windows.ps1 -RunTests
```

构建、测试并生成安装包，需要先安装 Inno Setup：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-windows.ps1 -RunTests -BuildInstaller
```

构建产物：

```text
src\AiBatchRenamer.App\bin\Release\AiBatchRenamer.exe
```

仓库也包含 GitHub Actions Windows 构建配置：

```text
.github\workflows\windows-build.yml
```

CI 会同时运行 `AiBatchRenamer.Tests.exe`，覆盖核心重命名规则、校验、执行和撤销，并生成 Inno Setup 安装包。

## 使用

1. 打开程序
2. 点击“添加文件”或把文件拖入窗口
3. 如需多名称逐行替换，可先用“上移”“下移”调整文件顺序
4. 选择重命名模式
5. 输入命名要求或名称列表
6. 点击“生成预览”
7. 确认所有状态正常
8. 点击“确认重命名”

## 三种命名模式

### 自然语言规则

配置 DeepSeek API Key 后，程序会调用 DeepSeek 生成文件名预览。未配置 API Key 时，程序使用本地启发式规则兜底。

默认模型：

```text
deepseek-v4-flash
```

API Key 不会写入代码仓库。程序支持两种配置方式：

1. 在右侧“DeepSeek 设置”中输入 API Key 并保存，程序会使用 Windows DPAPI 按当前用户加密保存。
2. 设置环境变量 `DEEPSEEK_API_KEY`，程序会优先读取环境变量。

保存后可以点击“测试 DeepSeek”验证连接；如需删除本机保存的密钥，点击“清除本机 Key”。

示例：

```text
命名为“项目资料”
```

会生成：

```text
项目资料-001.ext
项目资料-002.ext
```

也支持简单替换表达：

```text
把草稿替换成正式版
```

### 多名称逐行替换

每行一个新名称，按当前文件顺序匹配。

```text
合同-北京客户
合同-上海客户
合同-广州客户
```

程序默认保留原扩展名。

### 查找替换

在右侧“查找替换”区域输入查找内容和替换内容，然后生成预览。

### 模板命名

模板模式支持在输入框中使用 token：

```text
{name}-{index:000}
{folder}-{name}-{date}
```

可用 token：

- `{name}` 或 `{filename}`：原文件名，不含扩展名
- `{folder}`：所在文件夹名
- `{ext}`：扩展名，不含点
- `{date}`：文件修改日期，格式 `yyyy-MM-dd`
- `{time}`：文件修改时间，格式 `HHmmss`
- `{index}`、`{index:00}`、`{index:000}`、`{index:0000}`：当前顺序编号

## 操作日志

日志保存在：

```text
%APPDATA%\AiBatchRenamer\Logs
```

程序会保存最近一次成功执行的操作，用于撤销。

## DeepSeek 接入状态

当前版本已接入 DeepSeek OpenAI-compatible Chat Completions 接口：

```text
https://api.deepseek.com/chat/completions
```

模型默认使用：

```text
deepseek-v4-flash
```

程序会要求模型返回 JSON，并在本地再次校验结果。AI 只生成新文件名建议，不直接操作文件系统。
重命名任务默认关闭 DeepSeek thinking mode，以获得更快、更稳定的结构化 JSON 输出。

详细设计见 [docs/AI接入设计.md](docs/AI接入设计.md)。

## 安全原则

- AI 只生成文件名建议，不直接操作文件系统
- 所有真实重命名都由本地程序执行
- 执行前必须预览
- 有冲突、非法字符、缺少名称时阻止执行
- 默认不上传文件内容

隐私和数据说明见 [PRIVACY.md](PRIVACY.md)。
