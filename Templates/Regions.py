{{info}}


def create_regions(world, player: int):
    from . import create_region
    from .Items import item_table
    from .Locations import lookup_name_to_id
    world.regions += [
        create_region(world, player, 'Menu', None, ['Hollow Nest S&Q']),
        create_region(world, player, 'Hollow Nest', [location["name"] for location in lookup_name_to_id] +
                      [item_name for item_name, item_data in item_table.items() if item_data.type == "Event"])
    ]
