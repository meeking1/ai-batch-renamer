using System;
using System.Collections.Generic;

namespace AiBatchRenamer.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            var tests = new List<Action>
            {
                RenameCoreTests.MultiNamePreview_AppliesNamesAndKeepsExtensions,
                RenameCoreTests.MultiNamePreview_MarksMissingNamesInvalid,
                RenameCoreTests.Validation_DetectsDuplicateProposedNames,
                RenameCoreTests.NaturalLanguagePreview_ParsesReplaceInstruction,
                RenameExecutionTests.ExecuteAndUndo_RenamesFilesAndRestoresOriginalNames,
                DeepSeekAiNamingServiceTests.ParseNamingResult_ReturnsItems_WhenJsonIsValid,
                DeepSeekAiNamingServiceTests.ParseNamingResult_RejectsDuplicateIndexes,
                DeepSeekAiNamingServiceTests.ParseNamingResult_RejectsEmptyNames,
                AppSettingsServiceTests.SaveLoadAndClear_PreservesModelAndRemovesKey
            };

            var passed = 0;
            foreach (var test in tests)
            {
                try
                {
                    test();
                    passed++;
                    Console.WriteLine("PASS " + test.Method.Name);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("FAIL " + test.Method.Name);
                    Console.Error.WriteLine(ex);
                    return 1;
                }
            }

            Console.WriteLine(string.Format("{0}/{1} tests passed", passed, tests.Count));
            return 0;
        }
    }
}
