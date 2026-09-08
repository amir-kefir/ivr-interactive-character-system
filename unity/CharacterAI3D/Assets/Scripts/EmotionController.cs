using UnityEngine;

public class EmotionController : MonoBehaviour
{
    [Header("Emotion")]
    [SerializeField]
    private float intensityThreshold = 0.5f;

    private string currentEmotion = "neutral";
    private float currentIntensity = 0.4f;

    public void ApplyEmotion(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        EmotionData data;

        try
        {
            data = JsonUtility.FromJson<EmotionData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Failed to parse emotion JSON: " + e.Message
            );
            return;
        }

        if (data == null || string.IsNullOrWhiteSpace(data.emotion))
        {
            Debug.LogWarning("Invalid emotion data.");
            return;
        }

        currentEmotion = data.emotion;
        currentIntensity = data.intensity;

        UpdateEmotionState();
    }

    private void UpdateEmotionState()
    {
        if (currentIntensity <= intensityThreshold)
        {
            SetSemiIdle(currentEmotion);
        }
        else
        {
            SetIdle(currentEmotion);
        }
    }

    private void SetSemiIdle(string emotion)
    {
        Debug.Log(
            $"Emotion state: {emotion} → SemiIdle " +
            $"(intensity: {currentIntensity:F2})"
        );

        // Animator подключим следующим этапом.
    }

    private void SetIdle(string emotion)
    {
        Debug.Log(
            $"Emotion state: {emotion} → Idle " +
            $"(intensity: {currentIntensity:F2})"
        );

        // Animator подключим следующим этапом.
    }

    [System.Serializable]
    private class EmotionData
    {
        public string emotion;
        public float intensity;
        public float confidence;
    }
}