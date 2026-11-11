import ClassCatalog from "../components/ClassCatalog";
import CreateClassForm from "../components/CreateClassForm";
import SummaryCards from "../components/SummaryCards";

export default function AdminPage({
    classes,
    initialLoading,
    busyAction,
    className,
    totalSeatsInput,
    totalSeatCapacity,
    availableSeats,
    heldSeats,
    confirmedSeats,
    onClassNameChange,
    onTotalSeatsChange,
    onCreateClass,
    onRefresh
}) {
    return (
        <>
            <section className="page-heading">
                <div>
                    <p className="eyebrow">ADMINISTRATION</p>
                    <h2>Class management</h2>
                    <p className="muted">Manage classes and review seat availability.</p>
                </div>
                <button className="button button-secondary" onClick={onRefresh} disabled={initialLoading}>
                    Refresh
                </button>
            </section>

            <SummaryCards items={[
                { label: "Total classes", value: classes.length, foot: "Admission shifts" },
                { label: "Total seats", value: totalSeatCapacity, foot: "Across all classes" },
                { label: "Available seats", value: availableSeats, foot: "Currently open", valueClass: "green-text" },
                { label: "Held / confirmed", value: `${heldSeats} / ${confirmedSeats}`, foot: "Current seat counts" }
            ]} />

            <div className="content-grid">
                <ClassCatalog classes={classes} initialLoading={initialLoading} />
                <CreateClassForm
                    className={className}
                    totalSeats={totalSeatsInput}
                    busyAction={busyAction}
                    onClassNameChange={onClassNameChange}
                    onTotalSeatsChange={onTotalSeatsChange}
                    onSubmit={onCreateClass}
                />
            </div>
        </>
    );
}
