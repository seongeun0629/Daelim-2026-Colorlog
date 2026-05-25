import argparse
from datetime import datetime
from . import create_tables, delete_diagnoses_by_user, update_user

parser = argparse.ArgumentParser()
parser.add_argument("--user-id", type=int, required=True)
args = parser.parse_args()

create_tables()
delete_diagnoses_by_user(args.user_id)

now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
update_user(args.user_id, created_at=now)

print("ok")
