namespace LiveGalGameWAS.Models
{
    public enum SpeechRecognitionType
    {
        WindowsSpeech,
        Vosk
    }

    public class AppSettings
    {
        public SpeechRecognitionType SpeechRecognitionType { get; set; } = SpeechRecognitionType.WindowsSpeech;
        public BackgroundType BackgroundType { get; set; } = BackgroundType.StaticImage;
        public string VoskModelPath { get; set; } = string.Empty;
        public string SelectedCameraDeviceId { get; set; } = string.Empty;
        public string SelectedMicrophoneDeviceId { get; set; } = string.Empty;
    }
}