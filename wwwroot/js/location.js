let watchId = null; // Identifiant pour la surveillance de la position
let dotNetHelper = null; // Référence à l'objet .NET pour les appels interopérables
const POSITION_INTERVAL = 30000; // Interval de temps pour enregistrer la position

// Variables pour la simulation de position
let simulationInterval = null;


function initializeWalkData() {
    let walkData = JSON.parse(localStorage.getItem('currentWalk')) || {
        startTime: new Date(),
        positions: [],
        lastSync: new Date(),
        lastRecordedTime: Date.now() 
    };
    localStorage.setItem('currentWalk', JSON.stringify(walkData));
    return walkData;
}

// Synchronise les nouvelles positions avec le backend toutes les minutes
function syncIfNeeded(currentWalk) {
    const now = new Date();
    const timeSinceLastSync = now - new Date(currentWalk.lastSync);
    const SYNC_INTERVAL = 1000 * 60; 

    if (timeSinceLastSync >= SYNC_INTERVAL && dotNetHelper) {
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
}

function startSimulation(baseLat, baseLon, maxOffset) {
    simulationInterval = setInterval(() => {
        const currentTime = Date.now();
        const currentWalk = JSON.parse(localStorage.getItem('currentWalk'));

        if (currentTime - currentWalk.lastRecordedTime >= POSITION_INTERVAL) {
            const simulatedPosition = getRandomPosition(baseLat, baseLon, maxOffset);
            currentWalk.positions.push(simulatedPosition);
            currentWalk.lastRecordedTime = currentTime;
            localStorage.setItem('currentWalk', JSON.stringify(currentWalk));

            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnLocationUpdate', simulatedPosition);
            }

            syncIfNeeded(currentWalk);
        }
    }, 5000); // On garde un intervalle court pour la vérification mais on ne sauvegarde que selon POSITION_INTERVAL
}

function startGeolocationWatch() {
    if (!navigator.geolocation) {
        console.error('Geolocation is not supported');
        return;
    }

    watchId = navigator.geolocation.watchPosition(
        position => {
            const currentTime = Date.now();
            const currentWalk = JSON.parse(localStorage.getItem('currentWalk'));

            // Vérifier si suffisamment de temps s'est écoulé depuis le dernier enregistrement
            if (currentWalk.positions.length === 0 || currentTime - currentWalk.lastRecordedTime >= POSITION_INTERVAL) {
                const locationData = {
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude,
                    accuracy: position.coords.accuracy,
                    timestamp: new Date()
                };

                currentWalk.positions.push(locationData);
                currentWalk.lastRecordedTime = currentTime; // Mise à jour du timestamp
                localStorage.setItem('currentWalk', JSON.stringify(currentWalk));

                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnLocationUpdate', locationData);
                }

                syncIfNeeded(currentWalk);
            }
        },
        error => console.error(error),
        {
            enableHighAccuracy: true,
            timeout: 5000,
            maximumAge: 30000
        }
    );
}

// Fonction pour obtenir une position aléatoire basée sur une position de base et un décalage maximum
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
    initializeWalkData();
    
    if (simulate) {
        const baseLat = 46.1733888;
        const baseLon = -1.1337728;
        const maxOffset = 0.001; // Décalage maximum pour la simulation
        startSimulation(baseLat, baseLon, maxOffset);
    } else {
        startGeolocationWatch();
    }
};

window.startTimer = () => {
    let startTime = new Date();
    localStorage.setItem('walkStartTime', startTime.toISOString());
};

window.getStoredUntrackedWalkData = () => {
    return localStorage.getItem('walkStartTime');
};

window.getStoredWalkData = () => {
    const walkData = localStorage.getItem('currentWalk');
    return walkData ? JSON.parse(walkData) : null;
};

window.isTrackingEnabled = () => {
    const walkData = localStorage.getItem('currentWalk');
    return walkData !== null;
}

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

window.stopUntrackedWalk = () => {
  localStorage.removeItem('walkStartTime');
};

window.checkForOngoingWalk = () => {
    const walkData = localStorage.getItem('currentWalk');
    return walkData ? JSON.parse(walkData) : null;
};

