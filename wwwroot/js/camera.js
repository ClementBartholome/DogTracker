let dotNetHelper = null; // Référence à l'objet .NET pour les appels interopérables

export async function initializeCamera(dotnetHelper) {
    try {
        dotNetHelper = dotnetHelper; 
        const stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: 'environment' }
        });
        const video = document.getElementById('camera-preview');
        video.srcObject = stream;
        return true;
    } catch (err) {
        console.error("Erreur d'accès à la caméra:", err);
        return false;
    }
}

export async function takePhoto() {
    try {
        const video = document.getElementById('camera-preview');
        if (!video || !video.readyState === video.HAVE_ENOUGH_DATA) {
            console.error('Video not ready');
            return null;
        }

        const canvas = document.createElement('canvas');
        const scale = 0.8; 
        canvas.width = video.videoWidth * scale;
        canvas.height = video.videoHeight * scale;

        const context = canvas.getContext('2d');
        context.drawImage(video, 0, 0, canvas.width, canvas.height);

        const data = canvas.toDataURL('image/jpeg', 0.6);

        if (dotNetHelper) {
            setTimeout(() => {
                dotNetHelper.invokeMethodAsync('OnPhotoTaken', data);
            }, 0);
        }

        return data;
    } catch (error) {
        console.error('Error taking photo:', error);
        return null;
    }
}

export function stopCamera() {
    const video = document.getElementById('camera-preview');
    if (video && video.srcObject) {
        video.srcObject.getTracks().forEach(track => track.stop());
        video.srcObject = null;
    }
}

window.initializeCamera = initializeCamera;
window.takePhoto = takePhoto;
window.stopCamera = stopCamera;