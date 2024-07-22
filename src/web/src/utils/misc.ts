export const isDev = () => import.meta.env.MODE === 'development';

export const getResourceName = (): string => {
    return isDev() ? 'slm' : window.location.pathname.split('/')[1];
};
