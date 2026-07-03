import './MenuButton.css';
import '../../../styles/animations-main-buttons.css';
import '../../../styles/fill-animation.css';

const MenuButton = ({ topText, bottomText, size, color, onClick }) => {
    const sizeClass = size === 'large' ? 'menu-button--size-large' : 'menu-button--size-small';

    return (
        <button 
            className={`menu-button ${sizeClass} hover-lift fill-btn`}
            style={{ color: color }}
            onClick={onClick}
        >
            <span className="menu-button__text-top">{topText}</span>
            <span className="menu-button__text-bottom">{bottomText}</span>
        </button>
    );
};

export default MenuButton;