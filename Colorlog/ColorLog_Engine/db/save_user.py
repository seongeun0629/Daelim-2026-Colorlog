import argparse
from . import create_tables, seed_personal_color_types, seed_products, get_or_create_user, update_user

parser = argparse.ArgumentParser()
parser.add_argument("--user-name", required=True)
parser.add_argument("--age", default=None)
parser.add_argument("--user-id", type=int, default=None)
args = parser.parse_args()

create_tables()
seed_personal_color_types()
seed_products()

if args.user_id is not None:
    update_user(args.user_id, user_name=args.user_name, age=args.age)
    user_id = args.user_id
else:
    user_id = get_or_create_user(args.user_name, age=args.age)

print(user_id)
