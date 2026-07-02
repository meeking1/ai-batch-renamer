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
                RenameCoreTests.Validation_AllowsSelectedFilesToSwapNames,
                RenameCoreTests.Validation_RejectsReservedWindowsDeviceNames,
                RenameCoreTests.Validation_MarksInvalidCharactersWithoutThrowing,
                RenameCoreTests.NaturalLanguagePreview_ParsesReplaceInstruction,
                RenameCoreTests.NaturalLanguagePreview_AppliesMultipleLocalReplaceRules,
                RenameCoreTests.NaturalLanguagePreview_AcceptsReplaceSynonyms,
                RenameExecutionTests.ExecuteAndUndo_RenamesFilesAndRestoresOriginalNames,
                RenameExecutionTests.Execute_SwapsFileNamesAndUndoRestoresOriginals,
                RenameExecutionTests.Execute_MixedReadyAndUnchanged_RenamesReadyItems,
                RenameExecutionTests.Execute_SkipsConflictItemsAndRenamesReadyItems,
                RenameExecutionTests.UndoSpecificLog_DoesNotClearLatestPointer,
                RenameExecutionTests.Undo_CaseOnlyRename_DoesNotTreatOriginalAsConflict,
                RenameExecutionTests.ListRecent_ReturnsSavedOperationLogs,
                DeepSeekAiNamingServiceTests.ParseNamingResult_ReturnsItems_WhenJsonIsValid,
                DeepSeekAiNamingServiceTests.ParseNamingResult_RejectsDuplicateIndexes,
                DeepSeekAiNamingServiceTests.ParseNamingResult_RejectsEmptyNames,
                DeepSeekAiNamingServiceTests.IsRetryableWebException_ReturnsTrue_ForTimeout,
                AppSettingsServiceTests.SaveLoadAndClear_PreservesModelAndRemovesKey,
                RenameCoreTests.TemplatePreview_RendersNameFolderAndPaddedIndex,
                PreviewCsvExporterTests.Export_WritesEscapedCsvRows
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
