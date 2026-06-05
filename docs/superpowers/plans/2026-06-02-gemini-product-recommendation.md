# Gemini AI 제품 추천 연동 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 퍼스널컬러 진단 결과를 Gemini API에 전달해 올리브영 제품 3가지를 AI가 추천하고, 추천 URL을 products/rec_products 테이블에 저장한다.

**Architecture:** main.py에서 add_diagnosis() 호출 직후 get_ai_recommendation()을 호출하고, 결과를 save_ai_recommendations()으로 DB에 저장한 뒤 C#으로 전달하는 JSON에 recommendations 키로 포함한다. API 실패 시 기존 get_recommended_products()의 더미 시드 데이터로 폴백한다.

**Tech Stack:** google-generativeai, python-dotenv, SQLite (기존), gemini-1.5-flash 모델

---

## 파일 구조

| 파일 | 변경 유형 | 역할 |
|------|-----------|------|
| `Colorlog/ColorLog_Engine/.env` | 신규 생성 | GEMINI_API_KEY 환경변수 보관 |
| `Colorlog/ColorLog_Engine/requirements.txt` | 수정 | google-generativeai, python-dotenv 추가 |
| `.gitignore` (루트) | 수정 | .env 패턴 추가 |
| `Colorlog/ColorLog_Engine/db/recommendation.py` | 신규 생성 | Gemini API 호출 및 올리브영 URL 생성 |
| `Colorlog/ColorLog_Engine/db/repository.py` | 수정 | save_ai_recommendations() 함수 추가 |
| `Colorlog/ColorLog_Engine/db/__init__.py` | 수정 | 새 함수 export |
| `Colorlog/ColorLog_Engine/main.py` | 수정 | AI 추천 호출 및 JSON 응답에 포함 |

---

## Task 1: 환경 설정 (패키지, .env, .gitignore)

**Files:**
- Modify: `Colorlog/ColorLog_Engine/requirements.txt`
- Create: `Colorlog/ColorLog_Engine/.env`
- Modify: `.gitignore` (루트)

- [ ] **Step 1: requirements.txt에 패키지 추가**

`Colorlog/ColorLog_Engine/requirements.txt` 파일 끝에 추가:
```
google-generativeai
python-dotenv
```

- [ ] **Step 2: 패키지 설치**

```bash
cd Colorlog/ColorLog_Engine
pip install google-generativeai python-dotenv
```

Expected: Successfully installed google-generativeai-... python-dotenv-...

- [ ] **Step 3: .env 파일 생성**

`Colorlog/ColorLog_Engine/.env` 파일을 아래 내용으로 생성:
```
GEMINI_API_KEY=여기에_실제_키_입력
```

- [ ] **Step 4: 루트 .gitignore에 .env 추가**

루트 `.gitignore` 파일 끝에 아래 두 줄 추가:
```
# 환경 변수
.env
```

- [ ] **Step 5: Commit**

```bash
git add Colorlog/ColorLog_Engine/requirements.txt .gitignore
git commit -m "chore: add google-generativeai, python-dotenv deps and .gitignore .env"
```

> ⚠️ `.env` 파일은 절대 git add 하지 않는다.

---

## Task 2: db/recommendation.py 신규 생성

**Files:**
- Create: `Colorlog/ColorLog_Engine/db/recommendation.py`

- [ ] **Step 1: 파일 작성**

`Colorlog/ColorLog_Engine/db/recommendation.py`를 아래 내용으로 생성:

```python
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
```

- [ ] **Step 2: 수동 동작 확인 (API 키 설정 후)**

```bash
cd Colorlog/ColorLog_Engine
python -c "
from db.recommendation import get_ai_recommendation
result = get_ai_recommendation('봄 웜톤 (Spring Warm)', '생기있는, 따뜻한')
import json; print(json.dumps(result, ensure_ascii=False, indent=2))
"
```

Expected 출력 (형태):
```json
[
  {
    "product_name": "롬앤 쥬시 래스팅 틴트",
    "category": "립",
    "reason": "봄 웜톤에 어울리는 코랄 계열...",
    "product_url": "https://www.oliveyoung.co.kr/store/search/getSearchMain.do?query=..."
  },
  ...
]
```

API 키 미설정 시: `[]` 반환 (폴백 동작 확인)

- [ ] **Step 3: Commit**

```bash
git add Colorlog/ColorLog_Engine/db/recommendation.py
git commit -m "feat(db): add Gemini AI product recommendation module"
```

---

## Task 3: repository.py에 save_ai_recommendations() 추가

**Files:**
- Modify: `Colorlog/ColorLog_Engine/db/repository.py`

- [ ] **Step 1: 함수 추가**

`repository.py` 파일 끝(마지막 함수 다음)에 아래 함수를 추가:

