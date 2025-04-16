// Audio handling functions
window.chatInterop = {
    playAudio: (audioFile) => {
        const audio = new Audio(audioFile);
        audio.play().catch(e => console.error("Audio playback failed:", e));
    },

    pauseAudio: () => {
        const audios = document.getElementsByTagName('audio');
        for (let audio of audios) {
            audio.pause();
        }
    },

    clearAudioQueue: () => {
        // Implementation depends on your audio queue system
        console.log('Clearing audio queue');
    },

    // Audio recording
    startRecording: async () => {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            const mediaRecorder = new MediaRecorder(stream);
            const audioChunks = [];
            
            mediaRecorder.ondataavailable = (event) => {
                audioChunks.push(event.data);
            };
            
            mediaRecorder.onstop = async () => {
                const audioBlob = new Blob(audioChunks, { type: 'audio/wav' });
                const arrayBuffer = await audioBlob.arrayBuffer();
                DotNet.invokeMethodAsync('NetworkMonitorBlazor', 'ReceiveAudioBlob', arrayBuffer);
            };
            
            mediaRecorder.start();
            return {
                mediaRecorder: mediaRecorder,
                stream: stream
            };
        } catch (error) {
            console.error('Error starting recording:', error);
            throw error;
        }
    },

    stopRecording: async (recorderObj) => {
        if (recorderObj && recorderObj.mediaRecorder) {
            recorderObj.mediaRecorder.stop();
            if (recorderObj.stream) {
                recorderObj.stream.getTracks().forEach(track => track.stop());
            }
        }
    },

    // File download
    downloadFile: (filename, text) => {
        const element = document.createElement('a');
        element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(text));
        element.setAttribute('download', filename);
        element.style.display = 'none';
        document.body.appendChild(element);
        element.click();
        document.body.removeChild(element);
    },
    scrollToBottom: function(element) {
        element.scrollTop = element.scrollHeight;
    }
};
