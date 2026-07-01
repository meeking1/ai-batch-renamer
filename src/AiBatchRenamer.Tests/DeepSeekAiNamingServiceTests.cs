using System;
using System.Net;
using AiBatchRenamer.Infrastructure.Services;

namespace AiBatchRenamer.Tests
{
    internal static class DeepSeekAiNamingServiceTests
    {
        public static void ParseNamingResult_ReturnsItems_WhenJsonIsValid()
        {
            var json = "{\"items\":[{\"index\":1,\"newBaseName\":\"合同-北京\"},{\"index\":2,\"newBaseName\":\"合同-上海\"}],\"warning\":\"\"}";

            var result = DeepSeekAiNamingService.ParseNamingResult(json, 2);

            TestAssert.Equal(2, result.Items.Count, "AI result item count");
            TestAssert.Equal("合同-北京", result.Items[0].NewBaseName, "first AI result name");
            TestAssert.Equal("合同-上海", result.Items[1].NewBaseName, "second AI result name");
        }

        public static void ParseNamingResult_RejectsDuplicateIndexes()
        {
            var json = "{\"items\":[{\"index\":1,\"newBaseName\":\"A\"},{\"index\":1,\"newBaseName\":\"B\"}],\"warning\":\"\"}";

            ExpectInvalidOperation(
                () => DeepSeekAiNamingService.ParseNamingResult(json, 2),
                "重复");
        }

        public static void ParseNamingResult_RejectsEmptyNames()
        {
            var json = "{\"items\":[{\"index\":1,\"newBaseName\":\" \"}],\"warning\":\"\"}";

            ExpectInvalidOperation(
                () => DeepSeekAiNamingService.ParseNamingResult(json, 1),
                "空文件名");
        }

        public static void IsRetryableWebException_ReturnsTrue_ForTimeout()
        {
            var ex = new WebException("timeout", null, WebExceptionStatus.Timeout, null);

            TestAssert.True(DeepSeekAiNamingService.IsRetryableWebException(ex), "timeout should be retryable");
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                TestAssert.True(ex.Message.Contains(expectedMessage), "exception message should describe validation failure");
                return;
            }

            throw new InvalidOperationException("Expected InvalidOperationException was not thrown.");
        }
    }
}
