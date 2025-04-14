using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace NetworkMonitorBlazor.Services
{
    public class AudioService
    {
        private readonly IJSRuntime _jsRuntime;

        public AudioService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task PlayAudioSequentially(string audioFile)
        {
            await _jsRuntime.InvokeVoidAsync("playAudio", audioFile);
        }

        public async Task PauseAudio()
        {
            await _jsRuntime.InvokeVoidAsync("pauseAudio");
        }

        public async Task ClearQueue()
        {
            await _jsRuntime.InvokeVoidAsync("clearAudioQueue");
        }

        public async Task<string> TranscribeAudio(byte[] audioBlob)
        {
            return await _jsRuntime.InvokeAsync<string>("transcribeAudio", audioBlob);
        }
    }
}