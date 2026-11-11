import ApplicationList from "../components/ApplicationList";
import ClassCatalog from "../components/ClassCatalog";
import StudentIdentityCard from "../components/StudentIdentityCard";
import SummaryCards from "../components/SummaryCards";
import { isValidStudentId } from "../utils/admissions";

export default function ApplicantPage({
    classes,
    applications,
    studentId,
    initialLoading,
    busyAction,
    totalSeatCapacity,
    availableSeats,
    activeApplications,
    onStudentIdChange,
    onApply,
    onConfirm,
    onCancel,
    onRefresh
}) {
    const studentIdValid = isValidStudentId(studentId.trim());

    return (
        <>
            <section className="page-heading">
                <div>
                    <p className="eyebrow">ADMISSION SESSION</p>
                    <h2>Find your class</h2>
                    <p className="muted">Browse classes and manage your applications.</p>
                </div>
                <button className="button button-secondary" onClick={onRefresh} disabled={initialLoading}>
                    Refresh
                </button>
            </section>

            <StudentIdentityCard studentId={studentId} onStudentIdChange={onStudentIdChange} />

            <SummaryCards items={[
                { label: "Total classes", value: classes.length, foot: "Admission shifts" },
                { label: "Total seats", value: totalSeatCapacity, foot: "Across all classes" },
                { label: "Available seats", value: availableSeats, foot: "Currently open", valueClass: "green-text" },
                { label: "Your active holds", value: studentIdValid ? activeApplications : "—", foot: "Reserved seats" }
            ]} />

            <div className="content-grid applicant-grid">
                <ClassCatalog
                    classes={classes}
                    initialLoading={initialLoading}
                    allowApply
                    studentIdValid={studentIdValid}
                    busyAction={busyAction}
                    onApply={onApply}
                />
            </div>

            <ApplicationList
                applications={applications}
                studentIdValid={studentIdValid}
                initialLoading={initialLoading}
                busyAction={busyAction}
                onConfirm={onConfirm}
                onCancel={onCancel}
            />
        </>
    );
}
