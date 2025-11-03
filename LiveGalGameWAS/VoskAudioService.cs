using NAudio.Wave;
using System;
using System.IO;
using Vosk;

public class VoskAudioService : IDisposable
{
    private readonly VoskRecognizer _recognizer;
    private WaveInEvent _waveIn;
    private MemoryStream _audioBuffer;
    private bool _isRecording;

    // 音频格式配置
    private const int SampleRate = 16000; // VOSK需要的16kHz采样率
    private const int Channels = 1;       // 单声道
    private const int BufferMs = 50;      // 缓冲区大小(毫秒)

    public event Action<string> RecognitionResult;

    public VoskAudioService(string modelPath)
    {
        var model = new Model(modelPath);
        _recognizer = new VoskRecognizer(model, SampleRate);
        _recognizer.SetMaxAlternatives(0);
        _recognizer.SetWords(true);
    }

    public void StartListening()
    {
        if (_isRecording) return;

        _audioBuffer = new MemoryStream();
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(SampleRate, Channels),
            BufferMilliseconds = BufferMs
        };

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _waveIn.StartRecording();
        _isRecording = true;
    }

    public void StopListening()
    {
        if (!_isRecording || _waveIn == null) return;

        _waveIn.StopRecording();
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // 将音频数据写入缓冲区
        _audioBuffer.Write(e.Buffer, 0, e.BytesRecorded);

        // 实时发送给VOSK识别器
        if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
        {
            var result = _recognizer.Result();
            RaiseRecognitionResult(result);
        }
        else
        {
            var partialResult = _recognizer.PartialResult();
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
        var finalResult = _recognizer.FinalResult();
        RaiseRecognitionResult(finalResult);

        Cleanup();
    }

    private void RaiseRecognitionResult(string result)
    {
        RecognitionResult?.Invoke(result);
    }

    private void Cleanup()
    {
        if (_waveIn != null)
        {
            _waveIn.DataAvailable -= OnDataAvailable;
            _waveIn.RecordingStopped -= OnRecordingStopped;
            _waveIn.Dispose();
            _waveIn = null;
        }

        _audioBuffer?.Dispose();
        _audioBuffer = null;
        _isRecording = false;
    }

    public void Dispose()
    {
        Cleanup();
        _recognizer?.Dispose();
    }
}
