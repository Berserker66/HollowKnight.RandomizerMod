currentArea = None
connections = {}

class RuleSegment():
    def __init__(self, condition):
        self.condition = condition

    @property
    def state(self):
        return f'state.has("{self.condition}", player)'

    def __add__(self, other):
        return PartialSegment(f'({self.state} and {other.state})')

    def __or__(self, other):
        return PartialSegment(f'({self.state} or {other.state})')

    def __radd__(self, other):
        return PartialSegment(f'({self.state} and {other.state})')

    def __ror__(self, other):
        return PartialSegment(f'({self.state} or {other.state})')

    def __str__(self):
        return self.state

class PartialSegment(RuleSegment):
    @property
    def state(self):
        return f'{self.condition}'

class GameOptionSegment(RuleSegment):

    @property
    def state(self):
        return f'state.world.{self.condition}[player]'

class Essence(RuleSegment):

    @property
    def state(self):
        return f'state.has_essence({self.condition}, player)'

class Grubs(RuleSegment):

    @property
    def state(self):
        return f'state.has_grubs({self.condition}, player)'

class Waypoint(RuleSegment):

    @property
    def state(self):
        # return f'state.can_reach("{self.condition}", player)'
        return f'state.has("{self.condition}", player)'

def sanitize(variable):
    return variable.replace("'", "__1").replace("-", "__2")

def create_parser(macros, game_options, items, waypoints):
    global currentArea, connections
    for option in game_options:
        globals()[option] = GameOptionSegment(option)
    for item in items:
        globals()[sanitize(item)] = RuleSegment(item)
    for event in waypoints:
        globals()[sanitize(event)] = Waypoint(event)
    globals()["ESSENCE200"] = Essence(200)
    globals()["ESSENCECOUNT"] = Essence(1000)
    globals()["GRUBCOUNT"] = Grubs(30)



    def parse(rule_text):
        global currentArea
        currentArea = None
        rule_text = str(rule_text)
        rule_text = sanitize(rule_text)
        result = eval(rule_text)

        return result
    macros_to_define = macros.copy()
    for event in waypoints:
        del macros_to_define[event]
    round = 0

    while macros_to_define:
        round += 1
        defined = set()
        for option, optiontext in macros_to_define.items():
            try:
                globals()[option] = parse(optiontext)
                if currentArea:
                    connections[currentArea] = option
            except NameError:  # dependant macro not defined yet
                pass
            else:
                defined.add(option)
        for option in defined:
            del macros_to_define[option]
        if not defined:
            raise Exception(f"Got stuck generating logic keys, could not define {len(macros_to_define)} - "
                            f"{macros_to_define.keys()}")
        else:
            print(f"Defined {len(defined)} conditions in round {round}")

    return parse