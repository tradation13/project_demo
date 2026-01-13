from sqlalchemy.orm import Session
from infrastructure.repositories.user import UserRepository
from domain.models.schemas.user import UserCreate
from domain.models.entities.user import User


class AuthService:
    def __init__(self, db: Session = None):
        self.repo = UserRepository(db) if db else None

    def status(self):
        return {"status": "Authentication service is up and running."}
