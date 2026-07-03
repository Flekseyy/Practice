import './InfoBox.css';

const InfoBox = ({ value, variant = 'success' }) => {
    return (
        <div className={`info-box info-box--variant-${variant}`}>
            <span className="info-box__text">{value}</span>
        </div>
    );
};

export default InfoBox;