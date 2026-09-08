import json

from fastapi import FastAPI, WebSocket

from emotion_agent.emotion import analyze_emotion

import httpx
from pydantic import BaseModel

import re

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