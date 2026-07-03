import { useState, useEffect } from 'react';
import BackButton from '../../components/BackButton/BackButton';
import InfoBox from '../../components/InfoBox/InfoBox';
import RequestCard from '../../components/RequestCard/RequestCard';
import './ClosedRequestsPage.css';

const ClosedRequestsPage = () => {
    const [requests, setRequests] = useState([]);

    useEffect(() => {
        const generateRequest = () => {
            const types = ['Кредит', 'Выдача карты'];
            const randomType = types[Math.floor(Math.random() * types.length)];
            const newRequest = {
                id: Date.now(),
                type: randomType
            };
            setRequests(prev => [...prev, newRequest]);
        };

        generateRequest();
        const interval = setInterval(generateRequest, 120000);

        return () => clearInterval(interval);
    }, []);

    return (
        <main className="closed-requests-page">
            <BackButton />
            <InfoBox value={100} variant="success" />
            {requests.map((request, index) => (
                <RequestCard 
                    key={request.id}
                    type={request.type}
                    variant="success"
                    left={70 + (index * 320)}
                    top={120}
                />
            ))}
        </main>
    );
};

export default ClosedRequestsPage;