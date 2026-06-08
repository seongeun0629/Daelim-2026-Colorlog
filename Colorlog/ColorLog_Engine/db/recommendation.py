import os
import json
import re
import urllib.parse
from pathlib import Path
from dotenv import load_dotenv

load_dotenv(Path(__file__).parent.parent / ".env")

_OLIVEYOUNG_SEARCH = (
    "https://www.oliveyoung.co.kr/store/search/getSearchMain.do?query={}"
)

_PROMPT_TEMPLATE = """당신은 퍼스널컬러 및 피부 전문가입니다.
퍼스널컬러 타입: {color_type}
추구미: {preferred_style}
피부 밝기: {brightness_desc}
홍조 정도: {redness_desc}

위 정보를 바탕으로 올리브영에서 구매할 수 있는 화장품을 총 5가지 추천해주세요.
반드시 아래 JSON 형식으로만 응답하고 다른 텍스트는 포함하지 마세요.

{{
    "recommendations": [
        {{"product_name": "실제 올리브영 판매 제품명", "search_keyword": "브랜드명 제품종류", "category": "카테고리", "reason": "추천이유", "rating": "4.8"}},
        {{"product_name": "실제 올리브영 판매 제품명", "search_keyword": "브랜드명 제품종류", "category": "카테고리", "reason": "추천이유", "rating": "4.5"}},
        {{"product_name": "실제 올리브영 판매 제품명", "search_keyword": "브랜드명 제품종류", "category": "카테고리", "reason": "추천이유", "rating": "4.7"}},
        {{"product_name": "실제 올리브영 판매 제품명", "search_keyword": "브랜드명 제품종류", "category": "스킨케어", "reason": "추천이유", "rating": "4.6"}},
        {{"product_name": "실제 올리브영 판매 제품명", "search_keyword": "브랜드명 제품종류", "category": "스킨케어", "reason": "추천이유", "rating": "4.9"}}
    ]
}}

- 앞 3개는 색조 메이크업(치크, 립, 아이, 베이스 중 하나), 뒤 2개는 반드시 기초 스킨케어(카테고리: 스킨케어)로 작성하세요.
- 스킨케어 추천 시 피부 밝기와 홍조 정도를 고려하여 맞춤 제품을 선택하세요.
- product_name: 실제 제품 전체 이름 (예: 롬앤 쥬시 래스팅 틴트 15 피그말리온)
- search_keyword: 올리브영 검색창에 입력할 키워드, 브랜드명+제품명 전체를 포함하여 최대한 구체적으로 작성 (예: 롬앤 쥬시 래스팅 틴트 베어그레이프). 색상 번호(#숫자)는 제외하세요.
- 카테고리는 치크, 립, 아이, 베이스, 스킨케어 중 하나로 작성하세요.
- rating: 이 퍼스널컬러 타입과의 매칭도를 1.0~5.0 사이 소수점 한 자리로 평가하세요."""


def _brightness_desc(val: int) -> str:
    if val < 0:
        return "측정 불가"
    if val <= 40:
        return f"낮음({val}점) - 어두운 편으로 광채·밝기 보정 제품 필요"
    if val <= 70:
        return f"보통({val}점)"
    return f"밝은 편({val}점)"


def _redness_desc(val: int) -> str:
    if val < 0:
        return "측정 불가"
    if val <= 30:
        return f"없음({val}점)"
    if val <= 60:
        return f"약간 있음({val}점) - 진정 성분 제품 권장"
    return f"홍조 있음({val}점) - 진정·쿨링 제품 필수"


def get_ai_recommendation(
    color_type: str,
    preferred_style: str,
    brightness: int = -1,
    redness: int = -1,
) -> list[dict]:
    """Gemini API로 올리브영 제품 5가지(색조 3+스킨케어 2) 추천. 실패 시 빈 리스트 반환."""
    api_key = os.getenv("GEMINI_API_KEY")
    if not api_key:
        return []

    try:
        from google import genai
        client = genai.Client(api_key=api_key)
        prompt = _PROMPT_TEMPLATE.format(
            color_type=color_type,
            preferred_style=preferred_style,
            brightness_desc=_brightness_desc(brightness),
            redness_desc=_redness_desc(redness),
        )
        response = client.models.generate_content(
            model="gemini-2.5-flash",
            contents=prompt,
        )
        text = response.text.strip()

        # 코드블록으로 감싸진 경우 제거
        if text.startswith("```"):
            parts = text.split("```")
            text = parts[1] if len(parts) > 1 else text
            if text.startswith("json"):
                text = text[4:]

        text = text.strip()

        try:
            data = json.loads(text)
        except (json.JSONDecodeError, ValueError):
            return []

        recs = data.get("recommendations", [])

        for item in recs:
            keyword = item.get("search_keyword", "").strip()
            if not keyword:
                # search_keyword 없으면 색상 번호 제거한 제품명 사용
                keyword = re.sub(r'\s*#\S+.*', '', item["product_name"]).strip()
            item["product_url"] = _OLIVEYOUNG_SEARCH.format(
                urllib.parse.quote(keyword, safe="")
            )

        return recs

    except Exception:
        return []
