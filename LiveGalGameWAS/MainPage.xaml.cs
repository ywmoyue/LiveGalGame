using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LiveGalGameWAS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SpeechRecognizer? speechRecognizer;
        private ScreenCaptureService? screenCaptureService;
        private MemoryStream audioBuffer;
        private bool isListening = false;
        private List<string> currentOptions = new List<string>();
        private BackgroundType currentBackgroundType = BackgroundType.StaticImage;
        private VoskAudioService _voskAudioService;

        public MainPage()
        {
            Loaded += MainPage_Loaded;
            InitializeComponent();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSpeechRecognition();
            InitializeVoskAudioService();
            InitializeBackgroundServices();
            StartGame();
        }


        //private void InitializeVoskAudioService()
        //{
        //    try
        //    {
        //        var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "VoskModels", "vosk-model-cn-0.22");
        //        _voskAudioService = new VoskAudioService(modelPath);
        //        _voskAudioService.RecognitionResult += OnVoskRecognitionResult;

        //        UpdateSpeechStatus("VOSK音频服务已初始化");
        //    }
        //    catch (Exception ex)
        //    {
        //        UpdateSpeechStatus($"VOSK音频服务初始化失败: {ex.Message}");
        //    }
        //}


        private async Task InitializeVoskAudioService()
        {
            try
            {
                // Show folder picker dialog to select model path
                var folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");

                // Initialize with the WinRT window
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Current);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

                var modelFolder = await folderPicker.PickSingleFolderAsync();

                if (modelFolder != null)
                {
                    _voskAudioService = new VoskAudioService(modelFolder.Path);
                    _voskAudioService.RecognitionResult += OnVoskRecognitionResult;

                    UpdateSpeechStatus($"VOSK音频服务已初始化，模型路径: {modelFolder.Path}");
                }
                else
                {
                    UpdateSpeechStatus("未选择VOSK模型路径，语音识别将不可用");
                }
            }
            catch (Exception ex)
            {
                UpdateSpeechStatus($"VOSK音频服务初始化失败: {ex.Message}");
            }
        }

        private void OnVoskRecognitionResult(string result)
        {
            // 解析JSON结果
            try
            {
                var json = JObject.Parse(result);
                var text = json["text"]?.ToString();

                if (!string.IsNullOrEmpty(text))
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        ShowDialog("你(VOSK)", text);
                        ProcessSpeechInput(text);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析VOSK结果失败: {ex.Message}");
            }
        }


        private async void InitializeSpeechRecognition()
        {
            try
            {
                // 初始化Windows语音识别
                speechRecognizer = new SpeechRecognizer();

                // 创建语法规则
                var grammar = new SpeechRecognitionListConstraint(new List<string> { "你好", "天气", "再见", "名字" });
                speechRecognizer.Constraints.Add(grammar);

                var result = await speechRecognizer.CompileConstraintsAsync();

                if (result.Status == SpeechRecognitionResultStatus.Success)
                {
                    speechRecognizer.ContinuousRecognitionSession.ResultGenerated += SpeechRecognized;
                    UpdateSpeechStatus("语音识别已初始化");
                }
                else
                {
                    UpdateSpeechStatus("语音识别编译失败");
                }
            }
            catch (System.Exception ex)
            {
                UpdateSpeechStatus($"语音识别初始化失败: {ex.Message}");
            }

        }

        private void StartGame()
        {
            // 开始游戏对话
            ShowDialog("系统", "欢迎来到LiveGalGame！请开始对话吧。");
            ShowOptions(new List<string> { "开始语音识别", "手动输入", "退出游戏" });
        }

        private void ShowDialog(string speaker, string message)
        {
            SpeakerText.Text = speaker;
            DialogText.Text = message;

            // 显示字幕
            SubtitleText.Text = message;
            SubtitleBorder.Visibility = Visibility.Visible;
        }

        private void ShowOptions(List<string> options)
        {
            currentOptions = options;
            OptionsPanel.Visibility = Visibility.Visible;

            // 更新选项按钮
            var buttons = new[] { Option1Button, Option2Button, Option3Button };

            for (int i = 0; i < buttons.Length; i++)
            {
                if (i < options.Count)
                {
                    buttons[i].Content = options[i];
                    buttons[i].Visibility = Visibility.Visible;
                }
                else
                {
                    buttons[i].Visibility = Visibility.Collapsed;
                }
            }
        }

        private void HideOptions()
        {
            OptionsPanel.Visibility = Visibility.Collapsed;
        }

        private async void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Content != null)
            {
                var optionText = button.Content.ToString();
                if (!string.IsNullOrEmpty(optionText))
                {
                    await ProcessOption(optionText);
                }
            }
        }

        private async Task ProcessOption(string? option)
        {
            if (string.IsNullOrEmpty(option))
                return;

            HideOptions();

            switch (option)
            {
                case "开始语音识别":
                    StartListening();
                    ShowDialog("系统", "语音识别已启动，请开始说话...");
                    break;
                case "手动输入":
                    ShowDialog("系统", "手动输入功能暂未实现");
                    ShowOptions(new List<string> { "开始语音识别", "退出游戏" });
                    break;
                case "退出游戏":
                    Application.Current.Exit();
                    break;
                default:
                    // 处理用户选择的对话选项
                    ShowDialog("你", option);
                    await Task.Delay(1000);
                    // 模拟NPC回复
                    ShowDialog("NPC", $"你选择了：{option}，这是一个很好的选择！");
                    ShowOptions(new List<string> { "继续对话", "结束对话" });
                    break;
            }
        }

        private async void StartListening()
        {
            if (!isListening)
            {
                //await speechRecognizer.ContinuousRecognitionSession.StartAsync();
                _voskAudioService.StartListening();
                isListening = true;
                UpdateSpeechStatus("正在监听...");
                SpeechIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Green);
            }
        }

        private async void StopListening()
        {
            if (isListening)
            {
                await speechRecognizer.ContinuousRecognitionSession.StopAsync();
                isListening = false;
                UpdateSpeechStatus("语音识别已停止");
                SpeechIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }

        private void SpeechDetected(object sender, object e)
        {
            UpdateSpeechStatus("检测到语音输入...");
            SpeechIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Yellow);
        }

        private void SpeechRecognized(object sender, SpeechContinuousRecognitionResultGeneratedEventArgs e)
        {
            var recognizedText = e.Result.Text;
            UpdateSpeechStatus($"识别到: {recognizedText}");

            // 在主线程中更新UI
            DispatcherQueue.TryEnqueue(() =>
            {
                ShowDialog("你", recognizedText);
                ProcessSpeechInput(recognizedText);
            });
        }

        private void SpeechRejected(object sender, object e)
        {
            UpdateSpeechStatus("语音识别失败");
            SpeechIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);
        }

        private async void ProcessSpeechInput(string input)
        {
            // 简单的关键词匹配逻辑
            if (input.Contains("你好") || input.Contains("hello"))
            {
                await Task.Delay(1000);
                ShowDialog("NPC", "你好！很高兴认识你！");
            }
            else if (input.Contains("天气") || input.Contains("weather"))
            {
                await Task.Delay(1000);
                ShowDialog("NPC", "今天的天气很不错呢！");
            }
            else if (input.Contains("再见") || input.Contains("拜拜") || input.Contains("goodbye"))
            {
                await Task.Delay(1000);
                ShowDialog("NPC", "再见！期待下次聊天！");
            }
            else if (input.Contains("名字") || input.Contains("name"))
            {
                await Task.Delay(1000);
                ShowDialog("NPC", "我是你的语音助手，很高兴为你服务！");
            }
            else
            {
                await Task.Delay(1000);
                ShowDialog("NPC", $"你说：{input}，我听到了！");
            }

            ShowOptions(new List<string> { "继续语音识别", "手动选择", "退出" });
        }

        private void UpdateSpeechStatus(string status)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                SpeechStatusText.Text = status;
            });
        }

        private async void InitializeBackgroundServices()
        {
            try
            {
                screenCaptureService = new ScreenCaptureService();
                // screenCaptureService.FrameCaptured += OnFrameCaptured;

                await LoadAvailableCamerasAsync();
                await LoadAvailableApplicationsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"背景服务初始化失败: {ex.Message}");
            }
        }

        private async Task LoadAvailableCamerasAsync()
        {
            try
            {
                // 这里可以添加获取可用相机的逻辑
                CameraDeviceComboBox.Items.Clear();
                CameraDeviceComboBox.Items.Add("默认相机");
                CameraDeviceComboBox.SelectedIndex = 0;

                await Task.CompletedTask; // 避免异步警告
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载相机列表失败: {ex.Message}");
            }
        }

        private async Task LoadAvailableApplicationsAsync()
        {
            try
            {
                if (screenCaptureService != null)
                {
                    var windows = await screenCaptureService.GetAvailableWindowsAsync();
                    ApplicationComboBox.Items.Clear();

                    foreach (var window in windows)
                    {
                        ApplicationComboBox.Items.Add(window);
                    }

                    if (ApplicationComboBox.Items.Count > 0)
                    {
                        ApplicationComboBox.SelectedIndex = 0;
                    }
                }
                else
                {
                    // 如果screenCaptureService为null，添加一些示例应用程序
                    ApplicationComboBox.Items.Clear();
                    ApplicationComboBox.Items.Add("示例应用程序");
                    ApplicationComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载应用程序列表失败: {ex.Message}");
            }
        }

        private void OnFrameCaptured(SoftwareBitmapSource frame)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (VideoBackground != null)
                {
                    VideoBackground.Source = frame;
                    VideoBackground.Visibility = Visibility.Visible;
                }
            });
        }

        private void BackgroundSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundSettingsPanel.Visibility = BackgroundSettingsPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void BackgroundTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CameraDeviceComboBox == null) return;
            if (BackgroundTypeComboBox.SelectedItem is ComboBoxItem item)
            {
                var selectedType = item.Content.ToString();

                CameraDeviceComboBox.Visibility = selectedType == "相机视频流" ? Visibility.Visible : Visibility.Collapsed;
                ApplicationComboBox.Visibility = selectedType == "应用程序画面" ? Visibility.Visible : Visibility.Collapsed;

                currentBackgroundType = selectedType switch
                {
                    "静态图片" => BackgroundType.StaticImage,
                    "相机视频流" => BackgroundType.Camera,
                    "应用程序画面" => BackgroundType.Application,
                    _ => BackgroundType.StaticImage
                };
            }
        }

        private async void StartCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (currentBackgroundType)
                {
                    case BackgroundType.Camera:
                        CameraPreviewControl.Visibility = Visibility.Visible;
                        await CameraPreviewControl.StartAsync();
                        break;
                    case BackgroundType.Application:
                        if (screenCaptureService != null && !screenCaptureService.IsCapturing)
                        {
                            var selectedApp = ApplicationComboBox.SelectedItem?.ToString();
                            await screenCaptureService.StartCaptureAsync(selectedApp ?? string.Empty);
                            StartCaptureButton.Visibility = Visibility.Collapsed;
                            StopCaptureButton.Visibility = Visibility.Visible;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"开始捕获失败: {ex.Message}");
            }
        }

        private async void StopCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (currentBackgroundType)
                {
                    case BackgroundType.Camera:
                        CameraPreviewControl.Visibility = Visibility.Visible;
                        break;
                    case BackgroundType.Application:
                        if (screenCaptureService != null && screenCaptureService.IsCapturing)
                        {
                            await screenCaptureService.StopCaptureAsync();

                            CameraPreviewControl.Visibility = Visibility.Collapsed;
                            StartCaptureButton.Visibility = Visibility.Visible;
                            StopCaptureButton.Visibility = Visibility.Collapsed;

                            // 恢复静态背景
                            VideoBackground.Visibility = Visibility.Collapsed;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"停止捕获失败: {ex.Message}");
            }
        }

        public void Close()
        {

            speechRecognizer?.Dispose();
            _voskAudioService?.Dispose();
            screenCaptureService?.Dispose();
        }
    }
}
