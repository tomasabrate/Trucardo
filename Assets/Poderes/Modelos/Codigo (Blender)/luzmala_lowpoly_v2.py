r"""
LUZ MALA  (llama espectral low poly translúcida + chispas flotantes)

Unidades: metros, tamaño pensada para agarrar en la mano (~16.5 cm de alto,
~10.8 cm de ancho y ~9.4 cm de profundidad). Eje Z hacia arriba.
La llama mira hacia -Y (vista frontal de Blender, tecla 1 del teclado numérico,
igual que en la imagen de referencia). El origen (0, 0, 0) está en el centro
de la base plana, apoyada exactamente en z = 0.

Pensada para colocarle un Point Light adentro en Unity (aprox. en z = 6 cm):
los 5 materiales están configurados como translúcidos/transparentes (Alpha < 1,
Transmission y leve emisión) con el gradiente celeste, menta, blanco y azul de
la referencia.

Un solo objeto "LuzMala" con 5 materiales translúcidos (flat shading, sin
texturas externas):
    Nucleo_Blanco, Llama_Menta, Llama_Celeste, Llama_Turquesa, Base_Azul.

USO EN BLENDER (2.8 o superior):
    Pestaña Scripting > Open > este archivo > Run Script (Alt+P).
    Para exportar a Unity, poné una ruta en FBX_PATH más abajo, por ejemplo:
    FBX_PATH = "C:/Users/TuUsuario/Desktop/LuzMala.fbx"

Fuera de Blender (python luz_mala_lowpoly.py) solo imprime un resumen.
"""
import math
import os
import random

# ----------------------------------------------------------------- ajustes
SEED = 23
MAIN_SIDES = 12         # lados del cuerpo central de la llama
TONGUE_SIDES = 6        # lados de cada lengua de fuego secundaria

FBX_PATH = r"C:\Users\Tomas Abrate\Downloads\Poderes Modelos 3D\Modelos 3D\LuzMala.fbx"

# (nombre, color_sRGB, rugosidad, alpha_opacidad, transmision, emision)
MATERIALS = [
    ("Nucleo_Blanco",  "#EAFFFF", 0.15, 0.88, 0.55, 0.45),
    ("Llama_Menta",    "#8DF5DC", 0.20, 0.74, 0.70, 0.25),
    ("Llama_Celeste",  "#7BE0F7", 0.22, 0.66, 0.75, 0.20),
    ("Llama_Turquesa", "#4CA8BE", 0.28, 0.60, 0.78, 0.12),
    ("Base_Azul",      "#4682A6", 0.35, 0.72, 0.65, 0.08),
]
M_CORE, M_MINT, M_CYAN, M_TEAL, M_BLUE = 0, 1, 2, 3, 4


# ------------------------------------------------------------- utilidades
def hex_to_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def add(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def scale(a, s):
    return (a[0] * s, a[1] * s, a[2] * s)


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0])


def dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def unit(a):
    l = math.sqrt(dot(a, a)) or 1.0
    return (a[0] / l, a[1] / l, a[2] / l)


def face_normal(a, b, c):
    return unit(cross(sub(b, a), sub(c, a)))


def signed_volume(verts, faces):
    return sum(dot(verts[a], cross(verts[b], verts[c])) for a, b, c in faces) / 6.0


def _append_component(all_v, all_f, all_m, v, f, m):
    if signed_volume(v, f) < 0:
        f = [(a, c, b) for a, b, c in f]
    base = len(all_v)
    all_v.extend(v)
    all_f.extend((a + base, b + base, c + base) for a, b, c in f)
    all_m.extend(m)


def _pick_flame_material(cen, n, role, rnd):
    """Asigna el material imitando el gradiente radial y vertical de la imagen."""
    x, y, z = cen
    if role == "shard":
        return M_CYAN if z > 0.085 else (M_TEAL if z > 0.030 else M_BLUE)

    # Base facetada azulada
    if z < 0.016 or (z < 0.028 and n[2] < -0.15):
        return M_BLUE

    # Núcleo blanco brillante en la zona frontal-inferior central
    if role in ("main", "inner") and (0.016 <= z <= 0.056) and abs(x) < 0.020 and y < -0.008:
        if n[1] < -0.25 or z < 0.042:
            return M_CORE

    # Zona media verde menta / aguamarina
    if role in ("main", "inner") and (0.020 <= z <= 0.105) and abs(x) < 0.030 and y < 0.008:
        if n[1] < -0.10 or rnd.random() < 0.65:
            return M_MINT

    # Bordes bajos y lenguas exteriores en azul/turquesa
    if z < 0.036 and abs(x) > 0.028:
        return M_BLUE
    if role == "outer":
        return M_CYAN if (z > 0.090 or n[2] > 0.35) else M_TEAL

    # Parte alta de la llama principal en celeste brillante con facetas turquesa
    if z > 0.105:
        return M_CYAN if (n[1] < 0.15 or n[0] < 0.25) else M_TEAL

    return M_CYAN if (n[1] < 0.0 or rnd.random() < 0.55) else M_TEAL


