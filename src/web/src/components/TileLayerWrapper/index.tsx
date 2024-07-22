import { FC, useEffect } from 'react';
import { TileLayer, TileLayerProps, useMap } from 'react-leaflet';
import L, { LatLngBounds } from 'leaflet';

export const TileLayerWrapper: FC<TileLayerProps> = ({ url, minZoom, maxZoom, keepBuffer, noWrap, ...rest }: TileLayerProps) => {
    const map = useMap();

    useEffect(() => {
        map.attributionControl.setPrefix('<a href="https://github.com/all-in-simplicity/simple-livemap">SimpleLiveMap 1.0</a>');
        const southWest = map.unproject([0, 8192], map.getMaxZoom());
        const northEast = map.unproject([8192, 0], map.getMaxZoom());
        map.setMaxBounds(new L.LatLngBounds(southWest, northEast));
    }, [map]);

    return (
        <TileLayer
            keepBuffer={keepBuffer}
            bounds={new LatLngBounds(map.unproject([0, 8192], map.getMaxZoom()), map.unproject([8192, 0], map.getMaxZoom()))}
            noWrap={noWrap}
            url={url}
            minZoom={minZoom}
            maxZoom={maxZoom}
            {...rest}
        />
    );
};
