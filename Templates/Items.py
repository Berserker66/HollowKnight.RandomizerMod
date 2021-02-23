{{info}}

import typing

class HKItemData(typing.NamedTuple):
    advancement: bool
    id: int

item_table = \
{{items|safe}}

lookup_id_to_name = {data.id: item_name for item_name, data in item_table.items()}