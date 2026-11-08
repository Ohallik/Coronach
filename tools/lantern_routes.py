"""Writes the chapter 2 replay routes (chapter-12 to chapter-16) from the zones' real coordinates.

Waypoints come from each zone's baked navigation (LanternRoadBuilder positions, checked with QaRoutePaths), so the
virtual pad follows ground a body can cross. Each route resumes from the autosave the one before it left.

  python tools/lantern_routes.py
"""
import json
from pathlib import Path

ROUTES = Path(__file__).resolve().parent.parent / "docs" / "quality" / "routes"
FLIGHT_SPEED, BRAKED_FLIGHT, CIVIL_SPEED, COMBAT_SPEED = 11.0, 2.0, 4.5, 6.5


class Route:
    def __init__(self, name, note):
        self.name, self.note, self.steps = name, note, []
        self.scene = "Title"
        self.at = None
        self.speed = CIVIL_SPEED
        self.brake = False

    def add(self, name, seconds, **fields):
        fields = {k: v for k, v in fields.items() if v is not None}
        fields.setdefault("expectedScene", self.scene)
        self.steps.append(dict(name=name, seconds=round(seconds, 2), **fields))

    def resume(self, scene, flag=None, speed=CIVIL_SPEED, spawn=None, form=None, brake=False):
        """Title, Continue, the autosave by name: the picker opens on whichever save is newest."""
        self.add("read title", 1, expectedUi="title")
        self.add("select Continue", 0.2, buttons=[], expectedUi="title")
        self.add("release select Continue", 0.3)
        self.add("open save picker", 0.5, buttons=["South"], expectedUi="saves")
        self.add("release save picker", 0.3)
        self.scene = scene
        self.add("load the autosave", 4, buttons=["South"], pickRow="autosave", expectedUi="world", expectedFlag=flag, expectedForm=form)
        self.at, self.speed, self.brake = spawn, speed, brake

    def walk(self, name, x, y, z, tolerance=0.8, seconds=None, stop=None, buttons=None):
        distance = 20.0 if self.at is None else ((x - self.at[0]) ** 2 + (z - self.at[1]) ** 2) ** 0.5
        pace = BRAKED_FLIGHT if self.brake else self.speed
        limit = seconds if seconds else min(170.0, max(2.0, distance / pace * 1.6 + 2))
        self.add(name, limit, navigate=True, point=dict(x=x, y=y, z=z), tolerance=tolerance, until="arrived", stopWhen=stop,
                 leftTrigger=1 if self.brake else None, buttons=buttons)
        self.at = (x, z)

    def path(self, name, points, **kw):
        for i, (x, y, z) in enumerate(points):
            self.walk(f"{name} {i + 1}/{len(points)}", x, y, z, **kw)

    def press(self, name, button="South", seconds=0.2, **kw):
        self.add(name, seconds, buttons=[button], **kw)
        self.add("release " + name, 0.3)

    def talk(self, name, lines, flag=None, capture=True):
        """Start a conversation with the confirm button and read it. Each line is held long enough to read, as a person would."""
        self.add("start " + name, 0.2, buttons=["South"], expectedUi="dialogue")
        self.add("release start " + name, 0.3, expectedUi="dialogue")
        self.read(name, lines, flag, capture)

    def read(self, name, lines, flag=None, capture=True):
        # Hold each line as a person would, then press once. A long line may still be typing (that press completes it) and a
        # short one is already done (that press moves on), so line counts can't drift; nothing is pressed once it closes.
        closed = "ui:world"
        for i in range(lines if capture else 0):
            self.add(f"read {name} line {i + 1}", 1.9, stopWhen=closed)
            self.add(f"next {name} line {i + 1}", 0.15, buttons=["South"], stopWhen=closed)
            self.add(f"release {name} line {i + 1}", 0.2, stopWhen=closed)
        self.add(f"finish {name}", 40, buttons=["South"], pulseSeconds=1.0, until=closed, expectedUi="world", expectedFlag=flag)
        self.add(f"release after {name}", 0.5)

    def talk_to_shop(self, name, lines):
        """A conversation that ends at a stall: read it as talk() does, stop pressing the moment the stock opens (there the confirm button buys), then leave."""
        opened = "ui:shop"
        self.add("start " + name, 0.2, buttons=["South"], expectedUi="dialogue")
        self.add("release start " + name, 0.3, expectedUi="dialogue")
        for i in range(lines):
            self.add(f"read {name} line {i + 1}", 1.9, stopWhen=opened)
            self.add(f"next {name} line {i + 1}", 0.15, buttons=["South"], stopWhen=opened)
            self.add(f"release {name} line {i + 1}", 0.2, stopWhen=opened)
        self.add(f"finish {name}", 40, buttons=["South"], pulseSeconds=1.0, until=opened, expectedUi="shop")
        self.add(f"look over the stock after {name}", 1, expectedUi="shop")
        self.add(f"leave the stock after {name}", 0.5, buttons=["East"], expectedUi="world")
        self.add(f"release the stall after {name}", 0.3)

    def wait(self, name, seconds, **kw):
        self.add(name, seconds, **kw)

    def lead(self, hero):
        self.add(f"{hero} leads", 1.2, ensureCharacter=hero)

    def gel(self, label, stop):
        self.add("repair gel " + label, 0.2, leftTrigger=1, stopWhen=stop)
        self.add("release gel " + label, 0.4)

    def fight_ground(self, id_, rounds=4, seconds=30, skills=True):
        """An ordinary fight: lock, burst with what's equipped, chase and strike, dodge what's marked, gel between rounds."""
        stop = f"flag:clear.{id_}"
        self.press(f"lock {id_}", "LeftShoulder", 0.18)
        for r in range(rounds):
            if skills:
                self.add(f"skill {id_} {r + 1}a", 0.18, buttons=["RightShoulder", "South"], stopWhen=stop)
                self.add(f"release skill {id_} {r + 1}a", 0.45)
                self.add(f"skill {id_} {r + 1}b", 0.18, buttons=["RightShoulder", "West"], stopWhen=stop)
                self.add(f"release skill {id_} {r + 1}b", 0.45)
            self.add(f"fight {id_} round {r + 1}", seconds, buttons=["South"], pulseSeconds=0.46, navigate=True, approachTarget=True,
                     tolerance=2.3, rangedTolerance=12, evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=0, z=0))
            self.gel(f"{id_} {r + 1}", stop)
        self.add(f"{id_} cleared", 3, until=stop, expectedFlag=f"clear.{id_}")
        self.recover(id_)

    def recover(self, label):
        """What a player does between fights: revive the partner, take the lead back, top up with a gel."""
        self.add(f"reach the fallen hero after {label}", 16, navigate=True, approachPartner=True, tolerance=1.2, stopWhen="partyAlive", point=dict(x=0, y=0, z=0))
        self.add(f"both heroes ready after {label}", 6, until="partyAlive")
        self.lead("Taren")
        self.add(f"repair gel after {label}", 0.2, leftTrigger=1)
        self.add(f"release gel after {label}", 0.4)

    def fight_flight(self, id_, rounds=3, seconds=30):
        stop = f"flag:clear.{id_}"
        self.press(f"lock {id_}", "LeftShoulder", 0.18, leftTrigger=1)
        for r in range(rounds):
            self.add(f"pursue {id_} round {r + 1}", seconds, buttons=["South"], leftTrigger=1, navigate=True, approachTarget=True, tolerance=12,
                     rangedTolerance=12, evadeProjectiles=True, evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=1, z=0))
            self.add(f"pulse {id_} round {r + 1}", 0.18, buttons=["RightShoulder", "West"], leftTrigger=1, stopWhen=stop)
            self.add(f"release pulse {id_} round {r + 1}", 0.45, leftTrigger=1)
        self.add(f"{id_} cleared", 3, until=stop, expectedFlag=f"clear.{id_}")

    def write(self):
        data = dict(name=self.name, scene="Title", starterParty=False, settleSeconds=5, frameCap=60, screenshotInterval=0,
                    sourceRevision=self.note, steps=self.steps)
        path = ROUTES / (self.name + ".json")
        path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
        print(path.name, len(self.steps), "steps")