# ----------------------------------------- constructores geométricos cerrados
def build_main_flame(rnd):
    """Cuerpo central de la llama con crestas facetadas y curva en S hasta la cima."""
    # (z, cx, cy, rx, ry, twist_deg, stag)
    levels = [
        (0.000,  0.000, 0.000, 0.015, 0.014,   0.0, False),  # apoyo plano en z = 0
        (0.010,  0.000, 0.000, 0.031, 0.028,   8.0, True),   # bisel inferior
        (0.024,  0.000, 0.000, 0.040, 0.036,   0.0, False),  # panza baja ancha
        (0.042, -0.001, 0.000, 0.042, 0.037,  10.0, True),   # panza media
        (0.062, -0.003, 0.000, 0.036, 0.032,   2.0, False),  # cintura media
        (0.084, -0.002, 0.000, 0.030, 0.026,  14.0, True),   # estrechamiento
        (0.106, -0.006, 0.000, 0.023, 0.020,   5.0, False),  # curva hacia la izquierda
        (0.126, -0.008, 0.000, 0.017, 0.015,  16.0, True),   # quiebre alto izquierdo
        (0.144, -0.002, 0.000, 0.011, 0.010,   6.0, False),  # recuperación hacia la punta
        (0.156,  0.004, 0.000, 0.005, 0.005,  15.0, True),   # pre-punta
    ]
    tip_pos = (0.009, 0.000, 0.165)  # punta superior curvada a la derecha

    verts = [(0.0, 0.0, 0.0)]  # índice 0: centro de la base en z = 0
    rings = []

    for li, (z, cx, cy, rx, ry, twist_deg, stag) in enumerate(levels):
        start = len(verts)
        twist = math.radians(twist_deg) + (math.pi / MAIN_SIDES if stag else 0.0)
        for k in range(MAIN_SIDES):
            ang = 2.0 * math.pi * k / MAIN_SIDES + twist
            # Alternar crestas y valles para acentuar el tallado low poly
            if li == 0:
                lobe = 1.0
            else:
                lobe = 1.08 if (k % 2 == 0) else 0.88
                lobe += rnd.uniform(-0.03, 0.03)
            px = cx + rx * lobe * math.cos(ang)
            py = cy + ry * lobe * math.sin(ang)
            verts.append((px, py, z))
        rings.append(start)

    tip_idx = len(verts)
    verts.append(tip_pos)

    faces, fmats = [], []

    # Tapa plana inferior en z = 0
    r0 = rings[0]
    for k in range(MAIN_SIDES):
        kn = (k + 1) % MAIN_SIDES
        faces.append((0, r0 + kn, r0 + k))
        fmats.append(M_BLUE)

    # Bandas facetadas entre niveles
    for i in range(len(levels) - 1):
        ra, rb = rings[i], rings[i + 1]
        stag_a = levels[i][6]
        for k in range(MAIN_SIDES):
            kn = (k + 1) % MAIN_SIDES
            a, b = ra + k, ra + kn
            c, d = rb + kn, rb + k
            tris = [(a, b, c), (a, c, d)] if stag_a else [(a, b, d), (b, c, d)]
            for t in tris:
                va, vb, vc = verts[t[0]], verts[t[1]], verts[t[2]]
                cen = ((va[0] + vb[0] + vc[0]) / 3.0,
                       (va[1] + vb[1] + vc[1]) / 3.0,
                       (va[2] + vb[2] + vc[2]) / 3.0)
                n = face_normal(va, vb, vc)
                faces.append(t)
                fmats.append(_pick_flame_material(cen, n, "main", rnd))

    # Abanico superior hacia la punta
    rlast = rings[-1]
    for k in range(MAIN_SIDES):
        kn = (k + 1) % MAIN_SIDES
        t = (rlast + k, rlast + kn, tip_idx)
        va, vb, vc = verts[t[0]], verts[t[1]], verts[t[2]]
        cen = ((va[0] + vb[0] + vc[0]) / 3.0,
               (va[1] + vb[1] + vc[1]) / 3.0,
               (va[2] + vb[2] + vc[2]) / 3.0)
        n = face_normal(va, vb, vc)
        faces.append(t)
        fmats.append(_pick_flame_material(cen, n, "main", rnd))

    return verts, faces, fmats


