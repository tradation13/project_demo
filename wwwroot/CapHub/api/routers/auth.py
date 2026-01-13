from fastapi import APIRouter, Depends, Request, Response, HTTPException
from fastapi import APIRouter, Depends
from services.auth_service import AuthService
from api.dependencies import get_auth_service, get_identity_service, get_oauth_service, get_session_service, get_user_service
from services.oauth_service import OAuthService
from services.identity_service import IdentityService
from services.session_service import SessionService
from Utils.security_utils import SecurityUtils
from services.user_service import UserService
router = APIRouter(prefix="/auth", tags=["Authentication"])


router = APIRouter(
    prefix="/auth",
    tags=["Auhthentication"]
)

security_utils = SecurityUtils()
# Google OAuth Endpoints
@router.get("/google/login")
def google_login(
    oauth_service: OAuthService = Depends(get_oauth_service)
):
    print("google_login called")
    #Create a new State
    state = security_utils.generate_state()
    # 2. Generate PKCE (code_verifier, code_challenge)
    code_verifier, code_challenge = security_utils.generate_pkce_pair()
    # 3. store state and pkce temporarily (in-memory, cache, db, etc.)
    security_utils.store_state(state, ttl=300)
    security_utils.store_pkce(state, code_verifier)
    # 4.Google 0Auth authorization URL
    authorization_url = oauth_service.build_authorization_url(state, code_challenge)
    # 5. redirect user to Google
    return {"authorization_url": authorization_url}

@router.get("/google/callback")
def google_callback(
    request: Request,
    response: Response,
    oauth_service: OAuthService = Depends(get_oauth_service),
    identity_service: IdentityService = Depends(get_identity_service),
    session_service: SessionService = Depends(get_session_service),
    user_service: UserService = Depends(get_user_service)
):
    """
    استقبال الكود من Google: التحقق من state، تبادل الكود، التحقق من id_token، upsert مستخدم، إنشاء جلسة، إعادة توجيه.
    """
    
    pass

@router.post("/google/logout")
def logout(
    request: Request,
    response: Response,
    session_service: SessionService = Depends(get_session_service)
):
    """
    إنهاء الجلسة (مسح الكوكي/التوكن، الرد 204)
    """
    # منطق الخدمة هنا
    pass

@router.post("/google/refresh")
def refresh_token(
    request: Request,
    response: Response,
    session_service: SessionService = Depends(get_session_service)
):
    """
    تجديد التوكن (التحقق من refresh، إصدار access جديد)
    """
    # منطق الخدمة هنا
    pass

@router.get("/status")
async def auth_status(auth_service: AuthService = Depends(get_auth_service)):
    return auth_service.status()