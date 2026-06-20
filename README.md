# DroneVision — Система контроля кормления рыб

> Система автоматического мониторинга кормления рыб на основе компьютерного зрения. Анализирует видеопоток с камеры в реальном времени, детектирует и подсчитывает гранулы корма, уведомляет оператора о нештатных ситуациях.

![Python](https://img.shields.io/badge/Python-3.10+-3776AB?logo=python&logoColor=white)
![FastAPI](https://img.shields.io/badge/FastAPI-0.115-009688?logo=fastapi&logoColor=white)
![YOLOv8](https://img.shields.io/badge/YOLOv8-ultralytics-00FFFF?logo=yolo&logoColor=black)
![C#](https://img.shields.io/badge/C%23-.NET_10-239120?logo=csharp&logoColor=white)
![Avalonia](https://img.shields.io/badge/Avalonia_UI-11.3-8B5CF6)

---

## Демо

🌐 **API (Swagger UI):** [http://185.125.102.24:8000/docs](http://185.125.102.24:8000/docs)

---

## Как это работает

```
Камера / видеофайл
        │
        ▼
┌───────────────────┐
│   GranuleDetector  │  YOLOv8 — детекция гранул на кадре
│   (YOLOv8)        │  Фильтр по размеру бокса (< 12% кадра)
└────────┬──────────┘
         │
         ▼
┌───────────────────┐
│   GranuleTracker   │  IoU-трекер: исключает повторный счёт
│                   │  одной гранулы в нескольких кадрах
└────────┬──────────┘
         │  только новые гранулы
         ▼
┌───────────────────┐
│   GranuleCounter   │  Скользящее окно 5 сек →
│                   │  интенсивность (гранул/сек, гранул/мин)
└────────┬──────────┘
         │
         ▼
   FastAPI (NDJSON stream)
         │
         ▼
   Avalonia Desktop Client
   - MJPEG видео с оверлеем
   - Алерты при превышении порога
   - Журнал событий
```

---

## Стек технологий

### Сервер (Python)
| Компонент | Технология |
|-----------|-----------|
| REST API + стриминг | FastAPI, uvicorn |
| Детекция объектов | YOLOv8 (ultralytics 8.3) |
| Трекинг между кадрами | Собственный IoU-трекер |
| БД | SQLAlchemy 2.0 + MySQL |
| Аутентификация | JWT (python-jose) |
| Rate limiting | slowapi |
| Видео | OpenCV (headless) |
| Стриминг URL | yt-dlp (YouTube, HLS) |

### Клиент (C#)
| Компонент | Технология |
|-----------|-----------|
| UI фреймворк | Avalonia UI 11.3 |
| Паттерн | MVVM (ReactiveUI, CommunityToolkit) |
| Видео | LibVLC (software rendering) |
| Графики | LiveChartsCore |
| Локальная БД | Entity Framework + SQLite |

---

## Ключевые возможности

- **Детекция гранул** через кастомно обученную YOLOv8 в реальном времени
- **Трекинг** — каждая гранула считается ровно один раз, даже если видна в нескольких кадрах
- **Интенсивность** по скользящему окну: гранул/сек и гранул/мин
- **Расписание кормления** — система фиксирует кормление вне заданных промежутков
- **Алерты** — визуальный баннер и звуковой сигнал при превышении порога
- **MJPEG-стрим** с оверлеем детекций в реальном времени
- **Поддержка источников:** MP4/AVI файл, веб-камера, YouTube, HLS-поток, sochi.camera
- **Роли:** администратор (полный доступ) и оператор (просмотр и настройка)
- **Журнал событий** с комментариями и статистикой за период

---

## Архитектура проекта

```
ML_START/
├── server/                  — Python ML-сервер
│   ├── main.py              — точка входа, FastAPI app
│   ├── config.py            — настройки через .env
│   ├── database.py          — модели БД
│   ├── models/
│   │   ├── detector.py      — обёртка YOLOv8
│   │   ├── tracker.py       — IoU-трекер между кадрами
│   │   └── counter.py       — скользящее окно интенсивности
│   ├── api/
│   │   ├── auth.py          — JWT-авторизация, управление пользователями
│   │   ├── stream.py        — NDJSON-стриминг, MJPEG, анализ видео
│   │   ├── events.py        — журнал событий кормления
│   │   ├── stats.py         — статистика за период
│   │   └── settings_api.py  — настройки системы
│   ├── schemas/schemas.py   — Pydantic-схемы
│   └── tests/               — pytest (32 теста)
│
└── Client/                  — C# Avalonia десктоп
    └── Client/
        ├── ViewModels/      — MVVM логика
        ├── Views/           — AXAML интерфейс
        ├── Services/        — API-клиент, стриминг, авторизация
        └── Models/          — модели данных
```

---

## Быстрый старт

### Сервер

```bash
cd server
pip install -r requirements.txt
```

Создать `.env`:
```env
DB_URL=mysql+pymysql://user:password@localhost:3306/dronevision
SECRET_KEY=your-secret-key
```

Положить веса модели в `server/models/weights.pt`, затем:

```bash
uvicorn main:app --host 0.0.0.0 --port 8000
```

После запуска создаётся администратор по умолчанию: `admin / admin`.  
Swagger UI: [http://localhost:8000/docs](http://localhost:8000/docs)

### Клиент

Требуется .NET 10 SDK и libvlc:
```bash
sudo apt install dotnet-sdk-10.0 libvlc-dev  # Ubuntu
```

```bash
cd Client
dotnet run --project Client/Client.csproj
```

Адрес сервера настраивается в `Client/appsettings.json`.

---

## API

| Метод | Endpoint | Описание |
|-------|----------|----------|
| POST | `/auth/login` | JWT-токен |
| GET | `/auth/users` | Список пользователей (admin) |
| POST | `/auth/users` | Создать пользователя (admin) |
| PUT | `/auth/users/{id}` | Редактировать пользователя (admin) |
| POST | `/stream/start` | Запустить анализ потока по URL |
| POST | `/stream/upload` | Загрузить MP4/AVI и запустить анализ |
| GET | `/stream/mjpeg` | MJPEG-поток с оверлеем детекций |
| POST | `/stream/stop` | Остановить анализ |
| GET | `/events` | Журнал событий кормления |
| GET | `/stats` | Статистика за период |
| GET/PATCH | `/settings` | Настройки системы |

---

## Тесты

```bash
cd server
pytest tests/ -v
```

32 теста: алгоритм подсчёта, трекер, API.

---

## Команда

| Участник | Роль |
|----------|------|
| Сусоров Егор | ML — датасет, обучение YOLOv8 |
| Tchetyrkin Daniil | ML — алгоритм подсчёта, трекинг, интеграция модели |
| Ковпак Олеся | ML — REST API сервер |
| Нуриев Роял | Client — БД, бизнес-логика |
| Казнин Александр | Client — UI, Avalonia |
