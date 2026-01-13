from fastapi import FastAPI
import uvicorn
import os
from config import settings
from api.routers import auth_router
from api.routers.profile import router as profile_router

app = FastAPI()



# Include Routers
app.include_router(auth_router)
app.include_router(profile_router)


@app.get("/")
async def root():
	return {"message": "Hello, World!"}

if __name__ == "__main__":
	uvicorn.run("main:app", host=settings.APP_HOST, port=settings.APP_PORT, reload=True)
