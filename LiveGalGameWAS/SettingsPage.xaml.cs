using CommunityToolkit.WinUI.Helpers;
using LiveGalGameWAS.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Storage.Pickers;

namespace LiveGalGameWAS
{
    public sealed partial class SettingsPage : Page
    {
        private AppSettings _currentSettings;
        private AppSettings _originalSettings;

        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is AppSettings settings)
            {
                _currentSettings = settings;
                _originalSettings = new AppSettings
                {
                    SpeechRecognitionType = settings.SpeechRecognitionType,
                    BackgroundType = settings.BackgroundType,
                    VoskModelPath = settings.VoskModelPath,
                    SelectedCameraDeviceId = settings.SelectedCameraDeviceId,
                    SelectedMicrophoneDeviceId = settings.SelectedMicrophoneDeviceId
                };

                LoadSettings();
                await LoadAvailableDevicesAsync();
            }
        }

        private void LoadSettings()
        {
            // 加载语音识别设置
            foreach (ComboBoxItem item in SpeechRecognitionTypeComboBox.Items)
            {
                if (item.Tag?.ToString() == _currentSettings.SpeechRecognitionType.ToString())
                {
                    SpeechRecognitionTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载背景类型设置
            foreach (ComboBoxItem item in BackgroundTypeComboBox.Items)
            {
                if (item.Tag?.ToString() == _currentSettings.BackgroundType.ToString())
                {
                    BackgroundTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载VOSK模型路径
            VoskModelPathTextBox.Text = _currentSettings.VoskModelPath;
        }

        private async Task LoadAvailableDevicesAsync()
        {
            try
            {
                await LoadCameraDevicesAsync();
                await LoadMicrophoneDevicesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载设备列表失败: {ex.Message}");
            }
        }

        private async Task LoadCameraDevicesAsync()
        {
            try
            {
                var cameraDevices = await CameraHelper.GetFrameSourceGroupsAsync();

                CameraDeviceComboBox.Items.Clear();
                
                // 添加默认选项
                var defaultItem = new ComboBoxItem { Content = "默认相机", Tag = "" };
                CameraDeviceComboBox.Items.Add(defaultItem);
                
                foreach (var device in cameraDevices)
                {
                    var item = new ComboBoxItem { Content = device.DisplayName, Tag = device.Id };
                    CameraDeviceComboBox.Items.Add(item);
                    
                    // 如果设备ID匹配当前设置，则选中
                    if (device.Id == _currentSettings.SelectedCameraDeviceId)
                    {
                        CameraDeviceComboBox.SelectedItem = item;
                    }
                }
                
                // 如果没有选中任何设备，选择默认设备
                if (CameraDeviceComboBox.SelectedItem == null && CameraDeviceComboBox.Items.Count > 0)
                {
                    CameraDeviceComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载相机设备列表失败: {ex.Message}");
                CameraDeviceComboBox.Items.Clear();
                CameraDeviceComboBox.Items.Add(new ComboBoxItem { Content = "无法获取相机列表", Tag = "" });
                CameraDeviceComboBox.SelectedIndex = 0;
            }
        }

        private async Task LoadMicrophoneDevicesAsync()
        {
            try
            {
                var microphoneDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
                MicrophoneDeviceComboBox.Items.Clear();
                
                // 添加默认选项
                var defaultItem = new ComboBoxItem { Content = "默认麦克风", Tag = "" };
                MicrophoneDeviceComboBox.Items.Add(defaultItem);
                
                foreach (var device in microphoneDevices)
                {
                    var item = new ComboBoxItem { Content = device.Name, Tag = device.Id };
                    MicrophoneDeviceComboBox.Items.Add(item);
                    
                    // 如果设备ID匹配当前设置，则选中
                    if (device.Id == _currentSettings.SelectedMicrophoneDeviceId)
                    {
                        MicrophoneDeviceComboBox.SelectedItem = item;
                    }
                }
                
                // 如果没有选中任何设备，选择默认设备
                if (MicrophoneDeviceComboBox.SelectedItem == null && MicrophoneDeviceComboBox.Items.Count > 0)
                {
                    MicrophoneDeviceComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载麦克风设备列表失败: {ex.Message}");
                MicrophoneDeviceComboBox.Items.Clear();
                MicrophoneDeviceComboBox.Items.Add(new ComboBoxItem { Content = "无法获取麦克风列表", Tag = "" });
                MicrophoneDeviceComboBox.SelectedIndex = 0;
            }
        }

        private void SpeechRecognitionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpeechRecognitionTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedType = selectedItem.Tag?.ToString();
                VoskSettingsPanel.Visibility = selectedType == "Vosk" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void BackgroundTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BackgroundTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedType = selectedItem.Tag?.ToString();
                
                CameraSettingsPanel.Visibility = selectedType == "Camera" ? Visibility.Visible : Visibility.Collapsed;
                ApplicationSettingsPanel.Visibility = selectedType == "Application" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void RefreshCameraListButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadCameraDevicesAsync();
        }

        private async void RefreshMicrophoneListButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadMicrophoneDevicesAsync();
        }

        private void CameraDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CameraDeviceComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _currentSettings.SelectedCameraDeviceId = selectedItem.Tag?.ToString() ?? "";
            }
        }

        private void MicrophoneDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MicrophoneDeviceComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _currentSettings.SelectedMicrophoneDeviceId = selectedItem.Tag?.ToString() ?? "";
            }
        }

        private async void BrowseVoskModelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");

                // 初始化WinRT窗口
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Current);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

                var modelFolder = await folderPicker.PickSingleFolderAsync();

                if (modelFolder != null)
                {
                    VoskModelPathTextBox.Text = modelFolder.Path;
                }
            }
            catch (Exception ex)
            {
                // 显示错误信息
                var dialog = new ContentDialog
                {
                    Title = "错误",
                    Content = $"选择文件夹时发生错误: {ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 保存语音识别设置
            if (SpeechRecognitionTypeComboBox.SelectedItem is ComboBoxItem speechItem)
            {
                var speechType = speechItem.Tag?.ToString();
                if (Enum.TryParse<SpeechRecognitionType>(speechType, out var recognitionType))
                {
                    _currentSettings.SpeechRecognitionType = recognitionType;
                }
            }

            // 保存背景类型设置
            if (BackgroundTypeComboBox.SelectedItem is ComboBoxItem backgroundItem)
            {
                var backgroundType = backgroundItem.Tag?.ToString();
                if (Enum.TryParse<BackgroundType>(backgroundType, out var bgType))
                {
                    _currentSettings.BackgroundType = bgType;
                }
            }

            // 保存VOSK模型路径
            _currentSettings.VoskModelPath = VoskModelPathTextBox.Text;

            // 保存摄像机设备选择
            if (CameraDeviceComboBox.SelectedItem is ComboBoxItem cameraItem)
            {
                _currentSettings.SelectedCameraDeviceId = cameraItem.Tag?.ToString() ?? "";
            }

            // 保存麦克风设备选择
            if (MicrophoneDeviceComboBox.SelectedItem is ComboBoxItem microphoneItem)
            {
                _currentSettings.SelectedMicrophoneDeviceId = microphoneItem.Tag?.ToString() ?? "";
            }

            // 验证VOSK路径（如果选择了VOSK识别）
            if (_currentSettings.SpeechRecognitionType == SpeechRecognitionType.Vosk && 
                string.IsNullOrWhiteSpace(_currentSettings.VoskModelPath))
            {
                ShowErrorDialog("请选择VOSK模型文件夹路径");
                return;
            }

            if (_currentSettings.SpeechRecognitionType == SpeechRecognitionType.Vosk && 
                !Directory.Exists(_currentSettings.VoskModelPath))
            {
                ShowErrorDialog("选择的VOSK模型路径不存在");
                return;
            }

            // 导航回主页面并传递更新后的设置
            MainWindow.Current.Navigate(typeof(MainPage), _currentSettings);
        }

        private async void ShowErrorDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "设置错误",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 恢复原始设置
            _currentSettings.SpeechRecognitionType = _originalSettings.SpeechRecognitionType;
            _currentSettings.BackgroundType = _originalSettings.BackgroundType;
            _currentSettings.VoskModelPath = _originalSettings.VoskModelPath;

            // 导航回主页面并传递原始设置
            Frame.Navigate(typeof(MainPage), _originalSettings);
        }
    }
}