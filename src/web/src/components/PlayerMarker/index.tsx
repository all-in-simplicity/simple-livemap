import { Popup } from 'react-leaflet';
import ReactLeafletDriftMarker from 'react-leaflet-drift-marker';
import { icon, Marker } from 'leaflet';
import { Player } from '../../types/player.ts';
import { getResourceName } from '../../utils/misc.ts';

// fix for marker icon not found in prod environment
// ref: https://stackoverflow.com/questions/41144319/leaflet-marker-not-found-production-env
const iconRetinaUrl = `${getResourceName()}/assets/marker-icon-2x.png`;
const iconUrl = `${getResourceName()}/assets/marker-icon.png`;
const shadowUrl = `${getResourceName()}/assets/marker-shadow.png`;

const iconDefault = icon({
    iconRetinaUrl,
    iconUrl,
    shadowUrl,
    iconSize: [25, 41],
    iconAnchor: [12, 41],
    popupAnchor: [1, -34],
    tooltipAnchor: [16, -28],
    shadowSize: [41, 41],
});
Marker.prototype.options.icon = iconDefault;

interface PlayerMarkerProps {
    player: Player;
}

export const PlayerMarker = ({ player }: PlayerMarkerProps) => {
    return player.position ? (
        <ReactLeafletDriftMarker icon={iconDefault} position={[player.position.y, player.position.x]} duration={1250} key={player.name}>
            <Popup>{player.name}</Popup>
        </ReactLeafletDriftMarker>
    ) : null;
};