def build_tongue(stations, tip_pos, role, rnd):
    """Construye una lengua de fuego secundaria cerrada terminada en punta aguda.
    stations = lista de ((cx, cy, cz), rx, ry, twist_deg)."""
    verts = [stations[0][0]]
    base_cap = 0
    rings = []

    for (cx, cy, cz), rx, ry, twist_deg in stations:
        r_start = len(verts)
        twist = math.radians(twist_deg)
        for k in range(TONGUE_SIDES):
            ang = 2.0 * math.pi * k / TONGUE_SIDES + twist
            # Sección con arista viva exterior
            sharp = 1.15 if (k % 2 == 0) else 0.82
            px = cx + rx * sharp * math.cos(ang)
            py = cy + ry * sharp * math.sin(ang)
            verts.append((px, py, cz))
        rings.append(r_start)

    tip_idx = len(verts)
    verts.append(tip_pos)

    faces, fmats = [], []

    # Cierre inferior de la lengua
    r0 = rings[0]
    for k in range(TONGUE_SIDES):
        kn = (k + 1) % TONGUE_SIDES
        faces.append((base_cap, r0 + kn, r0 + k))
        fmats.append(M_BLUE if stations[0][0][2] < 0.030 else M_TEAL)

    # Cuerpo de la lengua
    for i in range(len(stations) - 1):
        ra, rb = rings[i], rings[i + 1]
        for k in range(TONGUE_SIDES):
            kn = (k + 1) % TONGUE_SIDES
            for t in ((ra + k, ra + kn, rb + kn), (ra + k, rb + kn, rb + k)):
                va, vb, vc = verts[t[0]], verts[t[1]], verts[t[2]]
                cen = ((va[0] + vb[0] + vc[0]) / 3.0,
                       (va[1] + vb[1] + vc[1]) / 3.0,
                       (va[2] + vb[2] + vc[2]) / 3.0)
                n = face_normal(va, vb, vc)
                faces.append(t)
                fmats.append(_pick_flame_material(cen, n, role, rnd))

    # Punta aguda de la lengua
    rlast = rings[-1]
    for k in range(TONGUE_SIDES):
        kn = (k + 1) % TONGUE_SIDES
        t = (rlast + k, rlast + kn, tip_idx)
        va, vb, vc = verts[t[0]], verts[t[1]], verts[t[2]]
        cen = ((va[0] + vb[0] + vc[0]) / 3.0,
               (va[1] + vb[1] + vc[1]) / 3.0,
               (va[2] + vb[2] + vc[2]) / 3.0)
        n = face_normal(va, vb, vc)
        faces.append(t)
        fmats.append(_pick_flame_material(cen, n, role, rnd))

    return verts, faces, fmats


def build_shard(center, rx, ry, rz, tilt_deg, rnd):
    """Chispa / cristal flotante octaedral (bipirámide de 6 vértices y 8 caras)."""
    cx, cy, cz = center
    tilt = math.radians(tilt_deg)
    ct, st = math.cos(tilt), math.sin(tilt)

    local_pts = [
        (0.0, 0.0, -rz),  # 0: punta inferior
        ( rx, 0.0, 0.0),  # 1: +X
        (0.0,  ry, 0.0),  # 2: +Y
        (-rx, 0.0, 0.0),  # 3: -X
        (0.0, -ry, 0.0),  # 4: -Y
        (0.0, 0.0,  rz),  # 5: punta superior
    ]
    verts = []
    for lx, ly, lz in local_pts:
        wx = cx + (lx * ct - lz * st)
        wy = cy + ly
        wz = cz + (lx * st + lz * ct)
        verts.append((wx, wy, wz))

    faces, fmats = [], []
    for k in range(4):
        kn = (k + 1) % 4
        t_bot = (0, 1 + kn, 1 + k)
        t_top = (1 + k, 1 + kn, 5)
        for t in (t_bot, t_top):
            n = face_normal(verts[t[0]], verts[t[1]], verts[t[2]])
            faces.append(t)
            fmats.append(_pick_flame_material(center, n, "shard", rnd))

    return verts, faces, fmats


