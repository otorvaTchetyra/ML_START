# DroneVision — Fish Feeding Monitoring System

> An automated computer vision system for real-time fish feeding monitoring. Analyzes live video streams, detects and counts feed granules frame by frame, and alerts operators about abnormal feeding events.

![Python](https://img.shields.io/badge/Python-3.10+-3776AB?logo=python&logoColor=white)
![FastAPI](https://img.shields.io/badge/FastAPI-0.115-009688?logo=fastapi&logoColor=white)
![YOLOv8](https://img.shields.io/badge/YOLOv8-ultralytics-00FFFF?logo=yolo&logoColor=black)
![C#](https://img.shields.io/badge/C%23-.NET_10-239120?logo=csharp&logoColor=white)
![Avalonia](https://img.shields.io/badge/Avalonia_UI-11.3-8B5CF6)

---

## Live Demo

🌐 **API (Swagger UI):** [http://185.125.102.24:8000/docs](http://185.125.102.24:8000/docs)

---

## How It Works

```
Camera / Video file
        │
        ▼
┌───────────────────┐
│  GranuleDetector  │  YOLOv8 — detects granules per frame
│  (YOLOv8)        │  Bounding box size filter (< 12% of frame)
└────────┬──────────┘
         │
         ▼
┌───────────────────┐
│  GranuleTracker   │  IoU-based tracker — prevents counting
│                   │  the same granule across multiple frames
└────────┬──────────┘
         │  new granules only
         ▼
┌───────────────────┐
│  GranuleCounter   │  Sliding window (5 sec) →
│                   │  intensity (granules/sec, granules/min)
└────────┬──────────┘
         │
         ▼
  FastAPI (NDJSON stream)
         │
         ▼
  Avalonia Desktop Client
  - MJPEG stream with detection overlay
  - Threshold alerts with sound
  - Feeding event journal
```

---

## Tech Stack

### Server (Python)
| Component | Technology |
|-----------|-----------|
| REST API & streaming | FastAPI, uvicorn |
| Object detection | YOLOv8 (ultralytics 8.3) |
| Inter-frame tracking | Custom IoU tracker |
| Database | SQLAlchemy 2.0 + MySQL |
| Authentication | JWT (python-jose) |
| Rate limiting | slowapi |
| Video processing | OpenCV (headless) |
| Stream URL resolving | yt-dlp (YouTube, HLS) |

### Client (C#)
| Component | Technology |
|-----------|-----------|
| UI framework | Avalonia UI 11.3 |
| Pattern | MVVM (ReactiveUI, CommunityToolkit) |
| Video playback | LibVLC (software rendering) |
| Charts | LiveChartsCore |
| Local database | Entity Framework + SQLite |

---

## Features

- **Real-time granule detection** using a custom-trained YOLOv8 model
- **IoU tracker** — each granule is counted exactly once, even if visible across multiple frames
- **Feeding intensity** via sliding window: granules/sec and granules/min
- **Feeding schedule** — detects feeding events outside defined time windows
- **Alerts** — visual banner and audio signal when granule threshold is exceeded
- **Live MJPEG stream** with detection overlay rendered server-side
- **Multiple video sources:** MP4/AVI files, webcam, YouTube, HLS streams, sochi.camera
- **Role-based access:** administrator (full access) and operator (view & configure)
- **Event journal** with comments and period statistics

---

## Project Structure

```
ML_START/
├── server/                  — Python ML server
│   ├── main.py              — entry point, FastAPI app
│   ├── config.py            — settings via .env
│   ├── database.py          — DB models
│   ├── models/
│   │   ├── detector.py      — YOLOv8 wrapper
│   │   ├── tracker.py       — IoU tracker
│   │   └── counter.py       — sliding window intensity counter
│   ├── api/
│   │   ├── auth.py          — JWT auth, user management
│   │   ├── stream.py        — NDJSON streaming, MJPEG, video analysis
│   │   ├── events.py        — feeding event journal
│   │   ├── stats.py         — period statistics
│   │   └── settings_api.py  — system configuration
│   ├── schemas/schemas.py   — Pydantic schemas
│   └── tests/               — pytest (32 tests)
│
└── Client/                  — C# Avalonia desktop app
    └── Client/
        ├── ViewModels/      — MVVM logic
        ├── Views/           — AXAML UI
        ├── Services/        — API client, streaming, auth
        └── Models/          — data models
```

---

## Getting Started

### Server

```bash
cd server
pip install -r requirements.txt
```

Create `.env`:
```env
DB_URL=mysql+pymysql://user:password@localhost:3306/dronevision
SECRET_KEY=your-secret-key
```

Place model weights at `server/models/weights.pt`, then:

```bash
uvicorn main:app --host 0.0.0.0 --port 8000
```

A default admin account is created automatically: `admin / admin`.  
Swagger UI available at: [http://localhost:8000/docs](http://localhost:8000/docs)

### Client

Requires .NET 10 SDK and libvlc:
```bash
sudo apt install dotnet-sdk-10.0 libvlc-dev  # Ubuntu
```

```bash
cd Client
dotnet run --project Client/Client.csproj
```

Server address is configured in `Client/appsettings.json`.

---

## API Reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/auth/login` | Get JWT token |
| GET | `/auth/users` | List users (admin) |
| POST | `/auth/users` | Create user (admin) |
| PUT | `/auth/users/{id}` | Update user (admin) |
| POST | `/stream/start` | Start analysis from URL |
| POST | `/stream/upload` | Upload MP4/AVI and start analysis |
| GET | `/stream/mjpeg` | MJPEG stream with detection overlay |
| POST | `/stream/stop` | Stop analysis |
| GET | `/events` | Feeding event journal |
| GET | `/stats` | Statistics for a time period |
| GET/PATCH | `/settings` | System settings |

---

## Tests

```bash
cd server
pytest tests/ -v
```

32 tests covering the counter algorithm, tracker, and API endpoints.

---

## Team

| Member | Role |
|--------|------|
| Susorov Egor | ML — dataset collection, YOLOv8 training |
| Tchetyrkin Daniil | ML — counting algorithm, tracking, model integration |
| Kovpak Olesya | ML — REST API server |
| Nuriev Royal | Client — database, business logic |
| Kaznin Alexander | Client — UI, Avalonia |
