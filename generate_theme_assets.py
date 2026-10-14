"""
Generates the authentic 16-color EGA retro pixel-art spritesheets for JumpRunShoot.
All sprites are rendered on a 16x16 grid (or multiples) with RGBA transparency.
"""

import os
from PIL import Image

# 16-color standard IBM EGA palette
EGA = {
    0:  (0, 0, 0, 255),        # Black
    1:  (0, 0, 170, 255),      # Blue
    2:  (0, 170, 0, 255),      # Green
    3:  (0, 170, 170, 255),    # Cyan
    4:  (170, 0, 0, 255),      # Red
    5:  (170, 0, 170, 255),    # Magenta
    6:  (170, 85, 0, 255),     # Brown
    7:  (170, 170, 170, 255),  # Light Gray
    8:  (85, 85, 85, 255),     # Dark Gray
    9:  (85, 85, 255, 255),    # Bright Blue
    10: (85, 255, 85, 255),    # Bright Green
    11: (85, 255, 255, 255),   # Bright Cyan
    12: (255, 85, 85, 255),    # Bright Red
    13: (255, 85, 255, 255),   # Bright Magenta
    14: (255, 255, 85, 255),   # Yellow
    15: (255, 255, 255, 255),  # Bright White
    -1: (0, 0, 0, 0)           # Transparent
}

def create_sprite(matrix):
    """Creates a PIL Image from a 2D matrix of EGA color indices."""
    h = len(matrix)
    w = len(matrix[0])
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for y in range(h):
        for x in range(w):
            c_idx = matrix[y][x]
            img.putpixel((x, y), EGA[c_idx])
    return img

# ==========================================
# 1. TILES (16x16 each)
# ==========================================
def generate_tiles():
    # Tile 0: Solid Industrial Beam
    # Beveled metallic block with rivets
    t_solid = [
        [15]*16,
        [15] + [7]*14 + [8],
        [15, 7, 15, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 15, 7, 8],
        [15, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 7, 7, 7, 8],
        [15, 7, 7, 8, 7, 7, 7, 7, 7, 7, 7, 7, 8, 7, 7, 8],
        [15, 7, 7, 8, 7, 7, 7, 7, 7, 7, 7, 7, 8, 7, 7, 8],
        [15, 7, 7, 8, 7, 7, 7, 7, 7, 7, 7, 7, 8, 7, 7, 8],
        [15, 7, 7, 8, 7, 7, 7, 7, 7, 7, 7, 7, 8, 7, 7, 8],
        [15, 7, 7, 8, 7, 7, 7, 7, 7, 7, 7, 7, 8, 7, 7, 8],
        [15, 7, 7, 8, 7, 7, 7, 7, 7, 7, 7, 7, 8, 7, 7, 8],
        [15, 7, 7, 8, 7, 7, 7, 7, 7, 7, 7, 7, 8, 7, 7, 8],
        [15, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 7, 7, 7, 8],
        [15, 7, 15, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 15, 7, 8],
        [15] + [7]*14 + [8],
        [15] + [8]*15,
        [8]*16
    ]

    # Tile 1: One-Way Hazard Platform (Yellow & Red diagonal hazard stripes)
    t_oneway = []
    for y in range(16):
        row = []
        if y < 8:
            for x in range(16):
                if y == 0:
                    row.append(12) # Bright Red top border
                elif y == 7:
                    row.append(4)  # Red bottom border
                else:
                    # Diagonal stripe pattern
                    stripe = (x + y) % 6
                    row.append(14 if stripe < 3 else 12) # Yellow vs Bright Red
        else:
            row = [-1]*16 # Transparent lower half
        t_oneway.append(row)

    # Tile 2: Hazard Spikes (Sharp steel spikes)
    t_spikes = []
    for y in range(16):
        row = []
        for x in range(16):
            if y < 6:
                row.append(-1)
            elif y == 6:
                row.append(15 if x in [2, 6, 10, 14] else -1)
            elif y == 7:
                row.append(15 if x in [1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15] else -1)
            elif y in [8, 9]:
                row.append(7 if x in [1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15] else -1)
            elif y in [10, 11, 12]:
                row.append(12 if x in [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15] else -1)
            else:
                row.append(4) # Base anchor
        t_spikes.append(row)

    # Tile 3: Background Vent / Grate
    t_bg_vent = [
        [8]*16,
        [8] + [0]*14 + [8],
        [8, 0, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 0, 8],
        [8, 0, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 0, 8],
        [8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8],
        [8, 0, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 0, 8],
        [8, 0, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 0, 8],
        [8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8],
        [8, 0, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 0, 8],
        [8, 0, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 0, 8],
        [8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8],
        [8, 0, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 0, 8],
        [8, 0, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 0, 8],
        [8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8],
        [8] + [0]*14 + [8],
        [8]*16
    ]

    sheet = Image.new("RGBA", (64, 16), (0, 0, 0, 0))
    sheet.paste(create_sprite(t_solid), (0, 0))
    sheet.paste(create_sprite(t_oneway), (16, 0))
    sheet.paste(create_sprite(t_spikes), (32, 0))
    sheet.paste(create_sprite(t_bg_vent), (48, 0))
    return sheet

