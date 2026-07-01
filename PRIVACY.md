# 隐私与数据说明

AI Batch Renamer 默认只处理本机文件路径和文件名。

## DeepSeek 调用会发送什么

当用户配置 DeepSeek API Key，并使用“自然语言规则”生成预览时，程序会发送：

- 用户输入的重命名要求
- 文件序号
- 原文件名
- 文件扩展名

程序不会发送：

- 文件内容
- 完整文件路径
- 文件夹结构
- Windows 用户名
- API Key 到除 DeepSeek API 以外的第三方

## API Key 存储

程序支持两种方式：

1. 环境变量 `DEEPSEEK_API_KEY`
2. 在设置区输入后保存到 `%APPDATA%\AiBatchRenamer\settings.json`

本机保存时，API Key 会使用 Windows DPAPI 按当前 Windows 用户加密。仓库、日志、CSV 预览中不会写入 API Key。

## 本地日志

重命名操作日志保存在：

```text
%APPDATA%\AiBatchRenamer\Logs
```

日志包含：

- 原路径
- 新路径
- 执行状态
- 错误信息

日志用于撤销和排查问题。用户可以在程序中点击“打开日志”查看。

## 文件内容

当前版本不会读取或上传文件内容。后续如果增加 PDF、图片 EXIF、Office 文档属性等元数据能力，应在界面和文档中明确提示用户。
