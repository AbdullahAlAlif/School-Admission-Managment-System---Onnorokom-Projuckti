
const API_BASE_URL = (
    import.meta.env.VITE_API_URL || "http://localhost:8080"
).replace(/\/+$/, "");

// Difference between the backend clock and browser clock.
let serverOffsetMs = 0;

export function getServerNowMs() {
    return Date.now() + serverOffsetMs;
}



// GENERIC HTTP REQUEST
async function request(path, options = {}) {
    const headers = {
        ...(options.headers || {})
    };

    if (options.body && !headers["Content-Type"]) {
        headers["Content-Type"] = "application/json";
    }

    let response;

    try {
        response = await fetch(`${API_BASE_URL}${path}`, {
            ...options,
            headers,
            cache: "no-store"
        });
    } catch {
        throw new Error(
            "Cannot connect to the admission server. Please check whether the backend is running."
        );
    }

    // Synchronize browser time with the backend HTTP Date header.
    const serverDate = response.headers.get("Date");

    if (serverDate) {
        const parsedDate = Date.parse(serverDate);

        if (!Number.isNaN(parsedDate)) {
            serverOffsetMs = parsedDate - Date.now();
        }
    }

    // HTTP 204 has no response body.
    if (response.status === 204) {
        return null;
    }

    const contentType = response.headers.get("content-type") || "";

    
    let data = null;

    if (contentType.includes("application/json")) {
        data = await response.json();
    } else {
        const text = await response.text();
        data = text ? { message: text } : null;
    }

    // Standard API error handling.
    if (!response.ok) {
        const message =
            data?.error?.message ||
            data?.message ||
            `Request failed with HTTP ${response.status}.`;

        const error = new Error(message);

        error.status = response.status;
        error.code = data?.error?.code || "REQUEST_FAILED";

        throw error;
    }

    return data;
}



// STUDENT HEADER
function studentHeaders(studentId) {
    return {
        "X-Student-Id": studentId
    };
}



// 1. POST /classes
export function createClass(classData) {
    return request("/classes", {
        method: "POST",
        body: JSON.stringify(classData)
    });
}



// 2. GET /classes
export function getClasses() {
    return request("/classes");
}



// 3. GET /classes/{classId}
export function getClass(classId) {
    return request(
        `/classes/${encodeURIComponent(classId)}`
    );
}



// 4. POST /classes/{classId}/holds - APPLY FOR ADMISSION
export function createHold(classId, studentId) {
    return request(
        `/classes/${encodeURIComponent(classId)}/holds`,
        {
            method: "POST",
            headers: studentHeaders(studentId)
        }
    );
}



// 5. GET /holds/{holdId} - CHECK APPLICATION STATUS
export function getHold(holdId) {
    return request(
        `/holds/${encodeURIComponent(holdId)}`
    );
}



// 6. POST /holds/{holdId}/confirm
export function confirmHold(holdId, studentId) {
    return request(
        `/holds/${encodeURIComponent(holdId)}/confirm`,
        {
            method: "POST",
            headers: studentHeaders(studentId)
        }
    );
}



// 7. DELETE /holds/{holdId} -- CANCEL APPLICATION
export function cancelHold(holdId, studentId) {
    return request(
        `/holds/${encodeURIComponent(holdId)}`,
        {
            method: "DELETE",
            headers: studentHeaders(studentId)
        }
    );
}



// 8. GET /me/holds  - GET STUDENT APPLICATIONS

export function getMyHolds(studentId) {
    return request("/me/holds", {
        method: "GET",
        headers: studentHeaders(studentId)
    });
}
