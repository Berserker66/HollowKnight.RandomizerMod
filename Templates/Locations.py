locations = \
{{ locations|safe }}


lookup_id_to_name = {location_id: data["name"] for location_id, data in locations.items()}
lookup_name_to_id = {data["name"]: location_id for location_id, data in locations.items()}