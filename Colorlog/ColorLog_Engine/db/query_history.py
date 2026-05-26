import argparse
import json
import sys
from datetime import datetime
from . import create_tables, get_all_users, get_diagnoses_by_month, get_diagnoses_last_7days

parser = argparse.ArgumentParser()
parser.add_argument("--user-id", type=int, default=None)
parser.add_argument("--year", type=int, default=None)
parser.add_argument("--month", type=int, default=None)
args = parser.parse_args()

create_tables()

user_id = args.user_id
if user_id is None:
    users = get_all_users()
    if not users:
        print(json.dumps({"monthly": [], "weekly": []}))
        sys.exit(0)
    user_id = users[0]["user_id"]

today = datetime.today()
year = args.year or today.year
month = args.month or today.month

monthly = get_diagnoses_by_month(user_id, year, month)
weekly = get_diagnoses_last_7days(user_id)

print(json.dumps({"monthly": monthly, "weekly": weekly}))
