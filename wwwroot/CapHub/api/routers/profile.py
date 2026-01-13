from fastapi import APIRouter, Depends, Request, HTTPException

from api.dependencies import get_user_service

router = APIRouter(prefix="/profile", tags=["Profile"])

@router.get("/me")
def get_me(request: Request, UserService=Depends(get_user_service)):
    """
    جلب بيانات المستخدم الحالي (يتطلب تحقق الجلسة/التوكن)
    """
    # منطق الخدمة هنا
    pass
