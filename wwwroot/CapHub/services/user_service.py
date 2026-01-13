class UserService:
    """خدمة المستخدم (بياناتي وتحديث بياناتي)"""
    def get_me(self, user_id):
        """جلب بيانات المستخدم الحالي."""
        pass

    def update_me(self, user_id, payload):
        """تحديث بيانات المستخدم الحالي."""
        pass
from sqlalchemy.orm import Session
from infrastructure.repositories.user import UserRepository
from domain.models.schemas.user import UserCreate
from domain.models.entities.user import User

class UserService:
    def __init__(self, db: Session):
        self.repo = UserRepository(db)

    def create_user(self, user_in: UserCreate) -> User:
        user_dict = user_in.dict()
        return self.repo.create(user_dict)

    def get_user(self, user_id: int) -> User:
        return self.repo.get(user_id)

    def get_user_by_email(self, email: str) -> User:
        return self.repo.get_by_email(email)

    def get_users(self):
        return self.repo.get_all()

    def update_user(self, user_id: int, user_in: dict) -> User:
        user = self.repo.get(user_id)
        if user:
            return self.repo.update(user, user_in)
        return None

    def delete_user(self, user_id: int):
        self.repo.delete(user_id)
