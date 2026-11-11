export default function ClassCatalog({
    classes,
    initialLoading,
    allowApply = false,
    studentIdValid = false,
    busyAction,
    onApply
}) {
    return (
        <section className="panel">
            <div className="panel-title-row">
                <div>
                    <h3>{allowApply ? "Available classes" : "Class overview"}</h3>
                    <p className="muted">
                        {allowApply
                            ? "Choose a class and reserve a seat."
                            : "Seat capacity and current admission counts."}
                    </p>
                </div>
                <span className="count-badge">{classes.length} classes</span>
            </div>

            {initialLoading ? (
                <div className="empty-state">Loading classes...</div>
            ) : classes.length === 0 ? (
                <div className="empty-state">
                    <h4>No classes available</h4>
                    <p>No classes have been created yet.</p>
                </div>
            ) : (
                <div className="class-list">
                    {classes.map(schoolClass => {
                        const availableSeats = Number(schoolClass.availableSeats) || 0;
                        const noSeats = availableSeats <= 0;
                        const classBusy = busyAction === `apply:${schoolClass.id}`;

                        return (
                            <article className="class-card" key={schoolClass.id}>
                                <div className="class-card-top">
                                    <div>
                                        <h4>{schoolClass.name}</h4>
                                        <p className="muted small-text">ID: {schoolClass.id}</p>
                                    </div>
                                    <span className={`status-badge ${noSeats ? "status-expired" : "status-confirmed"}`}>
                                        {noSeats ? "FULL" : "OPEN"}
                                    </span>
                                </div>

                                <div className="seat-grid">
                                    <SeatStat label="Total" value={schoolClass.totalSeats} />
                                    <SeatStat label="Held" value={schoolClass.heldSeats} />
                                    <SeatStat label="Confirmed" value={schoolClass.confirmedSeats} />
                                    <SeatStat label="Available" value={availableSeats} valueClass="green-text" />
                                </div>

                                <div className="class-card-footer">
                                    <span className="muted small-text">
                                        {noSeats ? "No seats available." : `${availableSeats} seats remaining`}
                                    </span>
                                    {allowApply && (
                                        <button
                                            className="button button-primary"
                                            onClick={() => onApply(schoolClass)}
                                            disabled={!studentIdValid || noSeats || busyAction !== null}
                                            title={!studentIdValid ? "Enter a valid student ID first" : ""}
                                        >
                                            {classBusy
                                                ? "Applying..."
                                                : noSeats
                                                    ? "Fully booked"
                                                    : !studentIdValid
                                                        ? "Enter student ID"
                                                        : "Apply for admission"}
                                        </button>
                                    )}
                                </div>
                            </article>
                        );
                    })}
                </div>
            )}
        </section>
    );
}

function SeatStat({ label, value, valueClass = "" }) {
    return (
        <div className="seat-stat">
            <span>{label}</span>
            <strong className={valueClass}>{Number(value) || 0}</strong>
        </div>
    );
}
