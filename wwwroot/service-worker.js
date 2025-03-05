// wwwroot/service-worker.js
self.addEventListener('install', event => {
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(self.clients.claim());
});

// Store walk data
let walkData = {
    startTime: null,
    isTracking: false,
    duration: 0,
    distance: 0
};

// Background sync for walk updates
self.addEventListener('sync', event => {
    if (event.tag === 'walk-update') {
        event.waitUntil(updateWalkInBackground());
    }
});

// Handle messages from the app
self.addEventListener('message', event => {
    if (event.data.action === 'startWalk') {
        walkData = {
            startTime: Date.now(),
            isTracking: true,
            duration: 0,
            distance: event.data.distance || 0
        };

        // Set up periodic updates
        setInterval(() => {
            if (walkData.isTracking) {
                walkData.duration = Math.floor((Date.now() - walkData.startTime) / 1000);
                updateWalkNotification();
            }
        }, 1000);
    }
    else if (event.data.action === 'updateWalk') {
        walkData.distance = event.data.distance;
    }
    else if (event.data.action === 'stopWalk') {
        walkData.isTracking = false;
    }
});

function updateWalkInBackground() {
    if (!walkData.isTracking) return Promise.resolve();
    return updateWalkNotification();
}

function updateWalkNotification() {
    if (!walkData.isTracking) return Promise.resolve();

    const hours = Math.floor(walkData.duration / 3600);
    const minutes = Math.floor((walkData.duration % 3600) / 60);
    const seconds = walkData.duration % 60;
    const formattedDuration = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;

    return self.registration.showNotification('Promenade en cours', {
        body: `Durée: ${formattedDuration} - Distance: ${walkData.distance.toFixed(2)} km`,
        icon: '/images/app-icon.png',
        tag: 'walk-tracker',
        renotify: true,
        silent: true,
        requireInteraction: true
    });
}