def wick_reach():
    r = Route("chapter-12-wick-reach", "Chapter 2: the warp from Tallow, two relays, the gate, the dock")
    # A player who stopped at Tallow may still owe the Keeper the report, the Compact's call and the lantern tale. Talk until
    # the route is known; each round does nothing once it is.
    r.resume("TallowDrift", "tallow.lineObserved", spawn=(0, -10))
    r.lead("Taren")
    r.walk("through the pressure cabin", 0, 0, 1)
    r.walk("stand beside Keeper Abri", 0, 0, 2, tolerance=0.3)
    known = "flag:lantern.routeKnown"
    for i in range(3):
        r.add(f"talk to Abri {i + 1}", 0.2, buttons=["South"], stopWhen=known)
        r.add(f"release talk {i + 1}", 0.3)
        r.add(f"hear Abri out {i + 1}", 60, buttons=["South"], pulseSeconds=0.55, until="ui:world", expectedUi="world")
        r.add(f"release after Abri {i + 1}", 0.5)
    r.wait("the lantern route is known", 0.5, expectedFlag="lantern.routeKnown")
    r.walk("to the pressure lift", 0, 0, -8)
    r.scene = "TallowApproach"
    r.add("launch from Tallow", 3, buttons=["South"], expectedForm="CivilFlight")
    r.add("release launch", 0.5)
    r.at, r.speed, r.brake = (0, -8.5), FLIGHT_SPEED, False
    r.walk("toward the Wick Reach warp", 16, 1, -15, tolerance=3)
    r.brake = True
    r.walk("settle at the warp", 21, 1, -18, tolerance=1.5)
    r.brake = False
    r.scene = "WickReach"
    r.add("take the warp", 4, buttons=["South"], expectedForm="Flight")
    r.add("release warp", 0.5)
    r.at = (0, 6)
    r.walk("up the lane", 0, 1, 53, tolerance=3)
    r.fight_flight("Wick_0")
    r.walk("past the first wreck", 0, 1, 118, tolerance=3)
    r.fight_flight("Wick_1")
    r.walk("toward the first relay", -16, 1, 146, tolerance=3)
    r.brake = True
    r.walk("settle at the first relay", -21, 1, 150, tolerance=1)
    r.brake = False
    r.talk("first relay", 4, flag="wick.relay1")
    r.walk("back to the lane", 0, 1, 183, tolerance=3)
    r.fight_flight("Wick_2")
    r.walk("toward the second relay", 16, 1, 224, tolerance=3)
    r.brake = True
    r.walk("settle at the second relay", 21, 1, 228, tolerance=1)
    r.brake = False
    r.add("relight the second relay", 0.3, buttons=["South"], expectedFlag="wick.relay2")
    r.add("release second relay", 1)
    r.walk("toward the last raiders", 5, 1, 236, tolerance=3)
    r.fight_flight("Wick_3")
    r.wait("the curtain falls", 1.5, expectedFlag="wick.gateOpen")
    r.walk("through the lantern gate", 0, 1, 272, tolerance=3)
    r.brake = True
    r.walk("onto the courier approach", 0, 1, 282, tolerance=1)
    r.brake = False
    r.scene = "Lanternwick"
    r.add("dock at Lanternwick", 4, buttons=["South"], expectedUi="world", expectedForm="Natural")
    r.add("release dock", 0.5)
    r.wait("first look at the town", 2)
    r.write()


