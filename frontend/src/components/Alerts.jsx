export default function Alerts({
    error,
    notice,
    onClearError,
    onClearNotice
}) {
    return (
        <>
            {error && (
                <div className="alert alert-error" role="alert">
                    <strong>Something went wrong</strong>
                    <span>{error}</span>
                    <button onClick={onClearError} aria-label="Dismiss error">×</button>
                </div>
            )}
            {notice && (
                <div className="alert alert-success" role="status">
                    <strong>{notice}</strong>
                    <button onClick={onClearNotice} aria-label="Dismiss notification">×</button>
                </div>
            )}
        </>
    );
}
