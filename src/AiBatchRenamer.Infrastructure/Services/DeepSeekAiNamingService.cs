using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using AiBatchRenamer.Core.Models;
using AiBatchRenamer.Core.Services;

namespace AiBatchRenamer.Infrastructure.Services
{
    public class DeepSeekAiNamingService : IAiNamingService
    {
        private const string Endpoint = "https://api.deepseek.com/chat/completions";
        private readonly AppSettingsService settingsService;

        public DeepSeekAiNamingService(AppSettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public bool IsConfigured
        {
            get
            {
                var settings = settingsService.Load();
                return !string.IsNullOrWhiteSpace(settings.DeepSeekApiKey);
            }
        }

        public async Task<AiNamingResult> GenerateAsync(AiNamingRequest request)
        {
            var settings = settingsService.Load();
            if (string.IsNullOrWhiteSpace(settings.DeepSeekApiKey))
            {
                throw new InvalidOperationException("DeepSeek API Key 未配置");
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var chatRequest = CreateChatCompletionRequest(request, settings.DeepSeekModel);
            var requestJson = Serialize(chatRequest);
            var responseJson = await PostJsonAsync(requestJson, settings.DeepSeekApiKey);
            var response = Deserialize<ChatCompletionResponse>(responseJson);

            if (response == null || response.Choices == null || response.Choices.Count == 0)
            {
                throw new InvalidOperationException("DeepSeek 未返回有效结果");
            }

            var content = response.Choices[0].Message == null
                ? string.Empty
                : response.Choices[0].Message.Content;

            return ParseNamingResult(content, request.Files.Count);
        }

        private static ChatCompletionRequest CreateChatCompletionRequest(AiNamingRequest request, string model)
        {
            var userPrompt = BuildUserPrompt(request);
            return new ChatCompletionRequest
            {
                Model = string.IsNullOrWhiteSpace(model) ? "deepseek-v4-flash" : model.Trim(),
                Stream = false,
                Temperature = 0.2,
                MaxTokens = 4096,
                Thinking = new ThinkingOptions { Type = "disabled" },
                ResponseFormat = new ResponseFormat { Type = "json_object" },
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Role = "system",
                        Content = "你是文件批量重命名助手。只返回合法 JSON，不要 Markdown。所有 newBaseName 都必须是不含扩展名的 Windows 文件名主体。不要包含路径分隔符。"
                    },
                    new ChatMessage
                    {
                        Role = "user",
                        Content = userPrompt
                    }
                }
            };
        }