```python
def save_ai_recommendations(diagnosis_id: int, recommendations: list[dict]) -> list[dict]:
    """AI 추천 결과를 products + rec_products에 저장. 저장된 레코드 리스트 반환."""
    from .schema import get_connection
    conn = get_connection()
    saved = []
    try:
        for item in recommendations:
            cur = conn.execute(
                """INSERT INTO products (product_url, product_name, keyword, category, tone_type)
                   VALUES (?, ?, ?, ?, ?)""",
                (
                    item.get("product_url", ""),
                    item.get("product_name", ""),
                    item.get("keyword", ""),
                    item.get("category", ""),
                    item.get("tone_type", ""),
                ),
            )
            product_id = cur.lastrowid
            conn.execute(
                """INSERT INTO rec_products (product_id, diagnosis_id, rec_reason)
                   VALUES (?, ?, ?)""",
                (product_id, diagnosis_id, item.get("reason", "")),
            )
            saved.append({
                "product_id": product_id,
                "product_name": item.get("product_name", ""),
                "product_url": item.get("product_url", ""),
                "category": item.get("category", ""),
                "reason": item.get("reason", ""),
            })
        conn.commit()
    finally:
        conn.close()
    return saved
```

> ℹ️ `get_connection()`은 `db/schema.py`에서 export되는 DB 연결 함수이다. repository.py 상단에서 이미 `from .schema import get_connection`으로 import 중이다.

- [ ] **Step 2: __init__.py에 export 추가**

`Colorlog/ColorLog_Engine/db/__init__.py`를 열어 기존 import 목록에 아래를 추가:

```python
from .repository import save_ai_recommendations
from .recommendation import get_ai_recommendation
```

- [ ] **Step 3: Commit**

```bash
git add Colorlog/ColorLog_Engine/db/repository.py Colorlog/ColorLog_Engine/db/__init__.py
git commit -m "feat(db): add save_ai_recommendations to repository"
```

---

## Task 4: main.py에 AI 추천 호출 및 JSON 통합

**Files:**
- Modify: `Colorlog/ColorLog_Engine/main.py`

- [ ] **Step 1: import 추가**

main.py 상단 import 블록에 추가:

```python
from db.recommendation import get_ai_recommendation
from db.repository import save_ai_recommendations, get_recommended_products
```

- [ ] **Step 2: add_diagnosis() 호출 직후 AI 추천 블록 추가**

main.py에서 아래 기존 코드를 찾아:

```python
            diagnosis_id = add_diagnosis(
                user_id=user_id,
                lab_l=avg_L,
                lab_a=avg_a,
                lab_b=avg_b,
                brightness=brightness_val,
                redness=redness_val,
                type_id=type_id,
            )
```

해당 블록 **바로 아래**에 다음 코드를 추가 (diagnosis_saved = True 이전):

```python
            # AI 제품 추천 (실패 시 더미 시드 폴백)
            preferred_style = color_type.get("keyword", "") if color_type else ""
            ai_recs = get_ai_recommendation(best_type_name, preferred_style)

            if ai_recs:
                recommendations = save_ai_recommendations(diagnosis_id, ai_recs)
            else:
                # 폴백: 기존 시드 데이터에서 tone_type으로 조회
                tone = color_type.get("tone", "웜") if color_type else "웜"
                tone_type = "쿨" if "쿨" in tone else "웜"
                fallback = get_recommended_products(tone_type)
                recommendations = [
                    {
                        "product_id": p["product_id"],
                        "product_name": p.get("product_name", ""),
                        "product_url": p.get("product_url", ""),
                        "category": p.get("category", ""),
                        "reason": "퍼스널컬러 기반 추천",
                    }
                    for p in fallback[:3]
                ]

            frame_data["recommendations"] = recommendations
```

- [ ] **Step 3: color_type 딕셔너리의 "tone" 키 확인**

personal_color_types 테이블의 `tone` 컬럼이 schema.py에 정의되어 있다. repository.py의 `get_color_type_by_name()` 반환값에 `"tone"` 키가 포함되어 있는지 확인 후, 없으면 `color_type.get("type_name", "")` 기반으로 웜/쿨 판별:

```python
# tone 키가 없을 때 대체 판별
type_name = best_type_name or ""
tone_type = "쿨" if "쿨" in type_name else "웜"
```

- [ ] **Step 4: main.py 실행 테스트 (API 키 있을 때)**

```bash
cd Colorlog/ColorLog_Engine
python main.py --user-name "테스트유저"
```

Expected: JSON 출력에 `"recommendations"` 키 포함, 3개 상품 정보 존재

- [ ] **Step 5: 폴백 테스트 (API 키 없을 때)**

`.env`의 GEMINI_API_KEY를 빈 값으로 변경한 뒤 실행:
```bash
python main.py --user-name "폴백테스트"
```

Expected: `"recommendations"` 키가 존재하고 시드 데이터 기반 3개 상품이 반환됨

- [ ] **Step 6: Commit**

```bash
git add Colorlog/ColorLog_Engine/main.py
git commit -m "feat(main): integrate Gemini AI recommendation into diagnosis flow"
```

---

## 검증 체크리스트

| 항목 | 확인 방법 |
|------|-----------|
| `.env`가 git에 포함되지 않음 | `git status`에 `.env` 미출력 |
| AI 추천 정상 작동 | main.py JSON 출력에 `recommendations` 3개 포함 |
| API 실패 시 폴백 | GEMINI_API_KEY 제거 후 실행 → 시드 데이터 3개 반환 |
| products 테이블 저장 | `python -c "from db.repository import get_rec_products_by_diagnosis; print(get_rec_products_by_diagnosis(1))"` |
| _PRODUCTS_SEED 무결성 | schema.py의 시드 데이터 변경 없음 확인 |
