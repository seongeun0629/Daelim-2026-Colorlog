import argparse
import json
import sys
from datetime import datetime
from . import (
    create_tables,
    get_user,
    get_user_stats,
    get_diagnoses_by_user,
    get_rec_products_by_diagnosis,
)

parser = argparse.ArgumentParser()
parser.add_argument("--user-id", type=int, required=True)
args = parser.parse_args()

create_tables()

user = get_user(args.user_id)
if not user:
    print(json.dumps({"error": "user not found"}))
    sys.exit(1)

stats = get_user_stats(args.user_id)
diagnoses = get_diagnoses_by_user(args.user_id)

for d in diagnoses:
    d["recommendations"] = get_rec_products_by_diagnosis(d["diagnosis_id"])

backup = {
    "exported_at": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
    "app": "Colorlog",
    "user": {
        "user_id": user["user_id"],
        "user_name": user["user_name"],
        "age": user.get("age", ""),
        "created_at": user.get("created_at", ""),
    },
    "stats": stats,
    "diagnoses": diagnoses,
}

print(json.dumps(backup, ensure_ascii=False, indent=2))
