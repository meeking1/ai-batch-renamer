using System.Collections.Generic;
using System;
using System.IO;
using AiBatchRenamer.Core.Models;
using AiBatchRenamer.Core.Services;

namespace AiBatchRenamer.Tests
{
    internal static class RenameCoreTests
    {
        public static void MultiNamePreview_AppliesNamesAndKeepsExtensions()
        {
            var items = CreateItems("a.txt", "b.txt");
            var service = new MultiNamePreviewService();

            service.ApplyNames(items, "合同-北京\r\n合同-上海", true);
            new RenameValidationService().Validate(items);

            TestAssert.Equal("合同-北京.txt", items[0].ProposedName, "first proposed name");
            TestAssert.Equal("合同-上海.txt", items[1].ProposedName, "second proposed name");
            TestAssert.Equal(RenameStatus.Ready, items[0].Status, "first status");
            TestAssert.Equal(RenameStatus.Ready, items[1].Status, "second status");
        }

        public static void MultiNamePreview_MarksMissingNamesInvalid()
        {
            var items = CreateItems("a.txt", "b.txt");
            var service = new MultiNamePreviewService();

            service.ApplyNames(items, "只有一个名称", true);
            new RenameValidationService().Validate(items);

            TestAssert.Equal(RenameStatus.Invalid, items[1].Status, "missing name status");
            TestAssert.True(items[1].Message.Contains("缺少"), "missing name message");
        }

        public static void Validation_DetectsDuplicateProposedNames()
        {
            var items = CreateItems("a.txt", "b.txt");
            items[0].ProposedBaseName = "same";
            items[1].ProposedBaseName = "same";

            new RenameValidationService().Validate(items);

            TestAssert.Equal(RenameStatus.Conflict, items[0].Status, "first duplicate status");
            TestAssert.Equal(RenameStatus.Conflict, items[1].Status, "second duplicate status");
        }

        public static void Validation_AllowsSelectedFilesToSwapNames()
        {
            var root = Path.Combine(Path.GetTempPath(), "AiBatchRenamerCoreSwapTests-" + Guid.NewGuid().ToString("N"));
            var items = new List<RenameItem>
            {
                new RenameItem(Path.Combine(root, "a.txt")) { Index = 1, Status = RenameStatus.Pending, ProposedBaseName = "b" },
                new RenameItem(Path.Combine(root, "b.txt")) { Index = 2, Status = RenameStatus.Pending, ProposedBaseName = "a" }
            };

            new RenameValidationService().Validate(items);

            TestAssert.Equal(RenameStatus.Ready, items[0].Status, "first swap status");
            TestAssert.Equal(RenameStatus.Ready, items[1].Status, "second swap status");
        }

        public static void Validation_RejectsReservedWindowsDeviceNames()
        {
            var items = CreateItems("a.txt", "b.txt");
            items[0].ProposedBaseName = "CON.txt";
            items[1].ProposedBaseName = "LPT1";

            new RenameValidationService().Validate(items);

            TestAssert.Equal(RenameStatus.Invalid, items[0].Status, "CON status");
            TestAssert.Equal(RenameStatus.Invalid, items[1].Status, "LPT1 status");
        }

        public static void Validation_MarksInvalidCharactersWithoutThrowing()
        {
            var items = CreateItems("a.txt");
            items[0].ProposedBaseName = "bad\"name";

            new RenameValidationService().Validate(items);

            TestAssert.Equal(RenameStatus.Invalid, items[0].Status, "invalid character status");
        }

        public static void NaturalLanguagePreview_ParsesReplaceInstruction()
        {
            var items = CreateItems("产品-草稿.txt");

            new NaturalLanguagePreviewService().ApplyInstruction(items, "把草稿替换成正式版", true);
            new RenameValidationService().Validate(items);

            TestAssert.Equal("产品-正式版.txt", items[0].ProposedName, "replace proposed name");
            TestAssert.Equal(RenameStatus.Ready, items[0].Status, "replace status");
        }

        public static void NaturalLanguagePreview_AppliesMultipleLocalReplaceRules()
        {
            var items = CreateItems("E6108double.jpg", "E6175JM.jpg", "E6216.jpg");

            new NaturalLanguagePreviewService().ApplyInstruction(items, "double改成双色\r\nJM改成睫毛", true);
            new RenameValidationService().Validate(items);

            TestAssert.Equal("E6108双色.jpg", items[0].ProposedName, "first local replace");
            TestAssert.Equal("E6175睫毛.jpg", items[1].ProposedName, "second local replace");
            TestAssert.Equal("E6216.jpg", items[2].ProposedName, "unmatched keeps original");
            TestAssert.Equal(RenameStatus.Ready, items[0].Status, "first local replace status");
            TestAssert.Equal(RenameStatus.Ready, items[1].Status, "second local replace status");
            TestAssert.Equal(RenameStatus.Unchanged, items[2].Status, "unmatched status");
        }

        public static void NaturalLanguagePreview_AcceptsReplaceSynonyms()
        {
            var synonyms = new[]
            {
                "JM改成睫毛",
                "JM改为睫毛",
                "JM替换成睫毛",
                "JM替换为睫毛",
                "JM变成睫毛",
                "JM替换睫毛",
                "把JM改为睫毛",
                "将JM替换为睫毛"
            };

            for (var i = 0; i < synonyms.Length; i++)
            {
                var items = CreateItems("E6175JM.jpg", "E6216.jpg");

                new NaturalLanguagePreviewService().ApplyInstruction(items, synonyms[i], true);
                new RenameValidationService().Validate(items);

                TestAssert.Equal("E6175睫毛.jpg", items[0].ProposedName, "synonym replace proposed name " + synonyms[i]);
                TestAssert.Equal("E6216.jpg", items[1].ProposedName, "synonym unmatched keeps original " + synonyms[i]);
                TestAssert.Equal(RenameStatus.Ready, items[0].Status, "synonym replace status " + synonyms[i]);
                TestAssert.Equal(RenameStatus.Unchanged, items[1].Status, "synonym unmatched status " + synonyms[i]);
            }
        }

        public static void TemplatePreview_RendersNameFolderAndPaddedIndex()
        {
            var root = Path.Combine(Path.GetTempPath(), "AiBatchRenamerTemplateTests-" + Guid.NewGuid().ToString("N"));
            var item = new RenameItem(Path.Combine(root, "folder-a", "合同.txt"))
            {
                Index = 1,
                Status = RenameStatus.Pending
            };
            var items = new List<RenameItem> { item };

            new TemplatePreviewService().ApplyTemplate(items, "{folder}-{name}-{index:000}", true);

            TestAssert.Equal("folder-a-合同-001.txt", items[0].ProposedName, "template proposed name");
        }

        private static List<RenameItem> CreateItems(params string[] names)
        {
            var items = new List<RenameItem>();
            var root = Path.Combine(Path.GetTempPath(), "AiBatchRenamerCoreTests-" + Guid.NewGuid().ToString("N"));
            for (var i = 0; i < names.Length; i++)
            {
                var item = new RenameItem(Path.Combine(root, names[i]))
                {
                    Index = i + 1,
                    Status = RenameStatus.Pending
                };
                items.Add(item);
            }

            return items;
        }
    }
}
