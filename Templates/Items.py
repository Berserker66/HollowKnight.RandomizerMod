items = \
{{items|safe}}

item_table = {data["name"]: item_id for item_id, data in items.items()}
lookup_id_to_name = {item_id: data["name"] for item_id, data in items.items()}