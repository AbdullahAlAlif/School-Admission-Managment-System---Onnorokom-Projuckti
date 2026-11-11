import { getServerNowMs } from "../services/api";

export function isValidStudentId(value) {
    return /^\d{17}$/.test(value);
}

export function getRemainingSeconds(expiresAt) {
    const expiryTime = new Date(expiresAt).getTime();

    if (Number.isNaN(expiryTime)) {
        return 0;
    }

    return Math.max(0, Math.ceil((expiryTime - getServerNowMs()) / 1000));
}

export function formatCountdown(seconds) {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;

    return `${String(minutes).padStart(2, "0")}:${String(
        remainingSeconds
    ).padStart(2, "0")}`;
}

export function getDisplayStatus(application) {
    if (
        application.status === "HELD" &&
        getRemainingSeconds(application.expiresAt) <= 0
    ) {
        return "EXPIRED";
    }

    return application.status;
}

export function sumClassValues(classes, key) {
    return classes.reduce(
        (sum, schoolClass) => sum + (Number(schoolClass[key]) || 0),
        0
    );
}
