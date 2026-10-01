import json

from fastapi import FastAPI, WebSocket, HTTPException

from emotion_agent.emotion import analyze_emotion

import httpx
from pydantic import BaseModel

import re

from auth import hash_password, verify_password

from db import get_connection

EMOJI_PATTERN = re.compile(
    "["
    "\U0001F300-\U0001FAFF"
    "\U00002600-\U000027BF"
    "\U0001F1E6-\U0001F1FF"
    "\U00002B00-\U00002BFF"
    "]+",
    flags=re.UNICODE
)

def strip_emoji(text: str) -> str:
    return EMOJI_PATTERN.sub("", text)

# --------------------------------------------------
# FastAPI
# --------------------------------------------------

app = FastAPI()


# --------------------------------------------------
# Basic endpoint
# --------------------------------------------------

@app.get("/")
def root():
    return {
        "status": "ok",
        "message": "AI3DChat backend is running"
    }


# --------------------------------------------------
# Emotion WebSocket
# --------------------------------------------------

class ChatRequest(BaseModel):
    message: str

class RegisterRequest(BaseModel):
    username: str
    email: str
    password: str

class LoginRequest(BaseModel):
    email: str
    password: str


USERNAME_PATTERN = re.compile(r"^[A-Za-z0-9_]{3,30}$")
EMAIL_PATTERN = re.compile(r"^[^@\s]+@[^@\s]+\.[^@\s]+$")

@app.post("/auth/register")
def register(request: RegisterRequest):
    username = request.username.strip()
    email = request.email.strip().lower()
    password = request.password

    # Проверка username
    if not USERNAME_PATTERN.fullmatch(username):
        raise HTTPException(
            status_code=400,
            detail="INVALID_USERNAME"
        )

    # Проверка email
    if not EMAIL_PATTERN.fullmatch(email):
        raise HTTPException(
            status_code=400,
            detail="INVALID_EMAIL"
        )

    # Проверка пароля
    if len(password) < 8:
        raise HTTPException(
            status_code=400,
            detail="PASSWORD_TOO_SHORT"
        )

    with get_connection() as connection:
        with connection.cursor() as cursor:

            cursor.execute(
                """
                SELECT 1
                FROM users
                WHERE username = %s
                """,
                (username,)
            )

            username_exists = cursor.fetchone() is not None

            cursor.execute(
                """
                SELECT 1
                FROM users
                WHERE LOWER(email) = %s
                """,
                (email,)
            )

            email_exists = cursor.fetchone() is not None

            if username_exists and email_exists:
                raise HTTPException(
                    status_code=400,
                    detail="USERNAME_AND_EMAIL_TAKEN"
                )

            if username_exists:
                raise HTTPException(
                    status_code=400,
                    detail="USERNAME_TAKEN"
                )

            if email_exists:
                raise HTTPException(
                    status_code=400,
                    detail="EMAIL_TAKEN"
                )

            password_hash = hash_password(password)

            cursor.execute(
                """
                INSERT INTO users (username, email, password_hash)
                VALUES (%s, %s, %s)
                RETURNING id, username, email
                """,
                (
                    username,
                    email,
                    password_hash
                )
            )

            user = cursor.fetchone()

    return {
        "id": user[0],
        "username": user[1],
        "email": user[2]
    }

@app.post("/auth/login")
def login(request: LoginRequest):

    email = request.email.strip().lower()

    with get_connection() as connection:
        with connection.cursor() as cursor:

            cursor.execute(
                """
                SELECT id, username, email, password_hash
                FROM users
                WHERE email = %s
                """,
                (email,)
            )

            user = cursor.fetchone()

            if user is None:
                raise HTTPException(
                    status_code=401,
                    detail="INVALID_LOGIN"
                )

            password_ok = verify_password(
                request.password,
                user[3]
            )

            if not password_ok:
                raise HTTPException(
                    status_code=401,
                    detail="INVALID_LOGIN"
                )

    return {
        "id": user[0],
        "username": user[1],
        "email": user[2],
        "message": "Успешный вход"
    }

OLLAMA_GENERATE_URL = "http://localhost:11434/api/generate"
CHAT_MODEL_NAME = "qwen3:8b"


@app.post("/chat")
async def chat(request: ChatRequest):
    payload = {
        "model": CHAT_MODEL_NAME,
        "prompt": request.message,
        "stream": False,
        "think": False,
    }

    async with httpx.AsyncClient(trust_env=False, timeout=60.0) as client:
        response = await client.post(OLLAMA_GENERATE_URL, json=payload)
        response.raise_for_status()
        data = response.json()

    return {"response": data.get("response", "")}

@app.websocket("/ws/emotion")
async def emotion_websocket(
    websocket: WebSocket
):

    await websocket.accept()

    print(
        "Emotion WebSocket connected"
    )

    try:

        while True:

            message = await websocket.receive_text()

            print(
                "Received:",
                message
            )

            try:

                result = await analyze_emotion(
                    message
                )

                print(
                    "Emotion result:",
                    json.dumps(
                        result,
                        ensure_ascii=False
                    )
                )

                await websocket.send_text(
                    json.dumps(
                        result,
                        ensure_ascii=False
                    )
                )

            except Exception as e:

                print(
                    "Emotion Agent error:",
                    repr(e)
                )

                await websocket.send_text(
                    json.dumps(
                        {
                            "error": str(e)
                        },
                        ensure_ascii=False
                    )
                )

    except Exception as e:

        print(
            "WebSocket disconnected:",
            repr(e)
        )

@app.websocket("/ws/chat")
async def chat_websocket(websocket: WebSocket):
    await websocket.accept()
    print("Chat WebSocket connected")

    try:
        while True:
            message = await websocket.receive_text()
            print("Chat received:", message)

            payload = {
                "model": CHAT_MODEL_NAME,
                "prompt": message,
                "stream": True,
            }

            async with httpx.AsyncClient(trust_env=False, timeout=None) as client:
                async with client.stream("POST", OLLAMA_GENERATE_URL, json=payload) as response:
                    async for line in response.aiter_lines():
                        if not line:
                            continue
                        data = json.loads(line)
                        chunk = strip_emoji(data.get("response", ""))
                        if chunk:
                            await websocket.send_text(
                                json.dumps({"chunk": chunk}, ensure_ascii=False)
                            )
                        if data.get("done"):
                            await websocket.send_text(json.dumps({"done": True}))
                            break

    except Exception as e:
        print("Chat WebSocket disconnected:", repr(e))

@app.get("/db-test")
def db_test():
    with get_connection() as connection:
        with connection.cursor() as cursor:
            cursor.execute("SELECT 1")
            result = cursor.fetchone()

    return {"database": result[0]}