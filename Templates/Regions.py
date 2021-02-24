{{info}}

from . import create_region

def create_regions(world, player: int):
    world.regions += [
        create_region(world, player, 'Menu', None, ['Hollow Nest S&Q']),
        create_region(world, player, 'Hollow Nest', [location["name"] for location in locations.values()])
    ]