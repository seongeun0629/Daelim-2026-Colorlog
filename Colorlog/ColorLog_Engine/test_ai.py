from db.recommendation import get_ai_recommendation
import json

try:
    from google import genai
    print("google.genai 임포트 성공")
except Exception as e:
    print(f"google.genai 임포트 실패: {e}")

result = get_ai_recommendation('여름 뮤트 쿨톤 (Summer Mute Cool)', '차분한, 우아한, 뮤트한')
print(json.dumps(result, ensure_ascii=False, indent=2))
