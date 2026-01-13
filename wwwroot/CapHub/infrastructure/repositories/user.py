from sqlalchemy.orm import Session
from domain.models.entities.user import User
from .base import BaseRepository

class UserRepository(BaseRepository[User]):
    def __init__(self, db: Session):
        super().__init__(db, User)

    def get_by_email(self, email: str):
        return self.db.query(self.model).filter(self.model.primary_email == email).first()
