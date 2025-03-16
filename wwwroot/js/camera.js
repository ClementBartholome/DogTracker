let dotNetHelper = null; // Référence à l'objet .NET pour les appels interopérables

export async function initializeCamera(dotnetHelper) {
    try {
        dotNetHelper = dotnetHelper;
        const stream = await navigator.mediaDevices.getUserMedia({
            video: {
                facingMode: 'environment',
                width: { ideal: 1920 },
                height: { ideal: 1080 }
            }
        });
        const video = document.getElementById('camera-preview');
        video.srcObject = stream;

        await checkFlashAvailability();

        return true;
    } catch (err) {
        console.error("Erreur d'accès à la caméra:", err);
        return false;
    }
}


export async function takePhoto() {
    try {
        const video = document.getElementById('camera-preview');
        if (!video || video.readyState !== video.HAVE_ENOUGH_DATA) {
            console.error('Video not ready');
            return null;
        }

        const canvas = document.createElement('canvas');
        // Utiliser la résolution native au lieu de réduire
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;

        const context = canvas.getContext('2d');
        context.drawImage(video, 0, 0, canvas.width, canvas.height);

        // Améliorer le contraste pour les documents
        enhanceImageForDocument(context, canvas.width, canvas.height);

        // Augmenter la qualité de l'image à 0.9
        const data = canvas.toDataURL('image/jpeg', 0.9);

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

// Fonction pour améliorer les images de documents
function enhanceImageForDocument(ctx, width, height) {
    try {
        // Récupérer les données de l'image
        const imageData = ctx.getImageData(0, 0, width, height);
        const data = imageData.data;

        // Améliorer le contraste
        const contrast = 1.2; // Valeur > 1 augmente le contraste
        const factor = (259 * (contrast + 255)) / (255 * (259 - contrast));

        for (let i = 0; i < data.length; i += 4) {
            // Rouge
            data[i] = truncateColor(factor * (data[i] - 128) + 128);
            // Vert
            data[i + 1] = truncateColor(factor * (data[i + 1] - 128) + 128);
            // Bleu
            data[i + 2] = truncateColor(factor * (data[i + 2] - 128) + 128);
        }

        // Mettre à jour l'image
        ctx.putImageData(imageData, 0, 0);
    } catch (error) {
        console.error('Error enhancing image:', error);
    }
}

// Fonction utilitaire pour s'assurer que les valeurs de couleur restent dans la plage 0-255
function truncateColor(value) {
    if (value < 0) return 0;
    if (value > 255) return 255;
    return Math.round(value);
}

// Ajouter cette fonction
export async function checkFlashAvailability() {
    try {
        const devices = await navigator.mediaDevices.enumerateDevices();
        const cameras = devices.filter(device => device.kind === 'videoinput');
        if (cameras.length > 0) {
            // La vérification du flash est compliquée et varie selon les navigateurs
            // Pour le moment, on suppose que les appareils mobiles ont un flash
            const isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('SetFlashAvailability', isMobile);
            }
            return isMobile;
        }
        return false;
    } catch (error) {
        console.error('Error checking flash:', error);
        return false;
    }
}

export async function toggleFlash(enable) {
    try {
        const track = document.getElementById('camera-preview')?.srcObject?.getVideoTracks()[0];
        if (track && track.getCapabilities && track.getCapabilities().torch) {
            await track.applyConstraints({
                advanced: [{ torch: enable }]
            });
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error toggling flash:', error);
        return false;
    }
}

export function stopCamera() {
    const video = document.getElementById('camera-preview');
    if (video && video.srcObject) {
        video.srcObject.getTracks().forEach(track => track.stop());
        video.srcObject = null;
    }
}

window.toggleFlash = toggleFlash;
window.initializeCamera = initializeCamera;
window.takePhoto = takePhoto;
window.stopCamera = stopCamera;