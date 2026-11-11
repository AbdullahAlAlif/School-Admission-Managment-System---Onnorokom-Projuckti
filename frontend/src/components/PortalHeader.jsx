export default function PortalHeader({ page, onPageChange }) {
    return (
        <header className="topbar">
            <div className="brand">
                <div className="brand-icon"><span>SA</span></div>
                <div>
                    <h1>School Admission Portal</h1>
                    <p>Admission management system</p>
                </div>
            </div>

            <nav className="page-nav" aria-label="Portal pages">
                <button
                    className={page === "applicant" ? "active" : ""}
                    aria-current={page === "applicant" ? "page" : undefined}
                    onClick={() => onPageChange("applicant")}
                >
                    Applicant
                </button>
                <button
                    className={page === "admin" ? "active" : ""}
                    aria-current={page === "admin" ? "page" : undefined}
                    onClick={() => onPageChange("admin")}
                >
                    Admin
                </button>
            </nav>
        </header>
    );
}
