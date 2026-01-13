import secrets
import base64
import hashlib
import time

_STATE_STORE = {} # Temporary in-memory store for states
_PKCE_STORE = {} # Temporary in-memory store for PKCE

class SecurityUtils:
    """خدمات مساعدة للأمان (state, PKCE, hashing)"""
    def generate_state(self) -> str:
        return secrets.token_urlsafe(32) # Random State

    def sign_state(self, state: str) -> str:
        return state  # can add real signing later

    def store_state(self, state: str, ttl: int = 300):
        _STATE_STORE[state] = time.time() + ttl

    def validate_state(self, state: str) -> bool:
        exp = _STATE_STORE.get(state)
        if exp and exp > time.time():
            return True
        return False

    def generate_pkce_pair(self) -> tuple:
        """Generate code_verifier - code_challenge (PKCE)."""
        code_verifier = base64.urlsafe_b64encode(secrets.token_bytes(32)).rstrip(b'=').decode('utf-8')
        code_challenge = base64.urlsafe_b64encode(hashlib.sha256(code_verifier.encode()).digest()).rstrip(b'=').decode('utf-8')
        return code_verifier, code_challenge

    def store_pkce(self, state: str, code_verifier: str):
        _PKCE_STORE[state] = code_verifier

    def pop_pkce(self, state: str) -> str:
        """الحصول على code_verifier لمسار callback وحذفه."""
        return _PKCE_STORE.pop(state, None)
