# ASSUMPTIONS.md

Bellow is what I assumed in each case and why. 

## General

1. `X-Student-Id` must be exactly 17 digits (`^\d{17}$`).
2. Anyone using the UI can create a class (no admin login, as the brief says no login). The brief lists no edit or delete endpoint, so classes cannot be edited or deleted.
3. Each shift is treated as a separate class.
4. A seat is occupied by a hold with status `HELD` whose `expiresAt` is still in the future, or by a `CONFIRMED` hold. `EXPIRED` and `CANCELLED` holds occupy nothing.
5. `availableSeats = totalSeats - heldSeats - confirmedSeats`, and it is never negative.
6. A student may have at most one occupying hold (`HELD` or `CONFIRMED`) in the same class. Re-applying after a hold expires or is cancelled is allowed, as that is the natural expected behaviour.
7. A student may hold seats in different classes at the same time. `GET /me/holds` returns all applications of that student.
8. `expiresAt = created time + HOLD_DURATION_SECONDS`.

## Status transitions

A hold has four statuses: `HELD`, `CONFIRMED`, `EXPIRED`, `CANCELLED`.

```
HELD ──confirm──▶ CONFIRMED
HELD ──cancel───▶ CANCELLED ──▶ seat becomes available again for everyone
HELD ──time─────▶ EXPIRED   ──▶ seat becomes available again for everyone
                              (UI shows a link back to the application page)
```

## Concurrency and race condition mitigation

9. Every operation (hold, confirm, cancel) runs in one database transaction that first locks the class row (`SELECT ... FOR UPDATE`). The lock lives in PostgreSQL, not in application memory, so it also works with two backend processes sharing one database. Inside the lock, the code counts occupied seats, checks the duplicate rule, and inserts or updates, so two processes cannot take the same last seat.
10. Requests for the same class are serialized, so `held + confirmed` can never exceed `totalSeats`. Requests for different classes do not block each other.

## Payment

11. Pressing "Pay admission fee" calls confirm and is treated as a successful payment, and the hold becomes `CONFIRMED`. No payment record, amount, or receipt is stored. The brief says no real payment gateway is needed.