# ==========================================
# 2. PLAYER (16x24 each)
# ==========================================
def generate_player():
    # 16x24 hero: Helmet/Hair, Visor, Torso/Vest, Belt, Pants, Boots, Blaster
    def make_player_frame(leg_phase, arm_phase):
        m = [[-1]*16 for _ in range(24)]
        # Helmet / Hair (Yellow with highlights)
        for y in range(1, 6):
            for x in range(4, 12):
                m[y][x] = 14
        m[1][5] = 15; m[1][6] = 15
        
        # Visor / Face (Bright Cyan visor on dark face)
        for y in range(6, 10):
            for x in range(5, 12):
                m[y][x] = 6 # skin / helmet rim
        m[7][8] = 11; m[7][9] = 11; m[7][10] = 15; m[7][11] = 11 # glowing cyan visor
        m[8][8] = 3;  m[8][9] = 3;  m[8][10] = 11; m[8][11] = 3

        # Torso / Vest (Bright Red muscle vest)
        for y in range(10, 16):
            for x in range(4, 12):
                m[y][x] = 12
        m[10][6] = 15; m[11][6] = 15 # collar highlight
        
        # Belt
        for x in range(4, 12):
            m[15][x] = 8
        m[15][7] = 14; m[15][8] = 14 # golden buckle

        # Arms & Blaster
        if arm_phase == "shoot":
            # Raised blaster pointing right
            for x in range(11, 16):
                m[11][x] = 7; m[12][x] = 8
            m[11][15] = 11 # muzzle tip
        else:
            # Gun held forward-low
            for x in range(10, 15):
                m[13][x] = 7; m[14][x] = 8
            m[13][14] = 11

        # Blue Pants & Boots
        # Leg phases: 0=idle, 1=walk1, 2=walk2, 3=jump
        if leg_phase == 0:
            for y in range(16, 21):
                m[y][5] = 9; m[y][6] = 1; m[y][9] = 9; m[y][10] = 1
            for y in range(21, 24):
                m[y][4] = 8; m[y][5] = 7; m[y][6] = 7
                m[y][9] = 8; m[y][10] = 7; m[y][11] = 7
        elif leg_phase == 1: # Left leg forward
            for y in range(16, 20):
                m[y][4] = 9; m[y][5] = 1; m[y][10] = 9; m[y][11] = 1
            for y in range(20, 23):
                m[y][3] = 7; m[y][4] = 7; m[y][5] = 8
            for y in range(21, 24):
                m[y][10] = 7; m[y][11] = 8
        elif leg_phase == 2: # Right leg forward
            for y in range(16, 20):
                m[y][6] = 9; m[y][7] = 1; m[y][8] = 9; m[y][9] = 1
            for y in range(21, 24):
                m[y][5] = 7; m[y][6] = 8
            for y in range(20, 23):
                m[y][9] = 7; m[y][10] = 7; m[y][11] = 8
        elif leg_phase == 3: # Jump / Air
            for y in range(16, 19):
                m[y][4] = 9; m[y][5] = 1; m[y][9] = 9; m[y][10] = 1
            for y in range(19, 22):
                m[y][3] = 7; m[y][4] = 8; m[y][10] = 7; m[y][11] = 8

        return m

    sheet = Image.new("RGBA", (16 * 5, 24), (0, 0, 0, 0))
    sheet.paste(create_sprite(make_player_frame(0, "normal")), (0, 0))    # Idle
    sheet.paste(create_sprite(make_player_frame(1, "normal")), (16, 0))   # Walk 1
    sheet.paste(create_sprite(make_player_frame(2, "normal")), (32, 0))   # Walk 2
    sheet.paste(create_sprite(make_player_frame(3, "normal")), (48, 0))   # Jump/Air
    sheet.paste(create_sprite(make_player_frame(0, "shoot")),  (64, 0))   # Shoot
    return sheet

