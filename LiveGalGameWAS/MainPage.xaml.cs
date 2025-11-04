using CommunityToolkit.WinUI.Helpers;
using LiveGalGameWAS.Models;
using LiveGalGameWAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.SpeechRecognition;
using Windows.Storage.Pickers;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LiveGalGameWAS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IAsrService? _currentAsrService;
        private AppSettings _appSettings = new AppSettings();
        private List<string> currentOptions = new List<string>();
        private VoskAudioService _voskAudioService;
        private WindowsSpeechService _windowsSpeechService;
        private bool _isCameraInitialized = false;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await InitializeSpeechRecognitionServices();
            StartGame();

            // 如果从设置页面返回，应用新的设置
            if (e.Parameter is AppSettings settings)
            {
                _appSettings = settings;
                SwitchSpeechRecognitionService(settings.SpeechRecognitionType);
                
                // 应用背景设置和设备选择
                ApplyBackgroundSettings(settings);
            }
        }

        private async void ApplyBackgroundSettings(AppSettings settings)
        {
            System.Diagnostics.Debug.WriteLine($"应用背景设置: 类型={settings.BackgroundType}, 相机={settings.SelectedCameraDeviceId}, 麦克风={settings.SelectedMicrophoneDeviceId}");
            
            // 根据背景类型应用设置
            switch (settings.BackgroundType)
            {
                case BackgroundType.Camera:
                    await StartCameraPreview(settings.SelectedCameraDeviceId);
                    break;
                case BackgroundType.StaticImage:
                case BackgroundType.Application:
                default:
                    await StopCameraPreview();
                    break;
            }
        }

        private async Task StartCameraPreview(string cameraDeviceId)
        {
            try
            {
                // 如果相机已经在运行，先停止
                await StopCameraPreview();
                
                // 显示相机预览控件并开始预览
                CameraPreviewControl.Visibility = Visibility.Visible;

                // 如果指定了相机设备ID，使用指定的设备
                if (!string.IsNullOrEmpty(cameraDeviceId))
                {
                    var cameraDevices = await CameraHelper.GetFrameSourceGroupsAsync();
                    var device = cameraDevices.FirstOrDefault(x => x.Id == cameraDeviceId);
                    if (device != null)
                    {
                        CameraHelper cameraHelper = new CameraHelper() { FrameSourceGroup = device };
                        await CameraPreviewControl.StartAsync(cameraHelper);
                    }
                }
                else
                {
                    await CameraPreviewControl.StartAsync();
                }

                _isCameraInitialized = true;
                UpdateSpeechStatus("相机预览已启动");
                System.Diagnostics.Debug.WriteLine("相机预览已启动");
            }
            catch (Exception ex)
            {
                UpdateSpeechStatus($"启动相机预览失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"启动相机预览失败: {ex.Message}");
                
                // 如果初始化失败，确保预览控件隐藏
                CameraPreviewControl.Visibility = Visibility.Collapsed;
            }
        }

        private async Task StopCameraPreview()
        {
            if (_isCameraInitialized)
            {
                try
                {
                    // 停止预览并隐藏控件
                    CameraPreviewControl.Visibility = Visibility.Collapsed;
                    CameraPreviewControl.Stop();

                    _isCameraInitialized = false;
                    UpdateSpeechStatus("相机预览已停止");
                    System.Diagnostics.Debug.WriteLine("相机预览已停止");
                }
                catch (Exception ex)
                {
                    UpdateSpeechStatus($"停止相机预览失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"停止相机预览失败: {ex.Message}");
                }
            }
            else
            {
                // 确保预览控件隐藏
                CameraPreviewControl.Visibility = Visibility.Collapsed;
            }
        }

        private async Task InitializeSpeechRecognitionServices()
        {
            try
            {
                // 初始化Windows语音识别服务
                _windowsSpeechService = new WindowsSpeechService();
                await _windowsSpeechService.InitializeAsync(null);
                _windowsSpeechService.OnTextResult += AsrService_OnTextResult;

                // 初始化VOSK语音识别服务
                _voskAudioService = new VoskAudioService();
                _voskAudioService.OnTextResult += AsrService_OnTextResult;

                // 设置默认语音识别服务
                _currentAsrService = null;
                UpdateSpeechStatus("未选择语音识别引擎");
            }
            catch (Exception ex)
            {
                UpdateSpeechStatus($"语音识别服务初始化失败: {ex.Message}");
            }
        }

        private void AsrService_OnTextResult(object? sender, string text)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ShowDialog("你", text);
                ProcessSpeechInput(text);
            });
        }

        private void SwitchSpeechRecognitionService(SpeechRecognitionType type)
        {
            try
            {
                // 停止当前服务
                StopListening();

                // 切换服务
                _currentAsrService = type switch
                {
                    SpeechRecognitionType.WindowsSpeech => _windowsSpeechService,
                    SpeechRecognitionType.Vosk => _voskAudioService,
                    _ => _windowsSpeechService
                };

                // 如果切换到VOSK且需要模型路径，提示用户
                if (type == SpeechRecognitionType.Vosk && string.IsNullOrEmpty(_appSettings.VoskModelPath))
                {
                    UpdateSpeechStatus("请先设置VOSK模型路径");
                }
                else
                {
                    UpdateSpeechStatus($"已切换到{type}语音识别");
                }
            }
            catch (Exception ex)
            {
                UpdateSpeechStatus($"切换语音识别服务失败: {ex.Message}");
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
            try
            {
                await _currentAsrService.Start();
                UpdateSpeechStatus("正在监听...");
                SpeechIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Green);
            }
            catch (Exception ex)
            {
                UpdateSpeechStatus($"启动监听失败: {ex.Message}");
                SpeechIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }

        private async void StopListening()
        {
            try
            {
                await _currentAsrService.Stop();
                UpdateSpeechStatus("语音识别已停止");
                SpeechIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            catch (Exception ex)
            {
                UpdateSpeechStatus($"停止监听失败: {ex.Message}");
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 导航到设置页面
            MainWindow.Current.Navigate(typeof(SettingsPage), _appSettings);
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

        public void Close()
        {
            _voskAudioService?.Dispose();
            _windowsSpeechService?.Dispose();
        }
    }
}
