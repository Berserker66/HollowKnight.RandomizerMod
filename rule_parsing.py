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
        return f'state.has_essence(player, {self.condition})'

class Grubs(RuleSegment):

    @property
    def state(self):
        return f'state.has_grubs(player, {self.condition})'

class Waypoint(RuleSegment):

    @property
    def state(self):
        # return f'state.can_reach("{self.condition}", player)'
        return f'state.has("{self.condition}", player)'

def sanitize(variable):
    return variable.replace("'", "__1").replace("-", "__2")

def split_or_conditions(text: str):
    current_buffer = ""
    layer = 0
    segments = []
    for character in text:
        if character == "(":
            layer += 1
        elif character == ")":
            layer -= 1
        elif not layer and character == "|":
            segments.append(current_buffer.strip())
            current_buffer = ""
        else:
            current_buffer += character

    if current_buffer:
        segments.append(current_buffer.strip())

    return segments

def create_parser(macros, game_options, items, waypoints):
    eval_locals = {}
    for option in game_options:
        eval_locals[option] = GameOptionSegment(option)
    for item in items:
        eval_locals[sanitize(item)] = RuleSegment(item)
    for event in waypoints:
        eval_locals[sanitize(event)] = Waypoint(event)
    eval_locals["ESSENCE200"] = Essence(200)
    eval_locals["ESSENCECOUNT"] = Essence(300)
    eval_locals["GRUBCOUNT"] = Grubs(5)



    def parse(rule_text):
        rule_text = str(rule_text)
        rule_text = sanitize(rule_text)
        return eval(rule_text, eval_locals)

    def parse_segmented(rule_text):
        rule_text = str(rule_text)
        rules = split_or_conditions(sanitize(rule_text))
        return [eval(rule_text, eval_locals) for rule_text in rules]

    macros_to_define = macros.copy()
    for event in waypoints:
        del macros_to_define[event]
    round = 0

    while macros_to_define:
        round += 1
        defined = set()
        for option, optiontext in macros_to_define.items():
            try:
                eval_locals[option] = parse(optiontext)
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

    return parse, parse_segmented