# ==========================================
# 3. MONSTERS (Fixed behaviors, 5 distinct profiles)
# ==========================================
def generate_monsters():
    sheet = Image.new("RGBA", (16 * 8, 32), (0, 0, 0, 0))

    # Monster 1: Patrol Droid (16x16, 1x1, Ground)
    # Rover tread + armored dome + red scanning eye
    def make_patrol_droid(frame):
        m = [[-1]*16 for _ in range(16)]
        for y in range(4, 11):
            for x in range(3, 13):
                m[y][x] = 8 # Dark Gray armor
        for x in range(4, 12):
            m[3][x] = 7     # Top light highlight
        # Red eye
        eye_x = 10 if frame == 0 else 11
        m[6][eye_x] = 12; m[6][eye_x-1] = 15; m[7][eye_x] = 4
        # Treads
        for x in range(2, 14):
            m[12][x] = 7
            m[13][x] = 8 if (x + frame) % 2 == 0 else 0
            m[14][x] = 7
            m[15][x] = 0
        return m

    # Monster 2: Hover Drone (16x16, 1x1, Flying)
    # Orb core + twin side thrusters + sensor dome
    def make_hover_drone(frame):
        m = [[-1]*16 for _ in range(16)]
        for y in range(4, 12):
            for x in range(4, 12):
                m[y][x] = 3 # Cyan core
        for x in range(5, 11):
            m[3][x] = 11 # Highlight
        # Side thrusters
        for y in range(5, 11):
            m[y][1] = 7; m[y][2] = 8; m[y][13] = 8; m[y][14] = 7
        # Sensor visor
        m[7][7] = 14; m[7][8] = 15; m[8][7] = 14; m[8][8] = 14 # Yellow sensor
        # Jet exhaust flicker
        if frame == 0:
            m[11][1] = 12; m[12][1] = 14; m[11][14] = 12; m[12][14] = 14
        else:
            m[11][1] = 14; m[12][1] = 12; m[11][14] = 14; m[12][14] = 12
        return m

    # Monster 3: Ceiling Turret (16x16, 1x1, Stationary)
    # Ceiling bracket + swivel cannon pod
    def make_ceiling_turret(frame):
        m = [[-1]*16 for _ in range(16)]
        # Ceiling bracket
        for x in range(1, 15):
            m[0][x] = 7; m[1][x] = 8
        # Swivel ball
        for y in range(2, 9):
            for x in range(4, 12):
                m[y][x] = 8
        m[3][7] = 12; m[3][8] = 15 # Red targeting diode
        # Twin downward barrels
        for y in range(9, 14):
            m[y][5] = 7; m[y][6] = 8; m[y][9] = 7; m[y][10] = 8
        if frame == 1: # Muzzle spark
            m[14][5] = 14; m[14][6] = 12; m[14][9] = 14; m[14][10] = 12
        return m

    # Monster 4: Mecha-Hound (32x16, 2x1, Fast Ground Charger)
    def make_mecha_hound(frame):
        m = [[-1]*32 for _ in range(16)]
        # Head (facing right)
        for y in range(4, 9):
            for x in range(20, 29):
                m[y][x] = 8
        m[5][27] = 12; m[5][28] = 15 # Red optic
        # Jaws
        for x in range(24, 30):
            m[8][x] = 7
        # Spine & Body
        for y in range(5, 11):
            for x in range(7, 21):
                m[y][x] = 8
        m[4][10] = 7; m[4][14] = 7; m[4][18] = 7 # Armor ridges
        # Legs (Running cycle)
        if frame == 0:
            for y in range(11, 16):
                m[y][5] = 7; m[y][11] = 8; m[y][17] = 7; m[y][23] = 8
        else:
            for y in range(11, 16):
                m[y][7] = 8; m[y][9] = 7; m[y][19] = 8; m[y][21] = 7
        return m

    # Monster 5: Heavy Guard (16x32, 1x2, Tall Armored Enforcer)
    def make_heavy_guard(frame):
        m = [[-1]*16 for _ in range(32)]
        # Helmet & Visor
        for y in range(2, 8):
            for x in range(4, 12):
                m[y][x] = 8
        m[4][8] = 12; m[4][9] = 15; m[4][10] = 12 # Red mono-eye
        # Armored Torso & Shoulder Cannon
        for y in range(8, 18):
            for x in range(3, 13):
                m[y][x] = 4 # Dark Red armor plate
        m[6][1] = 7; m[6][2] = 8; m[7][1] = 7; m[7][2] = 8 # Shoulder gun mount
        for y in range(9, 17):
            m[y][6] = 12; m[y][7] = 12 # Chest power core
        # Belt
        for x in range(3, 13):
            m[18][x] = 8
        # Heavy Legs
        leg_offset = 1 if frame == 1 else 0
        for y in range(19, 28):
            m[y][4] = 8; m[y][5] = 7; m[y][6] = 7
            m[y][9] = 8; m[y][10] = 7; m[y][11] = 7
        for y in range(28, 32):
            m[y][3] = 8; m[y][4] = 8; m[y][5] = 7
            m[y][9] = 8; m[y][10] = 8; m[y][11] = 7
        return m

    # Paste into monsters sheet
    sheet.paste(create_sprite(make_patrol_droid(0)), (0, 0))
    sheet.paste(create_sprite(make_patrol_droid(1)), (16, 0))

    sheet.paste(create_sprite(make_hover_drone(0)), (32, 0))
    sheet.paste(create_sprite(make_hover_drone(1)), (48, 0))

    sheet.paste(create_sprite(make_ceiling_turret(0)), (64, 0))
    sheet.paste(create_sprite(make_ceiling_turret(1)), (80, 0))

    # Mecha-Hound (32x16) at row 16
    sheet.paste(create_sprite(make_mecha_hound(0)), (0, 16))
    sheet.paste(create_sprite(make_mecha_hound(1)), (32, 16))

    # Heavy Guard (16x32) at column 96
    sheet.paste(create_sprite(make_heavy_guard(0)), (96, 0))
    sheet.paste(create_sprite(make_heavy_guard(1)), (112, 0))

    return sheet

