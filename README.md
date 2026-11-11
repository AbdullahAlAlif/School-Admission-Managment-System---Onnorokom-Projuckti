# School Admission System

A full-stack online school admission management system built with ASP.NET Core, React, PostgreSQL and Docker.

## Tech Stack

- **Backend:** C# / ASP.NET Core 10 Web API
- **Frontend:** React + Vite
- **Database:** PostgreSQL 17
- **ORM:** Entity Framework Core
- **Web Server:** Nginx
- **Deployment:** Docker Compose

## Features

- Create and manage admission classes and shifts.
- View total, held, confirmed and available seats.
- Apply for admission with a 17-digit birth registration number.
- Temporarily reserve seats with automatic expiry.
- Confirm or cancel admission applications.
- View personal application history.
- Prevent duplicate applications.
- Protect seat allocation using PostgreSQL transactions and row-level locking.
- Automatically refresh seat availability.
- Preserve application data across container restarts.

## Getting Started



Start all services:

```bash
docker compose up --build
```

Docker Compose starts PostgreSQL, the ASP.NET Core backend and the React frontend. Database migrations are applied automatically when the backend starts.

### Application URLs

| Service | URL |
|---|---|
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:8080 |
| PostgreSQL | localhost:5432 |

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/classes` | Create a class |
| GET | `/classes` | List all classes |
| GET | `/classes/{classId}` | Get class details |
| POST | `/classes/{classId}/holds` | Apply for admission |
| GET | `/holds/{holdId}` | Get application status |
| POST | `/holds/{holdId}/confirm` | Confirm admission |
| DELETE | `/holds/{holdId}` | Cancel application |
| GET | `/me/holds` | Get student applications |

Student-specific endpoints require the `X-Student-Id` header containing a 17-digit birth registration number.

## Environment Variables

| Variable | Default | Description |
|---|---|---|
| `ConnectionStrings__Default` | Docker PostgreSQL connection | Database connection |
| `HOLD_DURATION_SECONDS` | `600` | Temporary seat reservation duration |
| `VITE_API_URL` | `http://localhost:8080` | Frontend API URL (build-time) |

## Database Persistence

PostgreSQL data is stored in a named Docker volume.

Stop the application without deleting database data:

```bash
docker compose down
```

Restart:

```bash
docker compose up -d
```

To remove containers and database volume:

```bash
docker compose down -v
```

**Warning:** The last command permanently deletes the stored database data.

## Project Structure

```text
school-admission-system/
├── backend/
│   ├── Dockerfile
│   └── SchoolAdmission.Api/
├── frontend/
│   ├── Dockerfile
│   ├── nginx.conf
│   └── src/
├── docker-compose.yml
├── .env.example
├── .gitignore
└── README.md
```



## License

Developed as a take-home software engineering assignment for Onnorokom Projukti Limited.