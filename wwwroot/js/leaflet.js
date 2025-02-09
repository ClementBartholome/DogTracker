export function load_map() {
    let map = L.map('map').setView([46.1733888, -1.1337728], 19);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
    }).addTo(map);

    let customIcon = L.icon({
        iconUrl: './images/poop-icon.png',
        iconSize: [38, 38],
        iconAnchor: [22, 38],
        popupAnchor: [-3, -38]
    });

    map.on('click', function (e) {
        let marker = L.marker([e.latlng.lat, e.latlng.lng], {icon: customIcon}).addTo(map)
            .bindPopup(getCustomPopupContent(e.latlng.lat, e.latlng.lng))
            .openPopup();
        marker.on('popupopen', function () {
            document.getElementById(`delete-marker-${e.latlng.lat}-${e.latlng.lng}`).addEventListener('click', function () {
                map.removeLayer(marker);
                removeMarker(e.latlng.lat, e.latlng.lng);
            });
        });
        saveMarker(e.latlng.lat, e.latlng.lng);
    });

    loadMarkers(map, customIcon);
    loadMarkersFromJson(map, customIcon);

    let locationMarker = null;

    map.locate({setView: true, maxZoom: 19});
    map.on('locationfound', function (e) {
        if (locationMarker) {
            locationMarker.setLatLng([e.latlng.lat, e.latlng.lng]);
        } else {
            locationMarker = L.marker([e.latlng.lat, e.latlng.lng]).addTo(map)
                .bindPopup('Position actuelle')
                .openPopup();
            window.currentLocationMarker = locationMarker;
        }
    });

    console.log('Map loaded', map);

    window.currentMap = map;
    return map;
}

async function loadMarkersFromJson(map, icon) {
    const response = await fetch('/json/opendata_sac_canin.json');
    const data = await response.json();

    data.forEach(item => {
        const coordinates = item.fields.coordinates;
        const emplacement = item.fields.emplacement;
        if (coordinates && coordinates.length === 2) {
            const [lng, lat] = coordinates;
            let marker = L.marker([lat, lng], {icon}).addTo(map)
                .bindPopup(getCustomPopupContent(lat, lng, emplacement));
            marker.on('popupopen', function () {
                document.getElementById(`delete-marker-${lat}-${lng}`).addEventListener('click', function () {
                    map.removeLayer(marker);
                    removeMarker(lat, lng);
                });
            });
        }
    });
}

export function addCurrentPositionMarker(lat, lng) {
    const map = window.currentMap;
    const marker = window.currentLocationMarker;

    if (!map || !marker) {
        console.error('La carte ou le marqueur n\'est pas initialisé');
        return null;
    }

    marker.setLatLng([lat, lng]);
    map.setView([lat, lng], map.getZoom());

    return {
        setLatLng: function(newLat, newLng) {
            marker.setLatLng([newLat, newLng]);
            map.setView([newLat, newLng], map.getZoom());
        }
    };
}

function getCustomPopupContent(lat, lng, emplacement = '') {
    return `<div>${emplacement}</div><br><button id="delete-marker-${lat}-${lng}">Supprimer</button>`;
}

function saveMarker(lat, lng) {
    let markers = JSON.parse(localStorage.getItem('markers')) || [];
    markers.push({lat, lng});
    localStorage.setItem('markers', JSON.stringify(markers));
}

function removeMarker(lat, lng) {
    let markers = JSON.parse(localStorage.getItem('markers')) || [];
    markers = markers.filter(marker => marker.lat !== lat || marker.lng !== lng);
    localStorage.setItem('markers', JSON.stringify(markers));
}

function loadMarkers(map, icon) {
    let markers = JSON.parse(localStorage.getItem('markers')) || [];
    markers.forEach(marker => {
        let loadedMarker = L.marker([marker.lat, marker.lng], {icon}).addTo(map)
            .bindPopup(getCustomPopupContent(marker.lat, marker.lng));
        loadedMarker.on('popupopen', function () {
            document.getElementById(`delete-marker-${marker.lat}-${marker.lng}`).addEventListener('click', function () {
                map.removeLayer(loadedMarker);
                removeMarker(marker.lat, marker.lng);
            });
        });
    });
}

window.load_map = load_map;
window.addCurrentPositionMarker = addCurrentPositionMarker;