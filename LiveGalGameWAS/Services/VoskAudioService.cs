using LiveGalGameWAS.Models;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using Vosk;

namespace LiveGalGameWAS.Services;

public class VoskAudioService : IDisposable, IAsrService
{
    #region Fields

    private VoskRecognizer m_recognizer;
    private WaveInEvent m_waveIn;
    private MemoryStream m_audioBuffer;
    private bool m_isRecording;
    // 音频格式配置
    private const int SampleRate = 16000; // VOSK需要的16kHz采样率
    private const int Channels = 1;       // 单声道
    private const int BufferMs = 50;      // 缓冲区大小(毫秒)

    #endregion

    #region Constructors

    public VoskAudioService()
    {
    }

    #endregion

    #region Events

    public event Action<string> RecognitionResult;

    public event EventHandler<string>? OnTextResult;

    #endregion

    #region Private Methods


    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // 将音频数据写入缓冲区
        m_audioBuffer.Write(e.Buffer, 0, e.BytesRecorded);

        // 实时发送给VOSK识别器
        if (m_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
        {
            var result = m_recognizer.Result();
            RaiseRecognitionResult(result);
        }
        else
        {
            var partialResult = m_recognizer.PartialResult();
            if (!string.IsNullOrEmpty(partialResult))
            {
                RaiseRecognitionResult(partialResult);
            }
        }
    }

    private void OnRecordingStopped(object sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            Console.WriteLine($"录音停止，发生错误: {e.Exception.Message}");
        }

        // 获取最终结果
        var finalResult = m_recognizer.FinalResult();
        RaiseRecognitionResult(finalResult);

        Cleanup();
    }

    private void RaiseRecognitionResult(string result)
    {
        RecognitionResult?.Invoke(result);

        // 解析JSON结果
        try
        {
            var json = JObject.Parse(result);
            var text = json["text"]?.ToString();

            if (!string.IsNullOrEmpty(text))
            {
                OnTextResult?.Invoke(this, text);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"解析VOSK结果失败: {ex.Message}");
        }
    }

    private void Cleanup()
    {
        if (m_waveIn != null)
        {
            m_waveIn.DataAvailable -= OnDataAvailable;
            m_waveIn.RecordingStopped -= OnRecordingStopped;
            m_waveIn.Dispose();
            m_waveIn = null;
        }

        m_audioBuffer?.Dispose();
        m_audioBuffer = null;
        m_isRecording = false;
    }


    #endregion

    #region Public Methods

    public async Task InitializeAsync(object config)
    {
        if (config is VoskModelConfig modelConfig)
        {
            var model = new Model(modelConfig.ModelPath);
            m_recognizer = new VoskRecognizer(model, SampleRate);
            m_recognizer.SetMaxAlternatives(0);
            m_recognizer.SetWords(true);
        }
    }

    public void Dispose()
    {
        Cleanup();
        m_recognizer?.Dispose();
    }

    public Task Start()
    {
        StartListening();
        return Task.CompletedTask;
    }

    public Task Stop()
    {
        StopListening();
        return Task.CompletedTask;
    }

    public void StartListening()
    {
        if (m_isRecording) return;

        m_audioBuffer = new MemoryStream();
        m_waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(SampleRate, Channels),
            BufferMilliseconds = BufferMs
        };

        m_waveIn.DataAvailable += OnDataAvailable;
        m_waveIn.RecordingStopped += OnRecordingStopped;

        m_waveIn.StartRecording();
        m_isRecording = true;
    }

    public void StopListening()
    {
        if (!m_isRecording || m_waveIn == null) return;

        m_waveIn.StopRecording();
    }

    #endregion
}