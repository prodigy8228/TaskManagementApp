#if ANDROID
using Android.Content;
using Android.Provider;
using Android.Media;
using Microsoft.Maui.ApplicationModel;


namespace TaskManagement.Services
{
    public class AudioRecorderService
    {
      private MediaCodec _recorder;
    private string _filePath;

    public void StartRecording()
    {
   // MediaCodec recorder = MediaCodec.CreateEncoderByType("audio/mp4a-latm");
        _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "recorded_audio.mp3");
        _recorder = MediaCodec.CreateEncoderByType("audio/mp4a-latm");
        //MediaRecorder();
       _recorder.Start();
    }

    public void StopRecording()
    {
        _recorder.Stop();
        _recorder.Release();
    }

    }
}
#endif