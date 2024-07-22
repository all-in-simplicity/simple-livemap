import { MapContainer } from 'react-leaflet';
import { FC } from 'react';
import { PlayerMarker } from './components/PlayerMarker';
import { usePlayers } from './hooks/usePlayers.ts';
import { CustomCRS } from './types/customCRS.ts';
import styled, { keyframes } from 'styled-components';
import { TileLayerWrapper } from './components/TileLayerWrapper';
import { getResourceName } from './utils/misc.ts';

const fadeIn = keyframes`
    from {
        opacity: 0;
    }
    to {
        opacity: 100;
    }
`;

const Wrapper = styled.div`
    display: flex;
    width: 100%;
    height: 100%;
    animation: ${fadeIn} 0.7s;
    background-color: #0fa7d0;
`;

const App: FC = () => {
    const { players } = usePlayers();

    return (
        <Wrapper>
            <MapContainer
                style={{ height: '100%', width: '100%', backgroundColor: 'inherit' }}
                crs={CustomCRS}
                minZoom={3}
                maxZoom={5}
                center={[0, 0]}
                preferCanvas={true}
                zoom={3}
            >
                <TileLayerWrapper
                    keepBuffer={64}
                    noWrap={true}
                    url={`/${getResourceName()}/assets/maps/atlas/{z}/{x}/{y}.jpg`}
                    minZoom={0}
                    maxZoom={5}
                />
                {players && players?.map((player) => <PlayerMarker player={player} key={player.name} />)}
            </MapContainer>
        </Wrapper>
    );
};

export default App;
