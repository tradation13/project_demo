import os
from urllib.parse import urlencode
import requests
import jwt
from jwt import PyJWKClient

class OAuthService:
    """خدمة مصادقة Google (OAuth 2.0)"""
    def build_authorization_url(self, state: str, code_challenge: str) -> str:
        client_id = os.getenv("GOOGLE_CLIENT_ID")
        redirect_uri = os.getenv("GOOGLE_REDIRECT_URI")
        scope = "openid email profile"
        params = {
            "client_id": client_id,
            "redirect_uri": redirect_uri,
            "response_type": "code",
            "scope": scope,
            "state": state,
            "code_challenge": code_challenge,
            "code_challenge_method": "S256",
            "access_type": "offline",
            "prompt": "consent"
        }
        return f"https://accounts.google.com/o/oauth2/v2/auth?{urlencode(params)}"

    def exchange_code_for_tokens(self, code: str, code_verifier: str) -> dict:
        """تبادل الكود مع Google للحصول على التوكنات."""
        token_url = "https://oauth2.googleapis.com/token"
        client_id = os.getenv("GOOGLE_CLIENT_ID")
        client_secret = os.getenv("GOOGLE_CLIENT_SECRET")
        redirect_uri = os.getenv("GOOGLE_REDIRECT_URI")
        data = {
            "client_id": client_id,
            "client_secret": client_secret,
            "code": code,
            "code_verifier": code_verifier,
            "redirect_uri": redirect_uri,
            "grant_type": "authorization_code"
        }
        response = requests.post(token_url, data=data)
        if response.status_code != 200:
            raise Exception(f"Google token exchange failed: {response.text}")
        return response.json()

    def validate_id_token(self, id_token: str) -> dict:
        """التحقق من id_token واستخراج claims."""
        jwks_url = "https://www.googleapis.com/oauth2/v3/certs"
        jwks_client = PyJWKClient(jwks_url)
        signing_key = jwks_client.get_signing_key_from_jwt(id_token)
        client_id = os.getenv("GOOGLE_CLIENT_ID")
        claims = jwt.decode(
            id_token,
            signing_key.key,
            algorithms=["RS256"],
            audience=client_id,
            issuer=["https://accounts.google.com", "accounts.google.com"],
        )
        return claims

    def fetch_userinfo(self, access_token: str) -> dict:
        """جلب بيانات المستخدم من Google (اختياري)."""
        userinfo_url = "https://openidconnect.googleapis.com/v1/userinfo"
        headers = {"Authorization": f"Bearer {access_token}"}
        response = requests.get(userinfo_url, headers=headers)
        if response.status_code != 200:
            raise Exception(f"Google userinfo fetch failed: {response.text}")
        return response.json()