# ==========================================
# 4. ITEMS & POWERUPS (16x16 each)
# ==========================================
def generate_items():
    # Gem Small (+100 pts)
    gem_small = [[-1]*16 for _ in range(16)]
    for y in range(4, 12):
        span = 3 - abs(y - 7)
        for x in range(8 - span, 8 + span + 1):
            gem_small[y][x] = 11 if x <= 8 else 3
    gem_small[5][7] = 15

    # Gem Medium (+250 pts)
    gem_med = [[-1]*16 for _ in range(16)]
    for y in range(3, 13):
        span = 4 - abs(y - 7)
        for x in range(8 - span, 8 + span + 1):
            gem_med[y][x] = 10 if x <= 8 else 2
    gem_med[4][7] = 15

    # Relic Large (+500 pts)
    relic_large = [[-1]*16 for _ in range(16)]
    for y in range(3, 13):
        for x in range(4, 12):
            relic_large[y][x] = 14
    for x in range(5, 11):
        relic_large[3][x] = 15
        relic_large[12][x] = 6
    relic_large[7][7] = 15; relic_large[7][8] = 15 # Center sparkle

    # Med-Kit (+2 HP)
    med_kit = [[-1]*16 for _ in range(16)]
    for y in range(4, 12):
        for x in range(3, 13):
            med_kit[y][x] = 15 # White body
    # Red cross
    for y in range(6, 10):
        med_kit[y][7] = 12; med_kit[y][8] = 12
    for x in range(5, 11):
        med_kit[7][x] = 12; med_kit[8][x] = 12

    # Powerup: Rapid Fire (Red badge with 'R')
    pw_rapid = [[-1]*16 for _ in range(16)]
    for y in range(3, 13):
        for x in range(3, 13):
            pw_rapid[y][x] = 12 # Bright Red
    # 'R'
    r_pixels = [(5,5),(5,6),(5,7),(6,5),(6,8),(7,5),(7,6),(7,7),(8,5),(8,7),(9,5),(9,8)]
    for ry, rx in r_pixels:
        pw_rapid[ry][rx] = 15

    # Powerup: High Jump Boots (Green badge with 'J')
    pw_jump = [[-1]*16 for _ in range(16)]
    for y in range(3, 13):
        for x in range(3, 13):
            pw_jump[y][x] = 10 # Bright Green
    # 'J'
    j_pixels = [(5,6),(5,7),(5,8),(6,7),(7,7),(8,7),(9,5),(9,6),(9,7)]
    for jy, jx in j_pixels:
        pw_jump[jy][jx] = 15

    # Powerup: Speed Sneakers (Cyan badge with 'S')
    pw_speed = [[-1]*16 for _ in range(16)]
    for y in range(3, 13):
        for x in range(3, 13):
            pw_speed[y][x] = 11 # Bright Cyan
    # 'S'
    s_pixels = [(5,6),(5,7),(5,8),(6,5),(7,6),(7,7),(8,8),(9,5),(9,6),(9,7)]
    for sy, sx in s_pixels:
        pw_speed[sy][sx] = 0 # Black 'S'

    sheet = Image.new("RGBA", (16 * 7, 16), (0, 0, 0, 0))
    sheet.paste(create_sprite(gem_small), (0, 0))
    sheet.paste(create_sprite(gem_med), (16, 0))
    sheet.paste(create_sprite(relic_large), (32, 0))
    sheet.paste(create_sprite(med_kit), (48, 0))
    sheet.paste(create_sprite(pw_rapid), (64, 0))
    sheet.paste(create_sprite(pw_jump), (80, 0))
    sheet.paste(create_sprite(pw_speed), (96, 0))
    return sheet

