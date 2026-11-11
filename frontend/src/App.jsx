import { Component, useCallback, useEffect, useRef, useState } from "react";
import {
    cancelHold,
    confirmHold,
    createClass,
    createHold,
    getClasses,
    getHold,
    getMyHolds
} from "./services/api";
import Alerts from "./components/Alerts";
import PortalHeader from "./components/PortalHeader";
import AdminPage from "./pages/AdminPage";
import ApplicantPage from "./pages/ApplicantPage";
import {
    getDisplayStatus,
    getRemainingSeconds,
    isValidStudentId,
    sumClassValues
} from "./utils/admissions";

function AdmissionApp() {
    const [page, setPage] = useState("applicant");
    const [studentId, setStudentId] = useState(
        () => localStorage.getItem("studentId") || ""
    );
    const cleanStudentId = studentId.trim();
    const studentIdValid = isValidStudentId(cleanStudentId);

    const [classes, setClasses] = useState([]);
    const [applications, setApplications] = useState([]);
    const [initialLoading, setInitialLoading] = useState(true);
    const [busyAction, setBusyAction] = useState(null);
    const [error, setError] = useState("");
    const [notice, setNotice] = useState("");
    const [className, setClassName] = useState("");
    const [totalSeatsInput, setTotalSeatsInput] = useState("");
    const [clockTick, setClockTick] = useState(0);
    const expiryChecks = useRef(new Set());

    useEffect(() => {
        if (cleanStudentId) {
            localStorage.setItem("studentId", cleanStudentId);
        } else {
            localStorage.removeItem("studentId");
        }
    }, [cleanStudentId]);

    const refreshDashboard = useCallback(async (silent = false) => {
        try {
            const [classData, holdData] = await Promise.all([
                getClasses(),
                studentIdValid ? getMyHolds(cleanStudentId) : Promise.resolve([])
            ]);

            setClasses(Array.isArray(classData) ? classData : []);
            setApplications(Array.isArray(holdData) ? holdData : []);
            setError("");
        } catch (requestError) {
            if (!silent) {
                setError(requestError.message);
            }
        } finally {
            setInitialLoading(false);
        }
    }, [cleanStudentId, studentIdValid]);

    useEffect(() => {
        void Promise.resolve().then(() => refreshDashboard());
        const timer = setInterval(() => refreshDashboard(true), 5000);
        return () => clearInterval(timer);
    }, [refreshDashboard]);

    useEffect(() => {
        const timer = setInterval(() => setClockTick(tick => tick + 1), 1000);
        return () => clearInterval(timer);
    }, []);

    useEffect(() => {
        const overdueApplications = applications.filter(application =>
            application.status === "HELD" &&
            getRemainingSeconds(application.expiresAt) <= 0 &&
            !expiryChecks.current.has(application.holdId)
        );

        overdueApplications.forEach(application => {
            expiryChecks.current.add(application.holdId);
            getHold(application.holdId)
                .then(latestHold => {
                    setApplications(previous => previous.map(item =>
                        item.holdId === latestHold.holdId
                            ? { ...item, status: latestHold.status }
                            : item
                    ));
                })
                .catch(requestError => {
                    setError(requestError.message);
                    expiryChecks.current.delete(application.holdId);
                });
        });
    }, [applications, clockTick]);

    async function handleCreateClass(event) {
        event.preventDefault();
        setError("");
        setNotice("");

        const trimmedName = className.trim();
        const seats = Number(totalSeatsInput);

        if (!trimmedName) {
            setError("Please enter a class name.");
            return;
        }
        if (trimmedName.length > 100) {
            setError("Class name cannot exceed 100 characters.");
            return;
        }
        if (!Number.isInteger(seats) || seats < 1 || seats > 100000) {
            setError("Seat count must be between 1 and 100000.");
            return;
        }

        try {
            setBusyAction("create");
            const result = await createClass({ name: trimmedName, totalSeats: seats });
            setNotice(`Successfully created ${result.name}.`);
            setClassName("");
            setTotalSeatsInput("");
            await refreshDashboard(true);
        } catch (requestError) {
            setError(requestError.message);
        } finally {
            setBusyAction(null);
        }
    }

    async function handleApply(schoolClass) {
        setError("");
        setNotice("");
        if (!studentIdValid) {
            setError("Enter a valid 17-digit birth registration number first.");
            return;
        }

        try {
            setBusyAction(`apply:${schoolClass.id}`);
            const result = await createHold(schoolClass.id, cleanStudentId);
            setNotice(`Your seat has been reserved. Application ID: ${result.holdId}`);
            await refreshDashboard(true);
        } catch (requestError) {
            setError(requestError.message);
            await refreshDashboard(true);
        } finally {
            setBusyAction(null);
        }
    }

    async function handleConfirm(application) {
        setError("");
        setNotice("");
        try {
            setBusyAction(`confirm:${application.holdId}`);
            await confirmHold(application.holdId, cleanStudentId);
            setNotice("Admission has been confirmed.");
            await refreshDashboard(true);
        } catch (requestError) {
            setError(requestError.message);
            await refreshDashboard(true);
        } finally {
            setBusyAction(null);
        }
    }

    async function handleCancel(application) {
        if (!window.confirm("Are you sure you want to cancel this application?")) {
            return;
        }

        setError("");
        setNotice("");
        try {
            setBusyAction(`cancel:${application.holdId}`);
            await cancelHold(application.holdId, cleanStudentId);
            setNotice("Your application has been cancelled.");
            await refreshDashboard(true);
        } catch (requestError) {
            setError(requestError.message);
            await refreshDashboard(true);
        } finally {
            setBusyAction(null);
        }
    }

    const totalSeatCapacity = sumClassValues(classes, "totalSeats");
    const availableSeats = sumClassValues(classes, "availableSeats");
    const heldSeats = sumClassValues(classes, "heldSeats");
    const confirmedSeats = sumClassValues(classes, "confirmedSeats");
    const activeApplications = applications.filter(application =>
        getDisplayStatus(application) === "HELD"
    ).length;

    return (
        <div className="app-shell">
            <PortalHeader page={page} onPageChange={setPage} />
            <main className="main-content">
                <Alerts
                    error={error}
                    notice={notice}
                    onClearError={() => setError("")}
                    onClearNotice={() => setNotice("")}
                />

                {page === "admin" ? (
                    <AdminPage
                        classes={classes}
                        initialLoading={initialLoading}
                        busyAction={busyAction}
                        className={className}
                        totalSeatsInput={totalSeatsInput}
                        totalSeatCapacity={totalSeatCapacity}
                        availableSeats={availableSeats}
                        heldSeats={heldSeats}
                        confirmedSeats={confirmedSeats}
                        onClassNameChange={setClassName}
                        onTotalSeatsChange={setTotalSeatsInput}
                        onCreateClass={handleCreateClass}
                        onRefresh={() => refreshDashboard()}
                    />
                ) : (
                    <ApplicantPage
                        classes={classes}
                        applications={applications}
                        studentId={studentId}
                        initialLoading={initialLoading}
                        busyAction={busyAction}
                        totalSeatCapacity={totalSeatCapacity}
                        availableSeats={availableSeats}
                        activeApplications={activeApplications}
                        onStudentIdChange={setStudentId}
                        onApply={handleApply}
                        onConfirm={handleConfirm}
                        onCancel={handleCancel}
                        onRefresh={() => refreshDashboard()}
                    />
                )}

                <footer className="footer">
                    <p>School Admission System</p>
                    <p>Seat counts refresh automatically every 5 seconds.</p>
                </footer>
            </main>
        </div>
    );
}

class AppErrorBoundary extends Component {
    constructor(props) {
        super(props);
        this.state = { hasError: false };
    }

    static getDerivedStateFromError() {
        return { hasError: true };
    }

    componentDidCatch(error, errorInfo) {
        console.error("Application rendering error:", error, errorInfo);
    }

    render() {
        if (this.state.hasError) {
            return (
                <div className="fatal-error">
                    <h1>Something went wrong</h1>
                    <p>The application encountered an unexpected error. Please reload.</p>
                    <button className="button button-primary" onClick={() => window.location.reload()}>
                        Reload application
                    </button>
                </div>
            );
        }
        return this.props.children;
    }
}

export default function App() {
    return (
        <AppErrorBoundary>
            <AdmissionApp />
        </AppErrorBoundary>
    );
}
