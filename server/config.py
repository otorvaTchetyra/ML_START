from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    db_url: str = "mysql+pymysql://root:password@localhost:3306/dronevision"
    secret_key: str = "change-me-in-production"
    algorithm: str = "HS256"
    token_expire_minutes: int = 480

    model_path: str = "models/weights.pt"
    fish_model_path: str = "models/fish_weights.pt"
    model_confidence: float = 0.5
    model_iou: float = 0.4

    intensity_window_sec: float = 5.0
    default_granule_threshold: int = 50
    frame_skip: int = 1
    tracker_iou_threshold: float = 0.3

    class Config:
        env_file = ".env"


settings = Settings()