# ==========================================
# 5. PROJECTILES (16x16 each)
# ==========================================
def generate_projectiles():
    sheet = Image.new("RGBA", (16 * 3, 16), (0, 0, 0, 0))

    # Base Blaster (Bright Cyan energy capsule)
    p_base = [[-1]*16 for _ in range(16)]
    for y in range(6, 10):
        for x in range(3, 13):
            p_base[y][x] = 11 # Bright Cyan
    for x in range(5, 11):
        p_base[7][x] = 15; p_base[8][x] = 15 # White hot core

    # Upgraded Plasma Shot (Yellow & Bright Red fiery blast)
    p_plasma = [[-1]*16 for _ in range(16)]
    for y in range(5, 11):
        for x in range(2, 14):
            p_plasma[y][x] = 12 # Bright Red
    for y in range(6, 10):
        for x in range(4, 12):
            p_plasma[y][x] = 14 # Yellow
    for x in range(6, 10):
        p_plasma[7][x] = 15; p_plasma[8][x] = 15

    # Monster Laser Bead (Red energy orb)
    p_enemy = [[-1]*16 for _ in range(16)]
    for y in range(5, 11):
        for x in range(5, 11):
            p_enemy[y][x] = 4 # Red
    for y in range(6, 10):
        for x in range(6, 10):
            p_enemy[y][x] = 12 # Bright Red
    p_enemy[7][7] = 15; p_enemy[7][8] = 15

    sheet.paste(create_sprite(p_base), (0, 0))
    sheet.paste(create_sprite(p_plasma), (16, 0))
    sheet.paste(create_sprite(p_enemy), (32, 0))
    return sheet

def main():
    target_dir = r"c:\work\jump-run-shoot\wwwroot\themes\default"
    os.makedirs(target_dir, exist_ok=True)

    tiles = generate_tiles()
    player = generate_player()
    monsters = generate_monsters()
    items = generate_items()
    projectiles = generate_projectiles()

    tiles.save(os.path.join(target_dir, "tiles.png"))
    player.save(os.path.join(target_dir, "player.png"))
    monsters.save(os.path.join(target_dir, "monsters.png"))
    items.save(os.path.join(target_dir, "items.png"))
    projectiles.save(os.path.join(target_dir, "projectiles.png"))

    # Also build an enlarged 4x Preview Sheet so the user can easily see everything at once!
    preview = Image.new("RGBA", (320, 200), (12, 13, 20, 255)) # Dark arcade background
    preview.paste(tiles, (10, 10))
    preview.paste(player, (10, 40))
    preview.paste(items, (10, 80))
    preview.paste(projectiles, (10, 110))
    preview.paste(monsters, (10, 140))

    # Scale 4x using nearest neighbor for authentic crisp pixel preview
    preview_4x = preview.resize((preview.width * 4, preview.height * 4), Image.NEAREST)
    preview_path = os.path.join(target_dir, "sprites_preview.png")
    preview_4x.save(preview_path)

    # Also copy to artifact directory for easy embedding in artifact
    artifact_preview = r"C:\Users\stefa\.gemini\antigravity\brain\e393e5b5-f59b-4c75-a37a-1750dc137437\sprites_preview.png"
    preview_4x.save(artifact_preview)

    print("All theme spritesheets and 4x preview generated successfully!")

if __name__ == "__main__":
    main()
