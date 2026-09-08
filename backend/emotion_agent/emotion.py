import json
import httpx

VALID_EMOTIONS = {"neutral", "joy", "sadness", "anger", "fear", "surprise"}

OLLAMA_URL = "http://localhost:11434/api/chat"
MODEL_NAME = "qwen3:8b"

SYSTEM_PROMPT = """
You are an emotion analysis agent for an interactive 3D character.

Your task is to determine what emotion the CHARACTER should express
in response to the user's CURRENT TEXT.

The text may be incomplete because the user is still typing.
Therefore, do not assume that the first part of a message represents
the final meaning.

Analyze the entire text provided to you.

Available emotions:
- neutral
- joy
- sadness
- anger
- fear
- surprise

IMPORTANT:
You are NOT simply detecting words that explicitly describe emotions.
Instead, determine the most appropriate emotional reaction of the
character to what the user is saying.

Consider:
- events described by the user;
- positive or negative situations;
- unexpected events;
- potentially worrying situations;
- frustration or conflict;
- context and meaning;
- whether the message appears unfinished;
- whether the later part of the message changes the meaning of the
  earlier part.

INTENSITY (0.0-1.0):
0.0-0.2: very weak or no emotional signal.
0.3-0.5: possible emotional direction, but still uncertain.
0.6-0.8: strong reason to react emotionally.
0.9-1.0: extremely clear and strong reaction.

Do not automatically default to 0.3 or 0.4 — choose based on actual meaning.

CONFIDENCE (0.0-1.0):
How certain you are that the selected emotion is correct.
Do not confuse confidence with intensity.

EXAMPLES:
"привет" → neutral, low intensity
"я очень рад что наконец-то всё получилось" → joy, high intensity
"это меня ужасно раздражает" → anger, high intensity
"я боюсь что всё пойдёт не так" → fear, high intensity
"у меня сегодня произошёл странный случай" → surprise, moderate intensity
"у меня сегодня произошёл ужасный эпизод в моей жизни" → fear/sadness, moderate/high
"ладно, я шучу, всё нормально" → neutral, low intensity

Respond ONLY with a JSON object matching the given schema. No explanation, no extra text.
"""

EMOTION_SCHEMA = {
    "type": "object",
    "properties": {
        "emotion": {
            "type": "string",
            "enum": list(VALID_EMOTIONS)
        },
        "intensity": {"type": "number"},
        "confidence": {"type": "number"}
    },
    "required": ["emotion", "intensity", "confidence"]
}


def _clamp(value, lo=0.0, hi=1.0) -> float:
    try:
        value = float(value)
    except (TypeError, ValueError):
        return lo
    return max(lo, min(hi, value))


async def analyze_emotion(text: str) -> dict:
    """Отправляет текст в Ollama и возвращает {emotion, intensity, confidence}."""
    payload = {
        "model": MODEL_NAME,
        "messages": [
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": text},
        ],
        "stream": False,
        "format": EMOTION_SCHEMA,
        "options": {
            "think": False
        }
    }

    try:
        async with httpx.AsyncClient(trust_env=False, timeout=30.0) as client:
            response = await client.post(OLLAMA_URL, json=payload)
            response.raise_for_status()
            data = response.json()
            raw_content = data["message"]["content"]
            result = json.loads(raw_content)
    except Exception as e:
        print(f"Emotion analysis error: {e}")
        return {"emotion": "neutral", "intensity": 0.0, "confidence": 0.0}

    emotion = str(result.get("emotion", "neutral")).lower()
    if emotion not in VALID_EMOTIONS:
        emotion = "neutral"

    return {
        "emotion": emotion,
        "intensity": _clamp(result.get("intensity", 0.0)),
        "confidence": _clamp(result.get("confidence", 0.0)),
    }