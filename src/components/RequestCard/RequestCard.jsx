import RequestButton from '../RequestButton/RequestButton';
import './RequestCard.css';

const RequestCard = ({ type, variant, left, top }) => {
    return (
        <div 
            className={`request-card request-card--variant-${variant}`}
            style={{ left: `${left}px`, top: `${top}px` }}
        >
            <span className="request-card__title">{type}</span>
            <div className="request-card__buttons">
                <RequestButton text="Отмена" variant="cancel" />
                <RequestButton text="Подробно" variant="details" />
            </div>
        </div>
    );
};

export default RequestCard;