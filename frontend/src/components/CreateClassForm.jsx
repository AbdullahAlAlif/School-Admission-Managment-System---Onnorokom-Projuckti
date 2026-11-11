export default function CreateClassForm({
    className,
    totalSeats,
    busyAction,
    onClassNameChange,
    onTotalSeatsChange,
    onSubmit
}) {
    return (
        <section className="panel create-panel">
            <div className="panel-title-row">
                <div>
                    <h3>Create a class</h3>
                    <p className="muted">Add a class or admission shift.</p>
                </div>
                <div className="section-icon purple-icon">+</div>
            </div>

            <form onSubmit={onSubmit} className="create-form">
                <label htmlFor="className">Class name</label>
                <input
                    id="className"
                    type="text"
                    value={className}
                    maxLength={100}
                    onChange={event => onClassNameChange(event.target.value)}
                    placeholder="e.g. Class 6 - Morning Shift"
                    required
                />

                <label htmlFor="totalSeats">Total seats</label>
                <input
                    id="totalSeats"
                    type="number"
                    min="1"
                    max="100000"
                    step="1"
                    value={totalSeats}
                    onChange={event => onTotalSeatsChange(event.target.value)}
                    placeholder="e.g. 120"
                    required
                />

                <button
                    type="submit"
                    className="button button-dark full-width"
                    disabled={busyAction !== null}
                >
                    {busyAction === "create" ? "Creating..." : "Create class"}
                </button>
            </form>
        </section>
    );
}
