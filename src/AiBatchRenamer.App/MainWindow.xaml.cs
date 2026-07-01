using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AiBatchRenamer.App.ViewModels;
using AiBatchRenamer.Core.Models;
using AiBatchRenamer.Core.Services;
using AiBatchRenamer.Infrastructure.Services;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace AiBatchRenamer.App
{
    public partial class MainWindow : Window
    {
        private readonly MultiNamePreviewService multiNamePreviewService;
        private readonly NaturalLanguagePreviewService naturalLanguagePreviewService;
        private readonly TemplatePreviewService templatePreviewService;
        private readonly RenameValidationService validationService;
        private readonly RenameExecutionService executionService;
        private readonly UndoService undoService;
        private readonly OperationLogRepository operationLogRepository;
        private readonly AppSettingsService settingsService;
        private readonly IAiNamingService aiNamingService;
        private readonly PreviewCsvExporter previewCsvExporter;

        public MainWindow()
        {
            InitializeComponent();

            multiNamePreviewService = new MultiNamePreviewService();
            naturalLanguagePreviewService = new NaturalLanguagePreviewService();
            templatePreviewService = new TemplatePreviewService();
            validationService = new RenameValidationService();
            previewCsvExporter = new PreviewCsvExporter();

            settingsService = new AppSettingsService();
            aiNamingService = new DeepSeekAiNamingService(settingsService);

            operationLogRepository = new OperationLogRepository();
            executionService = new RenameExecutionService(operationLogRepository);
            undoService = new UndoService(operationLogRepository);

            Items = new ObservableCollection<RenameItemViewModel>();
            DataContext = this;
            LoadSettingsIntoUi();
        }

        public ObservableCollection<RenameItemViewModel> Items { get; private set; }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "选择要重命名的文件"
            };

            if (dialog.ShowDialog(this) == true)
            {
                AddFiles(dialog.FileNames);
            }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new WinForms.FolderBrowserDialog())
            {
                dialog.Description = "选择包含待重命名文件的文件夹";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    var searchOption = RecursiveFolderCheckBox.IsChecked == true
                        ? SearchOption.AllDirectories
                        : SearchOption.TopDirectoryOnly;
                    AddFiles(EnumerateFilesSafe(dialog.SelectedPath, searchOption));
                }
            }
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = FilesGrid.SelectedItems
                .OfType<RenameItemViewModel>()
                .ToList();

            foreach (var item in selectedItems)
            {
                Items.Remove(item);
            }

            ReindexItems();
            UpdateStatus(string.Format("已移除 {0} 个文件，当前共 {1} 个。", selectedItems.Count, Items.Count));
        }

        private void MoveSelectedUp_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedItems(-1);
        }

        private void MoveSelectedDown_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedItems(1);
        }

        private void ExportPreview_Click(object sender, RoutedEventArgs e)
        {
            if (Items.Count == 0)
            {
                UpdateStatus("没有可导出的文件。");
                return;
            }

            validationService.Validate(Items.Select(item => item.Model).ToList());
            RefreshItems();

            var dialog = new SaveFileDialog
            {
                Title = "导出重命名预览",
                Filter = "CSV 文件 (*.csv)|*.csv",
                FileName = "rename-preview.csv"
            };

            if (dialog.ShowDialog(this) == true)
            {
                previewCsvExporter.Export(dialog.FileName, Items.Select(item => item.Model));
                UpdateStatus("已导出预览：" + dialog.FileName);
            }
        }

        private void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(operationLogRepository.LogDirectory);
            Process.Start(operationLogRepository.LogDirectory);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Items.Clear();
            UpdateStatus("已清空文件列表。");
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            var result = undoService.UndoLatest();
            UpdateStatus(string.Format("{0} 成功：{1}，失败：{2}", result.Message, result.SuccessCount, result.FailedCount));
            MessageBox.Show(this, MainWindowStatus.Text, "撤销结果", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (Items.Count == 0)
            {
                UpdateStatus("请先添加文件。");
                return;
            }

            var models = Items.Select(viewModel => viewModel.Model).ToList();
            var sanitize = SanitizeCheckBox.IsChecked == true;

            if (ModeComboBox.SelectedIndex == 1)
            {
                ApplyMultiNamePreview(models, sanitize);
            }
            else if (ModeComboBox.SelectedIndex == 2)
            {
                ApplyFindReplacePreview(models, sanitize);
            }
            else if (ModeComboBox.SelectedIndex == 3)
            {
                templatePreviewService.ApplyTemplate(models, InstructionTextBox.Text, sanitize);
            }
            else
            {
                await ApplyNaturalLanguagePreviewAsync(models, sanitize);
            }

            validationService.Validate(models);
            RefreshItems();
            UpdatePreviewStatus(models);
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            var currentSettings = settingsService.Load();
            var apiKeyToSave = ApiKeyPasswordBox.Password;
            if (string.IsNullOrWhiteSpace(apiKeyToSave) && !currentSettings.IsApiKeyFromEnvironment)
            {
                apiKeyToSave = currentSettings.DeepSeekApiKey;
            }

            settingsService.Save(new AppSettings
            {
                DeepSeekModel = ModelTextBox.Text,
                DeepSeekApiKey = apiKeyToSave
            });

            ApiKeyPasswordBox.Password = string.Empty;
            LoadSettingsIntoUi();
            UpdateStatus("AI 设置已保存，API Key 已加密存储。");
        }

        private async void TestDeepSeek_Click(object sender, RoutedEventArgs e)
        {
            if (!aiNamingService.IsConfigured)
            {
                UpdateStatus("请先保存 DeepSeek API Key，或设置 DEEPSEEK_API_KEY 环境变量。");
                return;
            }

            UpdateStatus("正在测试 DeepSeek 连接...");
            try
            {
                var result = await aiNamingService.GenerateAsync(new AiNamingRequest
                {
                    Instruction = "命名为“连接测试”",
                    KeepExtension = true,
                    Files = new List<AiNamingFile>
                    {
                        new AiNamingFile
                        {
                            Index = 1,
                            Name = "sample.txt",
                            Extension = ".txt"
                        }
                    }
                });

                var first = result.Items.FirstOrDefault();
                if (first == null || string.IsNullOrWhiteSpace(first.NewBaseName))
                {
                    UpdateStatus("DeepSeek 连接成功，但返回内容为空。");
                    return;
                }

                UpdateStatus("DeepSeek 连接成功。示例名称：" + first.NewBaseName);
            }
            catch (Exception ex)
            {
                UpdateStatus(ex.Message);
            }
        }

        private void ClearApiKey_Click(object sender, RoutedEventArgs e)
        {
            settingsService.ClearDeepSeekApiKey();
            ApiKeyPasswordBox.Password = string.Empty;
            LoadSettingsIntoUi();
            UpdateStatus("已清除本机加密保存的 DeepSeek API Key。");
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            validationService.Validate(Items.Select(item => item.Model).ToList());
            RefreshItems();

            var readyItems = Items.Where(item => item.Model.Status == RenameStatus.Ready).ToList();
            if (readyItems.Count == 0)
            {
                UpdateStatus("没有可执行的重命名项，请先生成并检查预览。");
                return;
            }

            var blockedCount = Items.Count(item =>
                item.Model.Status == RenameStatus.Invalid ||
                item.Model.Status == RenameStatus.Conflict ||
                item.Model.Status == RenameStatus.Pending);

            if (blockedCount > 0)
            {
                UpdateStatus("仍有无效、冲突或未预览的文件，不能执行。");
                return;
            }

            var confirm = MessageBox.Show(
                this,
                string.Format("即将重命名 {0} 个文件。执行前请确认预览无误。", readyItems.Count),
                "确认重命名",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.OK)
            {
                return;
            }

            var log = executionService.Execute(Items.Select(item => item.Model).ToList());

            foreach (var item in Items.Where(item => item.Model.Status == RenameStatus.Success))
            {
                item.MarkAsCurrentFile();
            }

            RefreshItems();
            var successCount = log.Items.Count(item => item.Status == "success");
            var failedCount = log.Items.Count(item => item.Status == "failed");
            UpdateStatus(string.Format("重命名完成。成功：{0}，失败：{1}。", successCount, failedCount));
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                AddFiles(ExpandFiles(paths, RecursiveFolderCheckBox.IsChecked == true));
            }
        }

        private void AddFiles(IEnumerable<string> filePaths)
        {
            var existing = new HashSet<string>(
                Items.Select(item => item.Model.OriginalPath),
                StringComparer.OrdinalIgnoreCase);

            var added = 0;
            foreach (var filePath in filePaths.Where(File.Exists).OrderBy(path => path))
            {
                if (existing.Contains(filePath))
                {
                    continue;
                }

                var item = new RenameItem(filePath)
                {
                    Index = Items.Count + 1,
                    Status = RenameStatus.Pending,
                    Message = "待预览"
                };

                Items.Add(new RenameItemViewModel(item));
                existing.Add(filePath);
                added++;
            }

            ReindexItems();
            UpdateStatus(string.Format("已添加 {0} 个文件，当前共 {1} 个。", added, Items.Count));
        }

        private static IEnumerable<string> ExpandFiles(IEnumerable<string> paths, bool includeSubdirectories)
        {
            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    yield return path;
                }
                else if (Directory.Exists(path))
                {
                    var searchOption = includeSubdirectories
                        ? SearchOption.AllDirectories
                        : SearchOption.TopDirectoryOnly;

                    foreach (var filePath in EnumerateFilesSafe(path, searchOption))
                    {
                        yield return filePath;
                    }
                }
            }
        }

        private void MoveSelectedItems(int direction)
        {
            var selectedItems = FilesGrid.SelectedItems
                .OfType<RenameItemViewModel>()
                .ToList();

            if (selectedItems.Count == 0)
            {
                UpdateStatus("请先选择要移动的文件。");
                return;
            }

            var ordered = direction < 0
                ? selectedItems.OrderBy(item => Items.IndexOf(item)).ToList()
                : selectedItems.OrderByDescending(item => Items.IndexOf(item)).ToList();

            foreach (var item in ordered)
            {
                var oldIndex = Items.IndexOf(item);
                var newIndex = oldIndex + direction;
                if (newIndex < 0 || newIndex >= Items.Count)
                {
                    continue;
                }

                if (selectedItems.Contains(Items[newIndex]))
                {
                    continue;
                }

                Items.Move(oldIndex, newIndex);
            }

            ReindexItems();
            FilesGrid.SelectedItems.Clear();
            foreach (var item in selectedItems)
            {
                FilesGrid.SelectedItems.Add(item);
            }

            UpdateStatus("已调整文件顺序。");
        }

        private static IEnumerable<string> EnumerateFilesSafe(string rootPath, SearchOption searchOption)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                string[] files;
                try
                {
                    files = Directory.GetFiles(rootPath);
                }
                catch
                {
                    yield break;
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                yield break;
            }

            var pending = new Stack<string>();
            pending.Push(rootPath);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                string[] files;
                try
                {
                    files = Directory.GetFiles(current);
                }
                catch
                {
                    files = new string[0];
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                string[] directories;
                try
                {
                    directories = Directory.GetDirectories(current);
                }
                catch
                {
                    directories = new string[0];
                }

                foreach (var directory in directories)
                {
                    pending.Push(directory);
                }
            }
        }

        private void ApplyMultiNamePreview(IList<RenameItem> models, bool sanitize)
        {
            var names = multiNamePreviewService.SplitNames(InstructionTextBox.Text);
            multiNamePreviewService.ApplyNames(models, InstructionTextBox.Text, sanitize);

            if (names.Count > models.Count)
            {
                UpdateStatus(string.Format("名称数量比文件多 {0} 行，多余名称不会使用。", names.Count - models.Count));
            }
        }

        private void ApplyFindReplacePreview(IEnumerable<RenameItem> models, bool sanitize)
        {
            var findText = FindTextBox.Text ?? string.Empty;
            var replaceText = ReplaceTextBox.Text ?? string.Empty;

            foreach (var item in models)
            {
                if (string.IsNullOrEmpty(findText))
                {
                    item.ProposedBaseName = item.OriginalBaseName;
                    item.Status = RenameStatus.Invalid;
                    item.Message = "查找内容不能为空";
                    continue;
                }

                var newName = item.OriginalBaseName.Replace(findText, replaceText);
                item.ProposedBaseName = sanitize ? FileNameSanitizer.SanitizeBaseName(newName) : newName;
                item.Status = RenameStatus.Pending;
                item.Message = string.Empty;
            }
        }

        private async Task ApplyNaturalLanguagePreviewAsync(IList<RenameItem> models, bool sanitize)
        {
            if (!aiNamingService.IsConfigured)
            {
                naturalLanguagePreviewService.ApplyInstruction(models, InstructionTextBox.Text, sanitize);
                return;
            }

            UpdateStatus("正在调用 DeepSeek 生成文件名预览...");

            try
            {
                var result = await aiNamingService.GenerateAsync(new AiNamingRequest
                {
                    Instruction = InstructionTextBox.Text,
                    KeepExtension = true,
                    Files = models
                        .Select(item => new AiNamingFile
                        {
                            Index = item.Index,
                            Name = item.OriginalName,
                            Extension = item.Extension
                        })
                        .ToList()
                });

                var itemsByIndex = result.Items.ToDictionary(item => item.Index);
                foreach (var item in models)
                {
                    AiNamingItem namingItem;
                    if (!itemsByIndex.TryGetValue(item.Index, out namingItem))
                    {
                        item.Status = RenameStatus.Invalid;
                        item.Message = "AI 返回缺少该文件";
                        continue;
                    }

                    item.ProposedBaseName = sanitize
                        ? FileNameSanitizer.SanitizeBaseName(namingItem.NewBaseName)
                        : (namingItem.NewBaseName ?? string.Empty).Trim();
                    item.Status = RenameStatus.Pending;
                    item.Message = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(result.Warning))
                {
                    UpdateStatus("DeepSeek 已返回预览：" + result.Warning);
                }
            }
            catch (Exception ex)
            {
                foreach (var item in models)
                {
                    item.Status = RenameStatus.Invalid;
                    item.Message = "DeepSeek 生成失败";
                }

                UpdateStatus(ex.Message);
            }
        }

        private void LoadSettingsIntoUi()
        {
            var settings = settingsService.Load();
            ModelTextBox.Text = string.IsNullOrWhiteSpace(settings.DeepSeekModel)
                ? "deepseek-v4-flash"
                : settings.DeepSeekModel;

            ApiKeyPasswordBox.Password = string.Empty;
            if (settings.IsApiKeyFromEnvironment)
            {
                AiSettingsStatusTextBlock.Text = "已从环境变量 DEEPSEEK_API_KEY 读取 API Key";
            }
            else if (!string.IsNullOrWhiteSpace(settings.DeepSeekApiKey))
            {
                AiSettingsStatusTextBlock.Text = "已保存 API Key。输入新 Key 并保存可覆盖。";
            }
            else
            {
                AiSettingsStatusTextBlock.Text = "未配置 API Key 时使用本地规则";
            }
        }

        private void RefreshItems()
        {
            foreach (var item in Items)
            {
                item.RefreshAll();
            }
        }

        private void ReindexItems()
        {
            for (var i = 0; i < Items.Count; i++)
            {
                Items[i].Index = i + 1;
            }
        }

        private void UpdatePreviewStatus(IList<RenameItem> models)
        {
            var ready = models.Count(item => item.Status == RenameStatus.Ready);
            var unchanged = models.Count(item => item.Status == RenameStatus.Unchanged);
            var invalid = models.Count(item => item.Status == RenameStatus.Invalid);
            var conflict = models.Count(item => item.Status == RenameStatus.Conflict);

            UpdateStatus(string.Format(
                "预览完成。可执行：{0}，未变化：{1}，无效：{2}，冲突：{3}。",
                ready,
                unchanged,
                invalid,
                conflict));
        }

        private void UpdateStatus(string message)
        {
            MainWindowStatus.Text = message;
        }
    }
}
