import { formatCountdown, getDisplayStatus, getRemainingSeconds } from "../utils/admissions";

export default function ApplicationList({
    applications,
    studentIdValid,
    initialLoading,
    busyAction,
    onConfirm,
    onCancel
}) {
    return (
        <section className="panel applications-panel">
            <div className="panel-title-row">
                <div>
                    <h3>My applications</h3>
                    <p className="muted">Track, confirm or cancel your applications.</p>
                </div>
                <span className="count-badge">
                    {studentIdValid ? applications.length : 0} applications
                </span>
            </div>

            {!studentIdValid ? (
                <div className="empty-state">
                    <h4>Student identification required</h4>
                    <p>Enter your valid 17-digit birth registration number to see your applications.</p>
                </div>
            ) : initialLoading ? (
                <div className="empty-state">Loading applications...</div>
            ) : applications.length === 0 ? (
                <div className="empty-state">
                    <h4>No applications yet</h4>
                    <p>Your applications will appear here after you reserve a seat.</p>
                </div>
            ) : (
                <div className="application-list">
                    {applications.map(application => {
                        const status = getDisplayStatus(application);
                        const remainingSeconds = getRemainingSeconds(application.expiresAt);
                        const isHeld = status === "HELD";
                        const isBusy =
                            busyAction === `confirm:${application.holdId}` ||
                            busyAction === `cancel:${application.holdId}`;

                        return (
                            <article className="application-card" key={application.holdId}>
                                <div className="application-main">
                                    <div>
                                        <h4>{application.className}</h4>
                                        <p className="muted small-text">
                                            Application ID: {application.holdId}
                                        </p>
                                    </div>
                                    <span className={`status-badge status-${status.toLowerCase()}`}>
                                        {status}
                                    </span>
                                </div>

                                <div className="application-details">
                                    <span>
                                        <strong>Expires:</strong>{" "}
                                        {new Date(application.expiresAt).toLocaleString()}
                                    </span>
                                    {isHeld ? (
                                        <span className="countdown">
                                            Time remaining: {formatCountdown(remainingSeconds)}
                                        </span>
                                    ) : status === "CONFIRMED" ? (
                                        <span className="green-text">Admission confirmed.</span>
                                    ) : status === "EXPIRED" ? (
                                        <span className="red-text">Reservation time has expired.</span>
                                    ) : (
                                        <span className="muted">This application was cancelled.</span>
                                    )}
                                </div>

                                {isHeld && (
                                    <div className="application-actions">
                                        <button
                                            className="button button-primary"
                                            onClick={() => onConfirm(application)}
                                            disabled={remainingSeconds <= 0 || busyAction !== null}
                                        >
                                            {busyAction === `confirm:${application.holdId}`
                                                ? "Processing..."
                                                : "Confirm admission"}
                                        </button>
                                        <button
                                            className="button button-danger-outline"
                                            onClick={() => onCancel(application)}
                                            disabled={remainingSeconds <= 0 || busyAction !== null}
                                        >
                                            {busyAction === `cancel:${application.holdId}`
                                                ? "Cancelling..."
                                                : "Cancel application"}
                                        </button>
                                    </div>
                                )}
                                {isBusy && <span className="sr-only" role="status">Updating application</span>}
                            </article>
                        );
                    })}
                </div>
            )}
        </section>
    );
}