# ------------------------------------------------------------ malla completa
def build_mesh():
    rnd = random.Random(SEED)
    verts, faces, fmats = [], [], []

    # 1. Llama central principal
    v, f, m = build_main_flame(rnd)
    _append_component(verts, faces, fmats, v, f, m)

    # 2. Lenguas de fuego secundarias que arman la silueta de la imagen y el volumen 3D
    tongues_spec = [
        # Lengua alta izquierda
        ([((-0.016, -0.003, 0.048), 0.015, 0.013, 10.0),
          ((-0.025, -0.004, 0.078), 0.013, 0.011, 22.0),
          ((-0.026, -0.003, 0.104), 0.008, 0.007, 35.0)],
         (-0.021, -0.002, 0.123), "outer"),

        # Lengua alta derecha
        ([(( 0.015, -0.003, 0.045), 0.015, 0.013, -8.0),
          (( 0.028, -0.004, 0.074), 0.013, 0.011, -20.0),
          (( 0.030, -0.003, 0.096), 0.008, 0.007, -30.0)],
         ( 0.025, -0.002, 0.114), "outer"),

        # Lengua media izquierda
        ([((-0.024,  0.002, 0.028), 0.014, 0.012, 15.0),
          ((-0.039,  0.002, 0.052), 0.012, 0.010, 28.0),
          ((-0.041,  0.002, 0.070), 0.007, 0.006, 40.0)],
         (-0.037,  0.002, 0.086), "outer"),

        # Lengua media derecha
        ([(( 0.024,  0.002, 0.025), 0.014, 0.012, -15.0),
          (( 0.038,  0.002, 0.044), 0.012, 0.010, -25.0),
          (( 0.043,  0.002, 0.058), 0.007, 0.006, -35.0)],
         ( 0.046,  0.002, 0.070), "outer"),

        # Pico bajo izquierdo
        ([((-0.026, -0.004, 0.015), 0.010, 0.009, 12.0),
          ((-0.038, -0.005, 0.024), 0.007, 0.006, 25.0)],
         (-0.046, -0.005, 0.033), "outer"),

        # Pico bajo derecho
        ([(( 0.026, -0.004, 0.015), 0.010, 0.009, -12.0),
          (( 0.038, -0.005, 0.025), 0.007, 0.006, -25.0)],
         ( 0.046, -0.005, 0.036), "outer"),

        # Lengua frontal-izquierda (color menta/núcleo)
        ([((-0.010, -0.016, 0.022), 0.014, 0.011, 5.0),
          ((-0.016, -0.020, 0.048), 0.011, 0.009, 18.0),
          ((-0.014, -0.016, 0.072), 0.006, 0.005, 28.0)],
         (-0.011, -0.012, 0.088), "inner"),

        # Lengua frontal-derecha (color menta/núcleo)
        ([(( 0.010, -0.016, 0.020), 0.014, 0.011, -5.0),
          (( 0.015, -0.020, 0.045), 0.011, 0.009, -18.0),
          (( 0.013, -0.015, 0.068), 0.006, 0.005, -28.0)],
         ( 0.009, -0.011, 0.082), "inner"),

        # Lengua dorsal-izquierda (+Y, para dar volumen 3D por detrás)
        ([((-0.012,  0.016, 0.030), 0.014, 0.012, 12.0),
          ((-0.018,  0.022, 0.062), 0.011, 0.010, 24.0),
          ((-0.015,  0.018, 0.090), 0.006, 0.006, 36.0)],
         (-0.010,  0.014, 0.108), "outer"),

        # Lengua dorsal-derecha (+Y, para dar volumen 3D por detrás)
        ([(( 0.012,  0.016, 0.028), 0.014, 0.012, -12.0),
          (( 0.018,  0.022, 0.058), 0.011, 0.010, -24.0),
          (( 0.015,  0.017, 0.084), 0.006, 0.006, -36.0)],
         ( 0.011,  0.013, 0.100), "outer"),
    ]

    for stations, tip_pos, role in tongues_spec:
        v, f, m = build_tongue(stations, tip_pos, role, rnd)
        _append_component(verts, faces, fmats, v, f, m)

    # 3. Chispas / fragmentos de cristal flotantes alrededor de la llama
    shards_spec = [
        # (centro_xyz, rx, ry, rz, inclinacion_deg)
        (( 0.015,  0.000, 0.148), 0.0028, 0.0022, 0.0065,  -8.0),
        (( 0.027,  0.002, 0.126), 0.0030, 0.0022, 0.0060, -14.0),
        (( 0.019, -0.004, 0.116), 0.0020, 0.0018, 0.0032, -10.0),
        ((-0.029,  0.001, 0.120), 0.0026, 0.0020, 0.0050,  15.0),
        ((-0.045, -0.002, 0.094), 0.0032, 0.0022, 0.0072,  12.0),
        ((-0.034,  0.003, 0.090), 0.0018, 0.0018, 0.0028,   8.0),
        (( 0.044, -0.002, 0.076), 0.0028, 0.0020, 0.0052, -18.0),
        (( 0.048,  0.002, 0.090), 0.0018, 0.0018, 0.0028, -12.0),
        (( 0.051,  0.000, 0.052), 0.0032, 0.0022, 0.0060, -15.0),
        (( 0.040, -0.003, 0.016), 0.0042, 0.0022, 0.0026, -50.0),
        ((-0.051,  0.000, 0.046), 0.0032, 0.0022, 0.0060,  20.0),
        ((-0.041, -0.003, 0.018), 0.0035, 0.0022, 0.0032,  42.0),
    ]

    for center, rx, ry, rz, tilt_deg in shards_spec:
        v, f, m = build_shard(center, rx, ry, rz, tilt_deg, rnd)
        _append_component(verts, faces, fmats, v, f, m)

    return verts, faces, fmats