def lanternwick():
    r = Route("chapter-13-lanternwick", "Chapter 2: Corra, Pell, Keeper Aumi, the side quests, Ishen, Brekk, the orchard gate")
    r.resume("Lanternwick", "wick.gateOpen", spawn=(0, -14))
    r.lead("Taren")
    r.walk("up to the arch market", 0, 0, 2)
    r.walk("to Corra's stall", -4, 0, 17)
    r.add("greet Corra", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release greet Corra", 0.3, expectedUi="dialogue")
    r.add("hear Corra's pitch", 12, buttons=["South"], pulseSeconds=0.55, until="ui:options", expectedUi="options")
    r.add("release before her stock", 0.5)
    r.add("look at the stock", 1.5, buttons=["South"], expectedUi="shop")
    r.add("release stock", 0.5)
    # A stall opens on its toggle, so the first offer is named: Corra's first row, 240 scrip.
    r.wait("read the Refined parts", 1.2, pickRow="Refined Emitter", expectedUi="shop")
    r.press("buy a Refined part", "South", 0.3, pickRow="Refined Emitter", expectedUi="shop")
    r.wait("read the receipt", 1.2, expectedUi="shop")
    # What a player with scrip to spare does before the gorge. Hessa is a long fight, and the route used to reach her on one gel.
    r.add("find the repair gel", 3, pickRow="Repair Gel", until="row:Repair Gel", expectedUi="shop")
    for i in range(12):
        r.press(f"buy repair gel {i + 1}", "South", 0.2, pickRow="Repair Gel", expectedUi="shop")
    r.add("leave the stall", 0.5, buttons=["East"], expectedUi="world")
    r.add("release leave stall", 0.3)
    r.walk("up to Pell", 4, 0, 33)
    r.talk("Pell", 4, flag="met.Pell")
    r.path("to the lantern stair", [(6, 0, 31), (17.6, 1, 25.1), (18.9, 1.5, 25.5), (19.4, 3.2, 49.5), (18.9, 3.1, 51.3), (20, 3.1, 53.5)])
    r.talk("Keeper Aumi", 8, flag="met.Aumi")
    r.path("down the lantern stair", [(18.9, 3.1, 51.3), (19.4, 3.2, 49.5), (18.9, 1.5, 25.5), (17.1, 1, 25.5), (7, 0.2, 36)])
    r.talk("Pell's lamp", 4)
    r.path("back through the market", [(5, 0.3, 34.5), (-4, 0.2, 17)])
    r.talk_to_shop("Corra's request", 6)
    r.path("up the main street", [(-2.5, 0.2, 27.8), (3, 0.2, 90)])
    r.talk("Ishen", 5, flag="met.Ishen")
    r.path("to the orchard", [(-6, 0.1, 81), (-15.8, 0.1, 74.1), (-30, 1.5, 60)])
    r.talk("the orchard shed", 3, flag="met.Brekk")
    r.path("to the orchard gate", [(-32.7, 1.5, 71.1), (-32, 1.5, 81.6), (-31.5, 1.5, 84)], tolerance=0.6)
    r.scene = "LanternGorge"
    r.add("through the orchard gate", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release orchard gate", 0.5)
    r.write()


def gorge():
    r = Route("chapter-14-lantern-gorge", "Chapter 2: three relays, the den, the camp, the door")
    r.resume("LanternGorge", "met.Aumi", speed=COMBAT_SPEED, spawn=(0, 0))
    r.lead("Taren")
    r.walk("into the gorge", 2, 0, 24)
    r.fight_ground("Gorge_0")
    r.walk("to the first relay", 8.2, 0.2, 50.5, tolerance=0.5)
    r.talk("first gorge relay", 3, flag="gorge.relay1")
    # Gorge_1 sits in the first relay's reach now: the pack waits at the ward's edge, so the walk stops inside it, in clear ground.
    r.walk("deeper into the dark", 6, 0, 58)
    r.fight_ground("Gorge_1")
    r.walk("to the den's mouth", -8, 0, 92)
    r.walk("into the den", -20, 0, 98)
    r.fight_ground("Gorge_Den", rounds=3)
    r.walk("to Pell's lamp", -24.5, 0, 99.5, tolerance=1)
    r.add("pick up the chewed lamp", 0.3, buttons=["South"], expectedFlag="pell.lampFound")
    r.add("release lamp", 0.5)
    r.walk("out of the den", -8, 0, 109)
    r.fight_ground("Gorge_Camp")
    r.walk("around the camp crates", -6, 0.2, 103, tolerance=1)
    r.walk("to the ration crate", -11, 0.3, 103.8, tolerance=0.6)
    r.add("take a ration crate", 0.3, buttons=["South"], expectedFlag="unsworn.rationsFound")
    r.add("release crate", 0.5)
    r.walk("to the second relay", -8, 0.2, 122.5, tolerance=0.5)
    r.add("relight the second relay", 0.3, buttons=["South"], expectedFlag="gorge.relay2")
    r.add("release second relay", 1)
    r.walk("past the camp", -4, 0.2, 128)
    r.walk("up the canyon", 2, 0.2, 152)
    r.fight_ground("Gorge_3")
    r.walk("to the third relay", 8.8, 0.4, 184.5, tolerance=0.5)
    r.add("relight the third relay", 0.3, buttons=["South"], expectedFlag="gorge.relaysLit")
    r.add("release third relay", 1)
    r.walk("to the last crew", 6, 0.4, 200)
    r.fight_ground("Gorge_4")
    r.walk("to the buried door", 0, 0.2, 226.8, tolerance=0.5)
    r.scene = "SunkenRelay"
    r.add("enter the Sunken Relay", 4, buttons=["South"], expectedUi="world", expectedForm="Shaped")
    r.add("release relay door", 0.5)
    r.write()


def relay():
    r = Route("chapter-15-sunken-relay", "Chapter 2: the plates, the checkpoint, Hessa Korl and the relay lift")
    r.resume("SunkenRelay", "gorge.relaysLit", speed=COMBAT_SPEED, spawn=(0, 3))
    r.lead("Taren")
    r.walk("into the entry hall", 0, 0.1, 12, tolerance=1)
    r.talk("the cut housings", 3, flag="relay.entered")
    r.walk("to the first plate", -5.2, 0.1, 14.8, tolerance=1)
    r.add("take the first plate", 0.3, buttons=["South"], expectedFlag="relay.plate1")
    r.add("release first plate", 0.5)
    r.path("into the cut corridor", [(-2.9, 0.1, 16.1), (0, 0.1, 27)])
    r.fight_ground("Relay_0", rounds=3)
    r.walk("into the flooded gallery", 0, 0.1, 55)
    r.fight_ground("Relay_1")
    r.walk("to the second plate", 11, 0.1, 75, tolerance=1)
    r.add("take the second plate", 0.3, buttons=["South"], expectedFlag="relay.plate2")
    r.add("release second plate", 0.5)
    r.path("to the lower vault", [(8.4, 0.1, 77.6), (0, 0.1, 93)])
    r.fight_ground("Relay_2")
    r.walk("to the third plate", -8, 0.1, 107, tolerance=1)
    r.add("take the third plate", 0.3, buttons=["South"], expectedFlag="relay.plate3")
    r.add("release third plate", 0.5)
    r.walk("to the relay cradle", 6.8, 0.1, 108.8, tolerance=1)
    r.add("repair and save at the cradle", 0.3, buttons=["South"])
    r.add("release cradle", 1)
    # She stands at (3,129): walking in past the open seal starts the talk with her in frame.
    r.walk("through the open seal", 0, 0.1, 120, tolerance=1)
    # The hall's whole width starts the talk, so it's a walk with no point to reach: it ends when she speaks, and fails if she doesn't.
    r.add("walk up until she speaks", 5, y=1, until="ui:dialogue")
    r.add("she speaks", 3, until="ui:dialogue", expectedUi="dialogue")
    r.read("the captain", 6, flag="relay.hessaConfronted")
    r.lead("Taren")
    r.walk("onto the Great Lantern's floor", 1, 0.1, 125.5, tolerance=1.5)
    r.press("lock Hessa", "LeftShoulder", 0.2)
    stop = "flag:bossdown.HessaKorl"
    for i in range(10):
        r.add(f"Cleave Hessa round {i + 1}", 0.2, buttons=["RightShoulder", "South"], stopWhen=stop)
        r.add(f"release Cleave Hessa round {i + 1}", 0.4)
        r.add(f"Pulse Hessa round {i + 1}", 0.2, buttons=["RightShoulder", "West"], stopWhen=stop)
        r.add(f"release Pulse Hessa round {i + 1}", 0.4)
        r.add(f"fight Hessa round {i + 1}", 35, buttons=["South"], pulseSeconds=0.46, navigate=True, approachTarget=True, tolerance=2.3, rangedTolerance=12,
              evadeTelegraphs=True, stopWhen=stop, point=dict(x=0, y=0, z=0))
        r.gel(f"Hessa {i + 1}", stop)
    r.add("Hessa yields", 4, until=stop, expectedFlag="bossdown.HessaKorl")
    # She waits for the party to get up, then says her piece unasked: the same body, where she stopped fighting.
    r.add("reach the fallen hero after Hessa", 16, navigate=True, approachPartner=True, tolerance=1.2, stopWhen="partyAlive", point=dict(x=0, y=0, z=0))
    r.add("both heroes ready after Hessa", 6, until="partyAlive")
    r.add("Hessa speaks", 10, until="ui:dialogue", expectedUi="dialogue")
    r.read("Hessa under guard", 4, flag="relay.hessaYielded")
    r.lead("Taren")
    r.add("repair gel after Hessa", 0.2, leftTrigger=1)
    r.add("release gel after Hessa", 0.4)
    r.at = None
    r.path("to the relay lift", [(-2.4, 0.1, 155.1), (-5, 0.1, 158)], tolerance=1)
    r.scene = "Lanternwick"
    r.add("ride the relay lift", 4, buttons=["South"], expectedUi="world", expectedForm="Natural")
    r.add("release lift", 0.5)
    r.write()


def home():
    r = Route("chapter-16-lantern-home", "Chapter 2: the side quests' returns, Aumi's chart, the road back, Abri")
    r.resume("Lanternwick", "bossdown.HessaKorl", spawn=(0, 92))
    r.lead("Taren")
    r.walk("to Ishen", 3, 0.2, 90, tolerance=1)
    r.talk("Ishen's plates", 6, flag="ishen.recordRestored")
    r.path("down to the stair", [(5.6, 0.2, 42.5), (6.9, 0.2, 36.3), (17.1, 1, 25.5), (18.9, 1.5, 25.5), (19.4, 3.2, 49.5), (18.9, 3.1, 51.3), (20, 3.1, 53.5)])
    r.talk("Aumi's chart", 9, flag="lanternwick.chartGiven")
    r.path("down from the stair", [(18.9, 3.1, 51.3), (19.4, 3.2, 49.5), (18.9, 1.5, 25.5), (17.1, 1, 25.5), (7, 0.2, 36)])
    r.talk("Pell's lamp back", 3, flag="pell.lampReturned")
    r.path("to the orchard shed", [(-5.5, 0.1, 49.5), (-15.8, 0.2, 51.9), (-21.5, 1.6, 55.2), (-30, 1.5, 60)])
    r.talk("Brekk's rations", 6, flag="unsworn.rationsDelivered")
    r.path("back to the market", [(-28.5, 1.5, 55.2), (-27.5, 1.5, 48.8), (-8.3, 0.2, 44.1), (-5.5, 0.2, 38.4), (-2.5, 0.2, 27.8), (-4, 0.2, 17)])
    r.add("start Corra's thanks", 0.2, buttons=["South"], expectedUi="dialogue")
    r.add("release start Corra's thanks", 0.3, expectedUi="dialogue")
    r.add("read Corra's thanks", 1.9, stopWhen="ui:shop")
    r.add("her shop opens", 12, buttons=["South"], pulseSeconds=1.0, until="ui:shop", expectedUi="shop")
    r.add("look over her stock", 1, expectedUi="shop")
    r.add("leave her stall", 0.5, buttons=["East"], expectedUi="world")
    r.add("release her stall", 0.3)
    r.walk("to the courier pad", 0, 0.4, -19, tolerance=1)
    r.scene = "WickReach"
    r.add("launch from Lanternwick", 4, buttons=["South"], expectedForm="Flight")
    r.add("release launch", 0.5)
    r.at, r.speed, r.brake = (0, 274), FLIGHT_SPEED, False
    r.walk("down the reach", 0, 1, 150, tolerance=2)
    r.walk("to the far end", -6, 1, 30, tolerance=3)
    r.walk("toward Tallow's warp", -12, 1, 6, tolerance=3)
    r.brake = True
    r.walk("settle at Tallow's warp", -13, 1, 0, tolerance=1)
    r.scene = "TallowApproach"
    r.add("warp home", 4, buttons=["South"], expectedForm="CivilFlight")
    r.add("release warp", 0.5)
    r.at, r.brake = (17, -21), False
    r.walk("clear of the warp", 3, 1, -10, tolerance=3)
    r.brake = True
    r.walk("to Tallow's dock", 0, 1, -4.2, tolerance=0.65)
    r.scene = "TallowDrift"
    r.add("dock at Tallow Drift", 3, buttons=["South"], expectedForm="Natural")
    r.add("release dock", 0.5)
    r.speed, r.brake = CIVIL_SPEED, False
    r.at = (0, -10)
    r.walk("through the pressure cabin", 0, 0, 1)
    r.walk("to Keeper Abri", 0, 0, 2, tolerance=0.3)
    r.talk("Abri's homecoming", 6, flag="chapter2.homecoming")
    r.wait("chapter two closes", 3, expectedFlag="chapter2.complete")
    r.write()


if __name__ == "__main__":
    wick_reach()
    lanternwick()
    gorge()
    relay()
    home()
