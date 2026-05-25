import argparse
import json
import sys
from . import create_tables, get_all_users, get_user_stats, get_user

parser = argparse.ArgumentParser()
parser.add_argument("--user-id", type=int, default=None)
args = parser.parse_args()

create_tables()

user_id = args.user_id
if user_id is None:
    users = get_all_users()
    if not users:
        print(json.dumps({"diagnosis_count": 0, "join_date": "", "latest_color_type": "", "latest_at": "", "user_name": ""}))
        sys.exit(0)
    user_id = users[0]["user_id"]

stats = get_user_stats(user_id)
user = get_user(user_id)
stats["user_id"] = user_id
stats["user_name"] = user["user_name"] if user else ""
print(json.dumps(stats))
