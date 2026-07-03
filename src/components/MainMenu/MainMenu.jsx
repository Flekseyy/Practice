import { useNavigate } from 'react-router-dom';
import MenuButton from './MenuButton/MenuButton';
import './MainMenu.css';

const MainMenu = () => {
    const navigate = useNavigate();

    const handleClosedRequestsClick = () => {
        navigate('/closed-requests');
    };

    const handleOpenRequestsClick = () => {
        navigate('/open-requests');
    };

    const handleErrorRequestsClick = () => {
        navigate('/error-requests');
    };

    return (
        <main className="main-menu">
            <div className="main-menu__container">
                <MenuButton 
                    topText="Закрытые" 
                    size="small" 
                    color="#296153"
                    onClick={handleClosedRequestsClick}
                />
                <MenuButton 
                    topText="Открытые" 
                    size="large" 
                    color="#296153"
                    onClick={handleOpenRequestsClick}
                />
                <MenuButton 
                    topText="Ошибочные" 
                    size="small" 
                    color="#612e29"
                    onClick={handleErrorRequestsClick}
                />
            </div>
        </main>
    );
};

export default MainMenu;