        private static string BuildUserPrompt(AiNamingRequest request)
        {
            var builder = new StringBuilder();
            builder.AppendLine("请根据用户要求为文件生成新的文件名主体。");
            builder.AppendLine("必须返回这个 JSON 结构：{\"items\":[{\"index\":1,\"newBaseName\":\"名称\"}],\"warning\":\"\"}");
            builder.AppendLine("规则：");
            builder.AppendLine("1. items 数量必须与 files 数量一致。");
            builder.AppendLine("2. index 必须使用输入文件的 index。");
            builder.AppendLine("3. newBaseName 不要包含扩展名。");
            builder.AppendLine("4. 不要使用 Windows 文件名非法字符：< > : \" / \\\\ | ? *。");
            builder.AppendLine("5. 尽量保留用户要求中的编号、日期、关键词和顺序。");
            builder.AppendLine();
            builder.AppendLine("用户要求：");
            builder.AppendLine(request.Instruction ?? string.Empty);
            builder.AppendLine();
            builder.AppendLine("files:");

            foreach (var file in request.Files)
            {
                builder.AppendFormat(
                    "{{\"index\":{0},\"name\":\"{1}\",\"extension\":\"{2}\"}}",
                    file.Index,
                    EscapePromptValue(file.Name),
                    EscapePromptValue(file.Extension));
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string EscapePromptValue(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private async Task<string> PostJsonAsync(string requestJson, string apiKey)
        {
            var request = (HttpWebRequest)WebRequest.Create(Endpoint);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + apiKey.Trim();
            request.Timeout = 60000;

            var bytes = Encoding.UTF8.GetBytes(requestJson);
            using (var stream = await request.GetRequestStreamAsync())
            {
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }

            try
            {
                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (WebException ex)
            {
                var message = ReadErrorResponse(ex);
                throw new InvalidOperationException("DeepSeek 请求失败：" + message, ex);
            }
        }

        private static string ReadErrorResponse(WebException ex)
        {
            if (ex.Response == null)
            {
                return ex.Message;
            }

            using (var response = ex.Response)
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var body = reader.ReadToEnd();
                return string.IsNullOrWhiteSpace(body) ? ex.Message : body;
            }
        }

        private static AiNamingResult ParseNamingResult(string content, int expectedCount)
        {
            var json = ExtractJson(content);
            var result = Deserialize<AiNamingResultDto>(json);
            if (result == null || result.Items == null)
            {
                throw new InvalidOperationException("DeepSeek 返回的 JSON 格式无效");
            }

            if (result.Items.Count != expectedCount)
            {
                throw new InvalidOperationException("DeepSeek 返回数量与文件数量不一致");
            }

            return new AiNamingResult
            {
                Warning = result.Warning ?? string.Empty,
                Items = result.Items
                    .Select(item => new AiNamingItem
                    {
                        Index = item.Index,
                        NewBaseName = item.NewBaseName
                    })
                    .ToList()
            };
        }

        private static string ExtractJson(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("DeepSeek 返回内容为空");
            }

            var trimmed = content.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var firstBrace = trimmed.IndexOf('{');
                var lastBrace = trimmed.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                {
                    return trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
                }
            }

            return trimmed;
        }

        private static string Serialize<T>(T value)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static T Deserialize<T>(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (T)serializer.ReadObject(stream);
            }
        }
    }

    [DataContract]
    internal class ChatCompletionRequest
    {
        [DataMember(Name = "model", Order = 1)]
        public string Model { get; set; }

        [DataMember(Name = "messages", Order = 2)]
        public List<ChatMessage> Messages { get; set; }

        [DataMember(Name = "stream", Order = 3)]
        public bool Stream { get; set; }

        [DataMember(Name = "temperature", Order = 4)]
        public double Temperature { get; set; }

        [DataMember(Name = "max_tokens", Order = 5)]
        public int MaxTokens { get; set; }

        [DataMember(Name = "thinking", Order = 6)]
        public ThinkingOptions Thinking { get; set; }

        [DataMember(Name = "response_format", Order = 7)]
        public ResponseFormat ResponseFormat { get; set; }
    }

    [DataContract]
    internal class ChatMessage
    {
        [DataMember(Name = "role", Order = 1)]
        public string Role { get; set; }

        [DataMember(Name = "content", Order = 2)]
        public string Content { get; set; }
    }

    [DataContract]
    internal class ResponseFormat
    {
        [DataMember(Name = "type", Order = 1)]
        public string Type { get; set; }
    }

    [DataContract]
    internal class ThinkingOptions
    {
        [DataMember(Name = "type", Order = 1)]
        public string Type { get; set; }
    }

    [DataContract]
    internal class ChatCompletionResponse
    {
        [DataMember(Name = "choices", Order = 1)]
        public List<ChatChoice> Choices { get; set; }
    }

    [DataContract]
    internal class ChatChoice
    {
        [DataMember(Name = "message", Order = 1)]
        public ChatMessage Message { get; set; }
    }

    [DataContract]
    internal class AiNamingResultDto
    {
        [DataMember(Name = "items", Order = 1)]
        public List<AiNamingItemDto> Items { get; set; }

        [DataMember(Name = "warning", Order = 2)]
        public string Warning { get; set; }
    }

    [DataContract]
    internal class AiNamingItemDto
    {
        [DataMember(Name = "index", Order = 1)]
        public int Index { get; set; }

        [DataMember(Name = "newBaseName", Order = 2)]
        public string NewBaseName { get; set; }
    }
}
