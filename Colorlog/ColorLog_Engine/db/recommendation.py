import os
import json
import urllib.parse
from pathlib import Path
from dotenv import load_dotenv

load_dotenv(Path(__file__).parent.parent / ".env")

_OLIVEYOUNG_SEARCH = (
    "https://www.oliveyoung.co.kr/store/search/getSearchMain.do?query={}"
)

_PROMPT_TEMPLATE = """당신은 퍼스널컬러 전문가입니다.
퍼스널컬러 타입: {color_type}
추구미: {preferred_style}

위 정보를 바탕으로 올리브영에서 구매할 수 있는 화장품 제품 3가지를 추천해주세요.
반드시 아래 JSON 형식으로만 응답하고 다른 텍스트는 포함하지 마세요.

{{
    "recommendations": [
        {{"product_name": "실제 올리브영 판매 제품명", "category": "카테고리", "reason": "추천이유"}},
        {{"product_name": "실제 올리브영 판매 제품명", "category": "카테고리", "reason": "추천이유"}},
        {{"product_name": "실제 올리브영 판매 제품명", "category": "카테고리", "reason": "추천이유"}}
    ]
}}

카테고리는 치크, 립, 아이, 베이스, 스킨케어 중 하나로 작성하세요."""


def get_ai_recommendation(color_type: str, preferred_style: str) -> list[dict]:
    """Gemini API로 올리브영 제품 3가지 추천. 실패 시 빈 리스트 반환."""
    api_key = os.getenv("GEMINI_API_KEY")
    if not api_key:
        return []

    try:
        import google.generativeai as genai

        genai.configure(api_key=api_key)
        model = genai.GenerativeModel("gemini-1.5-flash")
        prompt = _PROMPT_TEMPLATE.format(
            color_type=color_type,
            preferred_style=preferred_style,
        )
        response = model.generate_content(prompt)
        text = response.text.strip()

        # 코드블록 감싸진 경우 제거
        if text.startswith("```"):
            text = text.split("```")[1]
            if text.startswith("json"):
                text = text[4:]

        data = json.loads(text)
        recs = data.get("recommendations", [])

        for item in recs:
            item["product_url"] = _OLIVEYOUNG_SEARCH.format(
                urllib.parse.quote(item["product_name"])
            )

        return recs

    except Exception:
        return []
