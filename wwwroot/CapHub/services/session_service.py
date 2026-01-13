class SessionService:
    """خدمة إدارة الجلسات (كوكي أو JWT)"""
    def create_session(self, user_id):
        """إنشاء جلسة وتوليد session_id."""
        pass

    def set_session_cookie(self, response, session_id):
        """تعيين كوكي الجلسة في الاستجابة."""
        pass

    def get_user_from_session(self, request):
        """جلب المستخدم من الجلسة الحالية."""
        pass

    def destroy_session(self, session_id):
        """إنهاء الجلسة وحذفها."""
        pass

    def issue_access_token(self, user_id, scopes=None):
        """إصدار access_token (JWT)."""
        pass

    def issue_refresh_token(self, user_id):
        """إصدار refresh_token."""
        pass

    def verify_access_token(self, jwt):
        """التحقق من صحة access_token وإرجاع user_id."""
        pass
