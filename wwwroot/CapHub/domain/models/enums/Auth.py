import enum


class Provider(str, enum.Enum):
    google = "google"
    linkedin = "linkedin"
    x = "x"
    phone = "phone"
    email_password = "email_password"