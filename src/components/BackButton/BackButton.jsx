import { useNavigate } from 'react-router-dom';
import './BackButton.css';
import '../../styles/animation-back-button.css';

const BackButton = () => {
    const navigate = useNavigate();

    const handleClick = () => {
        navigate('/');
    };

    return (
        <button 
            className="back-button hover-scale-x"
            onClick={handleClick}
        >
            <img 
                src="https://i.ibb.co/m5QxGxRr/icons8-90.png" 
                alt="Back to menu"
                className="back-button__icon"
            />
        </button>
    );
};
export default BackButton;

