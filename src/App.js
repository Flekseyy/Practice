import { BrowserRouter, Routes, Route } from 'react-router-dom';
import MainMenu from './components/MainMenu/MainMenu';
import ClosedRequestsPage from './pages/ClosedRequestsPage/ClosedRequestsPage';
import OpenRequestsPage from './pages/OpenRequestsPage/OpenRequestsPage';
import ErrorRequestsPage from './pages/ErrorRequestsPage/ErrorRequestsPage';

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<MainMenu />} />
                <Route path="/closed-requests" element={<ClosedRequestsPage />} />
                <Route path="/open-requests" element={<OpenRequestsPage />} />
                <Route path="/error-requests" element={<ErrorRequestsPage />} />
            </Routes>
            <svg xmlns="http://www.w3.org/2000/svg" version="1.1" style={{ position: 'absolute', width: 0, height: 0, pointerEvents: 'none' }}>
                <defs>
                    <filter id="goo">
                        <feGaussianBlur in="SourceGraphic" result="blur" stdDeviation="12"></feGaussianBlur>
                        <feColorMatrix in="blur" mode="matrix" values="1 0 0 0 0 0 1 0 0 0 0 0 1 0 0 0 0 0 200 -5" result="goo"></feColorMatrix>
                        <feBlend in2="goo" in="SourceGraphic" result="mix"></feBlend>
                    </filter>
                </defs>
            </svg>
        </BrowserRouter>
    );
}

export default App;