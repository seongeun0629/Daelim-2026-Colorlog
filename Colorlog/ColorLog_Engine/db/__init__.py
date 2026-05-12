# db 패키지 초기화
# 아래 함수들을 from db import ... 형태로 바로 불러올 수 있습니다.

from .schema import create_tables, get_connection

from .repository import (
    # 퍼스널컬러 유형
    add_color_type,
    get_color_type,
    get_all_color_types,
    # 사용자
    add_user,
    get_user,
    get_all_users,
    # 진단결과
    add_diagnosis,
    get_diagnosis,
    get_diagnoses_by_user,
    # 화장품
    add_product,
    get_product,
    get_all_products,
    # 추천 화장품
    add_rec_product,
    get_rec_products_by_diagnosis,
)
