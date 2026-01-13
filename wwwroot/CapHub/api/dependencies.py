from services.auth_service import AuthService
from fastapi import Depends

from services.identity_service import IdentityService
from services.oauth_service import OAuthService
from services.session_service import SessionService
from services.user_service import UserService

def get_auth_service():
    return AuthService()

def get_oauth_service():
    return OAuthService()

def get_identity_service():
    return IdentityService()

def get_session_service():
    return SessionService()

def get_user_service():
    return UserService()

