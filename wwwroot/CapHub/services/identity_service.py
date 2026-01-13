class IdentityService:
    """خدمة إدارة المستخدمين وربط الهويات الخارجية"""
    def upsert_user_from_claims(self, claims: dict):
        """إدراج أو تحديث مستخدم بناءً على claims من Google."""
        pass

    def upsert_auth_identity(self, user, provider, provider_user_id, email_at_provider, is_primary=False):
        """إدراج أو تحديث هوية مزود خارجي للمستخدم."""
        pass

    def update_provider_tokens(self, identity, access_token=None, refresh_token=None, expires_at=None):
        """تحديث التوكنات الخاصة بالهوية الخارجية."""
        pass
