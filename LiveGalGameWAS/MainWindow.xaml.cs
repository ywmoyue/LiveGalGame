using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Media.SpeechRecognition;
using Vosk;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info;

namespace LiveGalGameWAS
{
    /// <summary>
    /// GalGame主窗口，包含语音识别和对话系统
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private SpeechRecognizer? speechRecognizer;
        private VoskRecognizer? voskRecognizer;
        private bool isListening = false;
        private List<string> currentOptions = new List<string>();
        
        public MainWindow()
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;
            InitializeSpeechRecognition();
            StartGame();
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
            
            // 初始化VOSK识别器
            InitializeVoskRecognition();
        }
        
        private void InitializeVoskRecognition()
        {
            try
            {
                // 初始化Vosk识别器
                var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "VoskModels", "vosk-model-cn-0.22");
                var model = new Vosk.Model(modelPath);
                voskRecognizer = new VoskRecognizer(model, 16000.0f);
                
                UpdateSpeechStatus("VOSK识别器已初始化");
            }
            catch (System.Exception ex)
            {
                UpdateSpeechStatus($"VOSK初始化失败: {ex.Message}");
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
            if (button != null)
            {
                var optionText = button.Content.ToString();
                await ProcessOption(optionText);
            }
        }
        
        private async Task ProcessOption(string option)
        {
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
                await speechRecognizer.ContinuousRecognitionSession.StartAsync();
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
        
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            speechRecognizer?.Dispose();
            voskRecognizer?.Dispose();
        }
    }
}
