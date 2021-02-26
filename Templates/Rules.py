{{info}}

from ..generic.Rules import set_rule

def set_rules(world, player):
    if world.logic[player] != 'nologic':
        world.completion_condition[player] = lambda state: state.has('Hollow Knight', player)

    {% for location, rule in conditions.items() %}
    set_rule(world.get_location("{{location}}", player), lambda state: {{rule}})
    {%- endfor %}

    # Events
    set_rule(world.get_location("Hollow Knight", player), lambda state: state.has('Lurien', player) and \
                                                                        state.has('Monomon', player) and \
                                                                        state.has('Herrah', player))
    {% for event, rule in waypoints.items() %}
    set_rule(world.get_location("{{event}}", player), lambda state: {{rule}})
    {%- endfor %}