# AI 接入设计

## 当前状态

当前版本已接入 DeepSeek OpenAI-compatible Chat Completions 接口。

默认配置：

```text
Base URL: https://api.deepseek.com
Endpoint: https://api.deepseek.com/chat/completions
Model: deepseek-v4-flash
Thinking: disabled
```

当 API Key 未配置时，程序使用本地规则兜底：

- 提取引号里的命名主题
- 自动追加 `001`、`002` 编号
- 支持简单“把 A 替换成 B”的中文表达

## 目标

后续接入 AI 后，用户可以输入更自由的自然语言，例如：

```text
这些是客户合同扫描件，请根据原文件名里的城市和日期命名为“城市-客户合同-日期”
```

AI 返回结构化的新文件名建议，本地程序负责校验和执行。

## 当前接口

```csharp
public interface IAiNamingService
{
    bool IsConfigured { get; }
    Task<AiNamingResult> GenerateAsync(AiNamingRequest request);
}
```

请求模型：

```csharp
public class AiNamingRequest
{
    public string Instruction { get; set; }
    public IList<AiNamingFile> Files { get; set; }
    public bool KeepExtension { get; set; }
}

public class AiNamingFile
{
    public int Index { get; set; }
    public string Name { get; set; }
    public string Extension { get; set; }
    public DateTime LastWriteTime { get; set; }
}
```

返回模型：

```csharp
public class AiNamingResult
{
    public IList<AiNamingItem> Items { get; set; }
    public string Warning { get; set; }
}

public class AiNamingItem
{
    public int Index { get; set; }
    public string NewBaseName { get; set; }
}
```

接口使用异步方法，避免 UI 阻塞。

## Prompt 要求

必须要求模型只返回 JSON：

```json
{
  "items": [
    { "index": 1, "newBaseName": "项目资料-001" }
  ],
  "warning": ""
}
```

请求会设置：

```json
{
  "response_format": { "type": "json_object" },
  "thinking": { "type": "disabled" }
}
```

重命名任务优先要稳定、快速、可解析的 JSON，因此默认关闭 thinking mode。

程序收到结果后还要做本地校验：

- 数量是否一致
- `index` 是否存在
- 新名称是否为空
- 是否包含非法字符
- 是否重复
- 是否会覆盖已有文件

## 隐私原则

MVP 阶段默认只发送：

- 文件名
- 扩展名
- 文件排序
- 可选时间戳

默认不发送：

- 文件内容
- 完整本地路径
- 用户系统信息

## Windows 7 注意事项

Windows 7 调云端 API 可能遇到 TLS 1.2 问题，需要：

- 要求 Windows 7 SP1
- 安装 .NET Framework 4.8
- 启用 TLS 1.2
- 测试目标 API 域名证书链

可在程序启动时设置：

```csharp
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
```

## API Key 存储

不要明文保存在配置文件。

当前实现：

1. 优先读取环境变量 `DEEPSEEK_API_KEY`
2. 如果环境变量不存在，读取 `%APPDATA%\AiBatchRenamer\settings.json` 中的 DPAPI 加密密钥

推荐顺序仍然是：

1. Windows Credential Manager
2. DPAPI 加密后保存到 `%APPDATA%`
3. 企业部署时使用环境变量
