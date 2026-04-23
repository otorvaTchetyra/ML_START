from sqlalchemy import create_engine, Column, Integer, Float, String, DateTime, Text, Boolean
from sqlalchemy.orm import DeclarativeBase, sessionmaker
from datetime import datetime
from config import settings

engine = create_engine(settings.db_url, pool_pre_ping=True)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)


class Base(DeclarativeBase):
    pass


class User(Base):
    __tablename__ = "users"

    id = Column(Integer, primary_key=True, index=True)
    username = Column(String(64), unique=True, nullable=False)
    hashed_password = Column(String(256), nullable=False)
    role = Column(String(16), nullable=False, default="operator")
    is_active = Column(Boolean, default=True)


class FeedingEvent(Base):
    __tablename__ = "feeding_events"

    id = Column(Integer, primary_key=True, index=True)
    timestamp = Column(DateTime, nullable=False, default=datetime.utcnow)
    granule_count = Column(Integer, nullable=False)
    intensity_per_sec = Column(Float, nullable=False)
    intensity_per_min = Column(Float, nullable=False)
    is_out_of_schedule = Column(Boolean, default=False)
    threshold_exceeded = Column(Boolean, default=False)
    comment = Column(Text, nullable=True)


class AppLog(Base):
    __tablename__ = "app_logs"

    id = Column(Integer, primary_key=True, index=True)
    timestamp = Column(DateTime, nullable=False, default=datetime.utcnow)
    level = Column(String(16), nullable=False)
    message = Column(Text, nullable=False)


class AppSettings(Base):
    __tablename__ = "app_settings"

    id = Column(Integer, primary_key=True, index=True)
    key = Column(String(64), unique=True, nullable=False)
    value = Column(Text, nullable=False)


def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


def init_db():
    Base.metadata.create_all(bind=engine)
