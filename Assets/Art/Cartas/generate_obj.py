import math

def generate_rounded_box_obj(filename, width_x, length_z, depth_y, corner_radius, num_segments=8):
    vertices = []
    faces = []
    
    # 2D profile on X-Z plane
    profile_points = []
    # top right (X+, Z+)
    cx = width_x/2 - corner_radius
    cz = length_z/2 - corner_radius
    for i in range(num_segments + 1):
        angle = math.pi/2 * i / num_segments
        profile_points.append((cx + corner_radius * math.cos(angle), cz + corner_radius * math.sin(angle)))
    # top left (X-, Z+)
    cx = -width_x/2 + corner_radius
    for i in range(num_segments + 1):
        angle = math.pi/2 + math.pi/2 * i / num_segments
        profile_points.append((cx + corner_radius * math.cos(angle), cz + corner_radius * math.sin(angle)))
    # bottom left (X-, Z-)
    cz = -length_z/2 + corner_radius
    for i in range(num_segments + 1):
        angle = math.pi + math.pi/2 * i / num_segments
        profile_points.append((cx + corner_radius * math.cos(angle), cz + corner_radius * math.sin(angle)))
    # bottom right (X+, Z-)
    cx = width_x/2 - corner_radius
    for i in range(num_segments + 1):
        angle = 3*math.pi/2 + math.pi/2 * i / num_segments
        profile_points.append((cx + corner_radius * math.cos(angle), cz + corner_radius * math.sin(angle)))

    # Extrude along Y
    # Top face vertices (Y+)
    for p in profile_points:
        vertices.append((p[0], depth_y/2, p[1]))
    # Bottom face vertices (Y-)
    for p in profile_points:
        vertices.append((p[0], -depth_y/2, p[1]))
        
    num_pts = len(profile_points)
    
    # Faces
    # Top face (fan triangulation from center)
    center_top = (0, depth_y/2, 0)
    vertices.append(center_top)
    ct_idx = len(vertices)
    for i in range(num_pts):
        nxt = (i + 1) % num_pts
        faces.append((ct_idx, nxt + 1, i + 1)) # standard unity winding for top
        
    # Bottom face (fan triangulation from center)
    center_bottom = (0, -depth_y/2, 0)
    vertices.append(center_bottom)
    cb_idx = len(vertices)
    for i in range(num_pts):
        nxt = (i + 1) % num_pts
        faces.append((cb_idx, num_pts + i + 1, num_pts + nxt + 1)) # reversed winding for bottom
        
    # Side faces
    for i in range(num_pts):
        nxt = (i + 1) % num_pts
        # Quad from top to bottom
        v_top1 = i + 1
        v_top2 = nxt + 1
        v_bot2 = num_pts + nxt + 1
        v_bot1 = num_pts + i + 1
        
        faces.append((v_top1, v_top2, v_bot2))
        faces.append((v_top1, v_bot2, v_bot1))
        
    with open(filename, 'w') as f:
        for v in vertices:
            f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
        # normal generation can be handled by Unity, or we can generate them. Unity does it automatically.
        for face in faces:
            f.write(f"f {face[0]} {face[1]} {face[2]}\n")

# Dimensions in Unity: 1x1x1. We round X and Z.
# Since it's a cube, corner radius of 0.05 is 5% of the total width.
generate_rounded_box_obj('C:/Users/Tomas Abrate/Trucardo/Assets/Art/Cartas/RoundedCard.obj', 1.0, 1.0, 1.0, 0.08, 12)
