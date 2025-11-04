using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Vosk;
using Windows.Media;
using Windows.Media.SpeechRecognition;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info;

namespace LiveGalGameWAS
{
    /// <summary>
    /// GalGame主窗口，包含语音识别和对话系统
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public static MainWindow Current;

        public Frame Frame => MainFrame;

        public MainWindow()
        {
            Current = this;
            InitializeComponent();
            this.Closed += MainWindow_Closed;
            Frame.Navigate(typeof(MainPage));
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (Frame.Content is MainPage mainPage)
            {
                mainPage.Close();
            }
        }

        public async Task Navigate(Type page,object args)
        {
            if (Frame.Content is MainPage mainPage)
            {
                mainPage.Close();
            }

            Frame.Navigate(page, args);
        }
    }
}
