import { isValidStudentId } from "../utils/admissions";

export default function StudentIdentityCard({ studentId, onStudentIdChange }) {
    const valid = isValidStudentId(studentId.trim());

    return (
        <section className="panel identity-panel">
            <div className="panel-heading">
                <div className="section-icon blue-icon">ID</div>
                <div>
                    <h3>Student identification</h3>
                    <p className="muted">Enter your 17-digit birth registration number.</p>
                </div>
            </div>

            <div className="identity-input">
                <input
                    type="text"
                    inputMode="numeric"
                    autoComplete="off"
                    maxLength={17}
                    value={studentId}
                    onChange={event => onStudentIdChange(
                        event.target.value.replace(/\D/g, "").slice(0, 17)
                    )}
                    placeholder="17-digit birth registration number"
                    aria-label="Student birth registration number"
                />
                <span className={`identity-status ${valid ? "valid" : "invalid"}`}>
                    {valid ? "Valid ID" : `${studentId.length}/17 digits`}
                </span>
            </div>
            <p className="helper-text">
                Your ID is saved in this browser to retrieve your applications later.
            </p>
        </section>
    );
}
