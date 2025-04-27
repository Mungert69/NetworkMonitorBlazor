class AudioRecorder {
    constructor(dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        this.mediaRecorder = null;
        this.audioChunks = [];
        this.audioContext = null;
    }

    async startRecording() {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            this.mediaRecorder = new MediaRecorder(stream, { mimeType: 'audio/webm' });
            
            this.mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    this.audioChunks.push(event.data);
                }
            };

            this.mediaRecorder.onstop = async () => {
                const audioBlob = new Blob(this.audioChunks, { type: 'audio/webm; codecs=opus' });
                const wavBlob = await this.convertToWav(audioBlob);
                await this.dotNetHelper.invokeMethodAsync('ProcessAudioBlob', wavBlob);
                
                // Clean up
                stream.getTracks().forEach(track => track.stop());
                this.audioChunks = [];
            };

            this.mediaRecorder.start(100); // Collect data every 100ms
            return true;
        } catch (error) {
            console.error('Recording error:', error);
            await this.dotNetHelper.invokeMethodAsync('OnRecordingError', error.message);
            return false;
        }
    }

    stopRecording() {
        if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
            this.mediaRecorder.stop();
        }
    }

    async convertToWav(audioBlob) {
        const arrayBuffer = await audioBlob.arrayBuffer();
        this.audioContext = this.audioContext || new (window.AudioContext || window.webkitAudioContext)();
        const audioBuffer = await this.audioContext.decodeAudioData(arrayBuffer);
        
        // Normalize audio (similar to your React implementation)
        const normalizedBuffer = this.normalizeAudio(audioBuffer);
        
        // Convert to WAV
        const wavBuffer = this.toWav(normalizedBuffer);
        return new Blob([wavBuffer], { type: 'audio/wav' });
    }

    normalizeAudio(audioBuffer, targetRMS = 0.1) {
        const rawData = audioBuffer.getChannelData(0);
        const rms = Math.sqrt(rawData.reduce((sum, sample) => sum + sample ** 2, 0) / rawData.length);
        const scale = targetRMS / rms;
        
        const normalizedData = rawData.map(sample => Math.max(-1, Math.min(1, sample * scale)));
        
        const normalizedBuffer = new AudioContext().createBuffer(
            1, 
            normalizedData.length,
            audioBuffer.sampleRate
        );
        normalizedBuffer.copyToChannel(normalizedData, 0);
        return normalizedBuffer;
    }

    toWav(audioBuffer) {
        // Implementation of toWav similar to audiobuffer-to-wav library
        // (Would include the actual conversion code here)
    }
}

// Export functions for Blazor to use
window.AudioRecorderInterop = {
    init: (dotNetHelper) => {
        return new AudioRecorder(dotNetHelper);
    }
};