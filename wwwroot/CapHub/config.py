from pydantic_settings import BaseSettings, SettingsConfigDict 

class Settings(BaseSettings): 
    APP_HOST: str = "127.0.0.1"
    APP_PORT: int = 8000       
    APP_RELOAD: bool = True    
    ENV: str = "dev"           
    DATABASE_URL: str

    model_config = SettingsConfigDict( 
        env_file=".env",               
        env_file_encoding="utf-8",     
        case_sensitive=False           
    )

settings = Settings()     