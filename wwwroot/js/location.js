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

    if (simulate) {
        const baseLat = 46.1733888
        const baseLon = -1.1337728;  
        const maxOffset = 0.001; // Maximum offset for simulation

        simulationInterval = setInterval(() => {
            const simulatedPosition = getRandomPosition(baseLat, baseLon, maxOffset);
            dotNetHelper.invokeMethodAsync('OnLocationUpdate', simulatedPosition);
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
                dotNetHelper.invokeMethodAsync('OnLocationUpdate', locationData);
            },
            error => console.error(error),
            { enableHighAccuracy: true }
        );
    }
};

window.stopWatchingPosition = () => {
    if (simulationInterval !== null) {
        clearInterval(simulationInterval);
        simulationInterval = null;
    }
    if (watchId !== null) {
        navigator.geolocation.clearWatch(watchId);
        watchId = null;
    }
};