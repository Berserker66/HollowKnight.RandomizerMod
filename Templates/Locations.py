{{info}}

lookup_id_to_name = \
{{ locations|safe }}



lookup_name_to_id = {location_name: location_id for location_id, location_name in lookup_id_to_name.items()}