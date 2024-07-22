import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react-swc';
import { viteSingleFile } from 'vite-plugin-singlefile';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [react(), viteSingleFile()],
    server: {
        proxy: {
            '/slm': {
                // needs to be the resourceName
                target: 'http://127.0.0.1:30120',
                changeOrigin: true,
            },
        },
    },
});
