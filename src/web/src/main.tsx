import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App.tsx';
import './index.css';
import { SSEProvider } from 'react-hooks-sse';
import { getResourceName, isDev } from './utils/misc.ts';

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <SSEProvider endpoint={`${isDev() ? '/slm/sse' : getResourceName() + '/sse'}`}>
            <App />
        </SSEProvider>
    </React.StrictMode>
);
