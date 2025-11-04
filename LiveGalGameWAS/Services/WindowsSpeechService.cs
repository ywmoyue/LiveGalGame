using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;

namespace LiveGalGameWAS.Services
{
    public class WindowsSpeechService : IDisposable, IAsrService
    {
        #region Fields

        private SpeechRecognizer? _speechRecognizer;
        private bool _isInitialized = false;
        private bool _isListening = false;

        #endregion

        #region Events

        public event EventHandler<string>? OnTextResult;

        #endregion

        #region Public Methods

        public async Task InitializeAsync(object config)
        {
            try
            {
                if (_isInitialized)
                {
                    return;
                }

                // 初始化Windows语音识别
                _speechRecognizer = new SpeechRecognizer();

                // 配置语法约束
                var grammar = new SpeechRecognitionListConstraint(new List<string> { "你好", "天气", "再见", "名字" });
                _speechRecognizer.Constraints.Add(grammar);

                var result = await _speechRecognizer.CompileConstraintsAsync();

                if (result.Status == SpeechRecognitionResultStatus.Success)
                {
                    _speechRecognizer.ContinuousRecognitionSession.ResultGenerated += SpeechRecognized;
                    _speechRecognizer.ContinuousRecognitionSession.AutoStopSilenceTimeout = TimeSpan.FromSeconds(3);
                    _isInitialized = true;
                }
                else
                {
                    throw new Exception("语音识别约束编译失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Windows语音识别初始化失败: {ex.Message}", ex);
            }
        }

        public async Task Start()
        {
            if (!_isInitialized || _speechRecognizer == null)
            {
                throw new InvalidOperationException("语音识别服务未初始化");
            }

            if (!_isListening)
            {
                await _speechRecognizer.ContinuousRecognitionSession.StartAsync();
                _isListening = true;
            }
        }

        public async Task Stop()
        {
            if (_isListening && _speechRecognizer != null)
            {
                await _speechRecognizer.ContinuousRecognitionSession.StopAsync();
                _isListening = false;
            }
        }

        public void Dispose()
        {
            if (_speechRecognizer != null)
            {
                if (_isListening)
                {
                    _speechRecognizer.ContinuousRecognitionSession.StopAsync().AsTask().Wait();
                }

                _speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= SpeechRecognized;
                _speechRecognizer.Dispose();
                _speechRecognizer = null;
            }

            _isInitialized = false;
            _isListening = false;
        }

        #endregion

        #region Private Methods

        private void SpeechRecognized(object sender, SpeechContinuousRecognitionResultGeneratedEventArgs e)
        {
            var recognizedText = e.Result.Text;
            OnTextResult?.Invoke(this, recognizedText);
        }

        #endregion
    }
}