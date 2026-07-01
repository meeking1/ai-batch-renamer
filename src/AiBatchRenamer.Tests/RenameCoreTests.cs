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

        public static void NaturalLanguagePreview_ParsesReplaceInstruction()
        {
            var items = CreateItems("产品-草稿.txt");

            new NaturalLanguagePreviewService().ApplyInstruction(items, "把草稿替换成正式版", true);
            new RenameValidationService().Validate(items);

            TestAssert.Equal("产品-正式版.txt", items[0].ProposedName, "replace proposed name");
            TestAssert.Equal(RenameStatus.Ready, items[0].Status, "replace status");
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
