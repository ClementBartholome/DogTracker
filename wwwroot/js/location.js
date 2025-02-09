let watchId = null;
let dotNetHelper = null;
let simulationInterval = null;

function getRandomPosition(baseLat, baseLon, maxOffset) {
    const offsetLat = (Math.random() - 0.5) * maxOffset;
    const offsetLon = (Math.random() - 0.5) * maxOffset;
    return {
        latitude: baseLat + offsetLat,
        longitude: baseLon + offsetLon,
        accuracy: Math.random() * 10,
        timestamp: new Date()
    };
}

window.getCurrentPosition = () => {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject('Geolocation is not supported');
            return;
        }

        navigator.geolocation.getCurrentPosition(
            position => {
                resolve({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude,
                    accuracy: position.coords.accuracy,
                    timestamp: new Date()
                });
            },
            error => reject(error),
            { enableHighAccuracy: true }
        );
    });
};

window.startWatchingPosition = (helper, simulate) => {
    dotNetHelper = helper;

    let walkData = JSON.parse(localStorage.getItem('currentWalk')) || {
        startTime: new Date(),
        positions: [],
        lastSync: new Date()
    };

    localStorage.setItem('currentWalk', JSON.stringify(walkData));

    const syncIfNeeded = (currentWalk) => {
        const now = new Date();
        const timeSinceLastSync = now - new Date(currentWalk.lastSync);
        const SYNC_INTERVAL = 1000 * 60; // Sync toutes les minutes

        if (timeSinceLastSync >= SYNC_INTERVAL && dotNetHelper) {
            // Envoyer seulement les nouvelles positions depuis lastSync
            const newPositions = currentWalk.positions.filter(p =>
                new Date(p.timestamp) > new Date(currentWalk.lastSync)
            );

            if (newPositions.length > 0) {
                dotNetHelper.invokeMethodAsync('SyncPositions', newPositions)
                    .then(() => {
                        currentWalk.lastSync = now;
                        localStorage.setItem('currentWalk', JSON.stringify(currentWalk));
                    })
                    .catch(err => console.error('Erreur de sync:', err));
            }
        }
    };

    if (simulate) {
        const baseLat = 46.1733888
        const baseLon = -1.1337728;
        const maxOffset = 0.001; // Maximum offset for simulation
        
        simulationInterval = setInterval(() => {
            const simulatedPosition = getRandomPosition(baseLat, baseLon, maxOffset);
            const currentWalk = JSON.parse(localStorage.getItem('currentWalk'));
            currentWalk.positions.push(simulatedPosition);
            localStorage.setItem('currentWalk', JSON.stringify(currentWalk));

            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnLocationUpdate', simulatedPosition);
            }

            syncIfNeeded(currentWalk);
        }, 3000);
    } else {
        if (!navigator.geolocation) {
            console.error('Geolocation is not supported');
            return;
        }
        
        watchId = navigator.geolocation.watchPosition(
            position => {
                const locationData = {
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude,
                    accuracy: position.coords.accuracy,
                    timestamp: new Date()
                };

                const currentWalk = JSON.parse(localStorage.getItem('currentWalk'));
                currentWalk.positions.push(locationData);
                localStorage.setItem('currentWalk', JSON.stringify(currentWalk));

                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnLocationUpdate', locationData);
                }

                syncIfNeeded(currentWalk);
            },
            error => console.error(error),
            {
                enableHighAccuracy: true,
                timeout: 5000,
                maximumAge: 0
            }
        );
    }
};

window.getStoredWalkData = () => {
    const walkData = localStorage.getItem('currentWalk');
    return walkData ? JSON.parse(walkData) : null;
};

window.stopWatchingPosition = () => {
    const walkData = localStorage.getItem('currentWalk');
    localStorage.removeItem('currentWalk');

    if (simulationInterval !== null) {
        clearInterval(simulationInterval);
        simulationInterval = null;
    }
    if (watchId !== null) {
        navigator.geolocation.clearWatch(watchId);
        watchId = null;
    }

    return walkData;
};

window.checkForOngoingWalk = () => {
    const walkData = localStorage.getItem('currentWalk');
    return walkData ? JSON.parse(walkData) : null;
};