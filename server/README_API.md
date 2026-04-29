# Отчёт по задачам — Ковпак Олеся

## Задача 1: REST API сервер на Python (FastAPI)
**Срок:** 16.04 – 21.04
**Статус:** Выполнено

### Что реализовано

FastAPI-приложение с JWT-авторизацией, ролями admin/operator, MySQL через SQLAlchemy и автоматической инициализацией схемы БД при старте.

| Группа | Эндпоинты | Назначение |
|--------|-----------|------------|
| Auth | `POST /auth/login`, `GET /auth/me`, `GET/POST/DELETE /auth/users` | Вход по логину/паролю, выдача JWT, управление пользователями (только admin) |
| Stream | `POST /stream/upload`, `POST /stream/start`, `POST /stream/stop`, `GET /stream/status` | Загрузка MP4/AVI или подключение к камере (RTSP/HTTP/индекс), управление анализом |
| Events | `GET /events`, `PATCH /events/{id}/comment` | Журнал кормлений с фильтрами по дате и типу события, добавление комментариев |
| Stats | `GET /stats` | Статистика за период с разбивкой по интервалам (`bucket=hour\|day\|minute\|<сек>`) |
| Settings | `GET /settings`, `PATCH /settings` | Конфигуратор: порог гранул, расписание, параметры модели; настройки сохраняются между сессиями |
| Logs | `GET /logs` | Журнал ошибок отдельно от событий кормления |
| Health | `GET /health` | Проверка живости сервиса |

Дополнительно:
- Защита от двойного запуска анализа (`409 Conflict` при `is_running=True`)
- Чанковая загрузка видео в tmp-файл (не блокирует event loop на больших файлах)
- Покадровая обработка ошибок: некритический сбой (потеря кадра, таймаут) логируется в `app_logs` и не валит обработку
- bcrypt-хэширование паролей, JWT с настраиваемым TTL
- При первом старте автоматически создаётся `admin / admin`

### Файлы

| Файл | Описание |
|------|----------|
| `main.py` | Точка входа, lifespan, регистрация роутеров, CORS |
| `config.py` | Настройки через `.env` (Pydantic Settings) |
| `database.py` | Модели SQLAlchemy (`User`, `FeedingEvent`, `AppLog`, `AppSettings`), engine |
| `api/auth.py` | Авторизация, JWT, RBAC |
| `api/stream.py` | Управление видеопотоком (file + camera) |
| `api/events.py` | Журнал событий |
| `api/stats.py` | Статистика и агрегация по интервалам |
| `api/settings_api.py` | Конфигуратор |
| `api/logs.py` | Журнал ошибок |
| `schemas/schemas.py` | Pydantic-схемы запросов и ответов |

---

## Задача 2: Интеграция модели с сервером и тестирование инференса
**Срок:** 22.04 – 25.04
**Ответственные:** Ковпак Олеся, Tchetyrkin Daniil
**Статус:** Выполнено

### Как стыкуется с моделью

`api/stream.py` — единая точка интеграции:

1. Клиент авторизуется (`/auth/login`) и получает JWT.
2. Запускает анализ:
   - `POST /stream/upload` (multipart с MP4/AVI), либо
   - `POST /stream/start` с телом `{"source": "rtsp://..." | "http://..." | "0"}` для камеры.
3. Сервер открывает источник через OpenCV, читает кадры в цикле, для каждого кадра:
   - вызывает `GranuleDetector.detect(frame)` → список `Detection`
   - вызывает `GranuleCounter.process_frame(detections, ts)` → `granule_count`, `intensity_per_sec`, `intensity_per_min`
   - сравнивает с настройками (порог гранул, расписание кормления)
   - при превышении порога или кормлении вне расписания пишет `FeedingEvent` в БД
   - отдаёт результат клиенту строкой NDJSON
4. Клиент в реальном времени читает поток `application/x-ndjson`, рисует bbox поверх видео, обновляет счётчик и индикатор активности.
5. `POST /stream/stop` снимает флаг `is_running`, цикл штатно завершается, временный файл удаляется.

Параметры детектора и счётчика (`confidence`, `iou`, `intensity_window_sec`) меняются на лету через `PATCH /settings` без перезапуска сервера.

### Формат NDJSON-кадра

```json
{"frame_index":42,"timestamp":1714.23,"granule_count":7,
 "intensity_per_sec":3.4,"intensity_per_min":204.0,
 "threshold_exceeded":false,"out_of_schedule":false,
 "bboxes":[{"x1":120.0,"y1":80.5,"x2":150.0,"y2":110.0,"confidence":0.87}]}
```

---

## Тесты

`tests/test_api.py` — интеграционные тесты на `TestClient` с SQLite на tmp-файле.

| Тест | Что проверяет |
|------|---------------|
| `test_health` | `GET /health` доступен без авторизации |
| `test_login_success_and_failure` | Корректные/неверные креды |
| `test_me_requires_token` | `/auth/me` без токена — 401, с токеном возвращает пользователя |
| `test_user_management_admin_only` | Создание пользователя оператором запрещено (403) |
| `test_cannot_delete_self` | Админ не может удалить сам себя (400) |
| `test_settings_get_and_patch` | Настройки читаются и сохраняются между запросами |
| `test_events_filter_and_comment` | Фильтр по `threshold_exceeded`, добавление комментария |
| `test_stats_with_buckets` | Агрегация по часовым корзинам, валидация невалидного `bucket` |
| `test_stats_invalid_range` | `date_to <= date_from` отклоняется |
| `test_logs_filter` | Фильтр по уровню, отклонение неизвестного уровня |
| `test_stream_upload_rejects_bad_extension` | Отклонение всего, кроме MP4/AVI |
| `test_stream_requires_auth` | Эндпоинты стрима требуют JWT |
| `test_stream_409_when_running` | Повторный старт во время работы возвращает 409 |
| `test_stream_status_reports_running_flag` | `/stream/status` отдаёт корректное состояние |

Результат: **14/14 тестов API + 10/10 counter = 24/24 пройдено**.

```
pytest tests/ -v
```

## Запуск

```bash
cd server
pip install -r requirements.txt
# .env: DB_URL=mysql+pymysql://... SECRET_KEY=...
# weights/best.pt — обученная модель
uvicorn main:app --host 0.0.0.0 --port 8000
```

Swagger UI — `http://localhost:8000/docs`.
