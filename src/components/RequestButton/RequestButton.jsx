import './RequestButton.css';

const RequestButton = ({ text, variant }) => {
    return (
        <button className={`request-button request-button--variant-${variant}`}>
            <span className="request-button__text">{text}</span>
        </button>
    );
};

export default RequestButton;