# ---------------------------------------------------------- verificacion
def check_mesh(verts, faces):
    edges = {}
    for f in faces:
        for i in range(3):
            e = (f[i], f[(i + 1) % 3])
            edges[e] = edges.get(e, 0) + 1
    closed = all(c == 1 and edges.get((b, a)) == 1 for (a, b), c in edges.items())
    xs, ys, zs = zip(*verts)
    return {
        "triangulos": len(faces),
        "vertices": len(verts),
        "cerrada_y_consistente": closed,
        "volumen_firmado_cm3": round(signed_volume(verts, faces) * 1e6, 1),
        "medidas_cm": (round((max(xs) - min(xs)) * 100, 1),
                       round((max(ys) - min(ys)) * 100, 1),
                       round((max(zs) - min(zs)) * 100, 1)),
        "z_min_cm": round(min(zs) * 100, 3),
    }


# ---------------------------------------------------------------- Blender
def build_in_blender(verts, faces, fmats):
    import bpy

    mesh = bpy.data.meshes.new("LuzMala")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    obj = bpy.data.objects.new("LuzMala", mesh)
    bpy.context.scene.collection.objects.link(obj)

    for mname, hx, rough, alpha, trans, emit in MATERIALS:
        rgb_lin = tuple(srgb_to_linear(c) for c in hex_to_rgb(hx))
        rgba_lin = rgb_lin + (alpha,)

        mat = bpy.data.materials.new(mname)
        mat.use_nodes = True
        # Activar transparencia en el viewport y en Eevee (Blender 2.8 a 5.x)
        if hasattr(mat, "surface_render_method"):
            mat.surface_render_method = "BLENDED"
        elif hasattr(mat, "blend_method"):
            mat.blend_method = "BLEND"
        if hasattr(mat, "use_backface_culling"):
            mat.use_backface_culling = False

        nt = mat.node_tree
        bsdf = nt.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = rgb_lin + (1.0,)
            bsdf.inputs["Roughness"].default_value = rough
            if "Alpha" in bsdf.inputs:
                bsdf.inputs["Alpha"].default_value = alpha
            # Transmisión para efecto cristal/llama translúcida
            if "Transmission Weight" in bsdf.inputs:
                bsdf.inputs["Transmission Weight"].default_value = trans
            elif "Transmission" in bsdf.inputs:
                bsdf.inputs["Transmission"].default_value = trans
            # Leve emisión del mismo tinte celeste
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = rgb_lin + (1.0,)
            elif "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value = rgb_lin + (1.0,)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emit

        mat.diffuse_color = rgba_lin
        mesh.materials.append(mat)

    for poly, m in zip(mesh.polygons, fmats):
        poly.material_index = m
        try:
            poly.use_smooth = False
        except Exception:
            pass

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    if FBX_PATH:
        os.makedirs(os.path.dirname(os.path.abspath(FBX_PATH)), exist_ok=True)
        bpy.ops.export_scene.fbx(
            filepath=FBX_PATH, use_selection=True, object_types={"MESH"},
            apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
            axis_forward="-Z", axis_up="Y", bake_space_transform=True,
            mesh_smooth_type="FACE")
    return obj


def _in_blender():
    try:
        import bpy  # noqa: F401
        return hasattr(bpy.context, "scene") and bpy.context.scene is not None \
            and bpy.app.background is False
    except ImportError:
        return False


if __name__ == "__main__":
    V, F, M = build_mesh()
    if _in_blender():
        build_in_blender(V, F, M)
    else:
        print(check_mesh(V, F))