from lxml import etree
import os
from pprint import pformat
from jinja2 import Template

HK_ID_OFFSET = 0x1000000
HK_LOC_ID_OFFSET = 0x1100000

parser = etree.XMLParser(remove_comments=True)
tree = etree.XML(open(os.path.join("RandomizerLib3.0", "Resources", "items.xml")).read(), parser=parser)

translation = {"progression": "advancement"}

location_translation = {"x": "x",
                        "y": "y",
                        "sceneName": "scene"}

default = {"advancement": False}
default_location = {"x": 0, "y": 0, "scene": None}

items = {}
locations = {}

current_id_offset = 0


for element in tree:
    current_id_offset += 1
    name = element.get("name")
    current_item = items[HK_ID_OFFSET+current_id_offset] = default.copy()
    current_location = locations[HK_LOC_ID_OFFSET+current_id_offset] = default_location.copy()
    current_item["name"] = current_location["name"] = name
    for attr in element:
        tag = attr.tag
        if tag in translation:
            current_item[translation[tag]] = attr.text == 'true'
        elif tag in location_translation:
            if tag in ("x", "y"):
                current_location[location_translation[tag]] = float(attr.text)
            else:
                current_location[location_translation[tag]] = attr.text


item_template = Template(open(os.path.join("Templates", "Items.py")).read()).render(items=pformat(items, indent=4))
with open("Items.py", "wt") as f:
    f.write(item_template)

loc_template = Template(open(os.path.join("Templates", "Locations.py")).read()).render(locations=pformat(locations, indent=4))
with open("Locations.py", "wt") as f:
    f.write(loc_template)