r"""
PAVA  (pava clásica de aluminio low poly con pico vertedor y mango de madera)

Unidades: metros, tamaño real (~22 cm de ancho incluyendo el pico, ~18.2 cm de
diámetro de panza y ~21.5 cm de alto total hasta el mango). Z hacia arriba.
El pico mira hacia -X (queda a la izquierda en la vista frontal de Blender,
tecla 1 del teclado numérico, igual que en la foto de referencia). El origen
(0, 0, 0) está en el centro de la base, apoyada exactamente en z = 0.

Un solo objeto "Pava" con 6 materiales de color plano (flat shading, sin
texturas externas):
    Metal_Claro, Metal_Medio, Metal_Oscuro, Madera_Clara, Madera_Media, Madera_Oscura.

USO EN BLENDER (2.8 o superior):
    Pestaña Scripting > Open > este archivo > Run Script (Alt+P).
    Para exportar a Unity, poné una ruta en FBX_PATH más abajo, por ejemplo:
    FBX_PATH = "C:/Users/TuUsuario/Desktop/Pava.fbx"

Fuera de Blender (python pava_lowpoly.py) solo imprime un resumen de la malla.
"""
import math
import os
import random

# ----------------------------------------------------------------- ajustes
SEED = 11
BODY_SIDES = 12         # 12 lados con anillos alternados (aspecto facetado triangular)
SPOUT_SIDES = 8         # sección octogonal para el pico
STRAP_SIDES = 6         # sección hexagonal achatada para las chapitas del asa
HANDLE_SIDES = 8        # sección octogonal para el mango de madera torneado

FBX_PATH = r"C:\Users\Tomas Abrate\Downloads\Poderes Modelos 3D\Modelos 3D\Pava.fbx"

MATERIALS = [           # nombre, color sRGB, rugosidad, metalicidad
    ("Metal_Claro",   "#DCDCDC", 0.32, 0.85),
    ("Metal_Medio",   "#B8B5B0", 0.38, 0.82),
    ("Metal_Oscuro",  "#726F6B", 0.45, 0.75),
    ("Madera_Clara",  "#B87D3E", 0.55, 0.05),
    ("Madera_Media",  "#965D2C", 0.58, 0.05),
    ("Madera_Oscura", "#6B3E1B", 0.62, 0.05),
]
M_MET_L, M_MET_M, M_MET_D = 0, 1, 2
M_WOOD_L, M_WOOD_M, M_WOOD_D = 3, 4, 5


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


def _pick_metal_shade(n, kind, rnd):
    if kind == "dark":
        return M_MET_D
    if kind == "knob_top":
        return M_MET_L if n[2] > 0.2 else M_MET_M
    # Variación facetada según orientación de la normal como en la referencia
    score = 0.45 * n[2] - 0.35 * n[1] - 0.20 * n[0] + rnd.uniform(-0.12, 0.12)
    if score > 0.12:
        return M_MET_L
    if score > -0.22:
        return M_MET_M
    return M_MET_D


def _pick_wood_shade(n, kind, rnd):
    if kind == "wood_dark":
        return M_WOOD_D
    if kind == "wood_light":
        return M_WOOD_L if n[2] >= -0.1 else M_WOOD_M
    score = 0.50 * n[2] - 0.35 * n[1] + rnd.uniform(-0.15, 0.15)
    if score > 0.16:
        return M_WOOD_L
    if score > -0.18:
        return M_WOOD_M
    return M_WOOD_D


# ----------------------------------------- constructores geométricos cerrados
def build_body_and_lid(rnd):
    """Cuerpo de la pava + tapa + perilla en una sola malla de revolución cerrada."""
    # (radio_m, altura_z_m, tipo_material, desfasar_anillo, ruido_radial)
    profile = [
        (0.078, 0.000, "dark",      False, 0.000),  # apoyo plano en z = 0
        (0.088, 0.018, "metal",     True,  0.002),  # panza baja
        (0.091, 0.044, "metal",     False, 0.002),  # panza media (diámetro ~18.2 cm)
        (0.083, 0.072, "metal",     True,  0.002),  # panza alta
        (0.067, 0.096, "metal",     False, 0.001),  # hombro
        (0.056, 0.106, "metal",     True,  0.000),  # borde asiento de la tapa
        (0.052, 0.107, "dark",      False, 0.000),  # ranura oscura bajo la tapa
        (0.055, 0.111, "metal",     False, 0.000),  # labio exterior de la tapa
        (0.049, 0.114, "dark",      True,  0.000),  # escalón interior de la tapa
        (0.032, 0.117, "metal",     False, 0.000),  # domo de la tapa
        (0.016, 0.118, "wood_dark", False, 0.000),  # arandela base de la perilla
        (0.014, 0.121, "wood_dark", False, 0.000),  # tope arandela base
        (0.009, 0.122, "dark",      False, 0.000),  # cuello metálico perilla (base)
        (0.009, 0.130, "dark",      False, 0.000),  # cuello metálico perilla (tope)
        (0.017, 0.133, "wood_dark", True,  0.000),  # expansión inferior perilla madera
        (0.019, 0.139, "wood",      False, 0.000),  # anillo ancho perilla madera
        (0.014, 0.144, "wood_light",True,  0.000),  # bisel superior perilla madera
        (0.007, 0.145, "knob_top",  False, 0.000),  # botón metálico central (base)
        (0.005, 0.148, "knob_top",  False, 0.000),  # botón metálico central (corona)
    ]

    verts = [(0.0, 0.0, 0.0)]  # índice 0: centro de la base en z = 0
    rings = []

    for r, z, _kind, stag, jitter in profile:
        start = len(verts)
        offset = (math.pi / BODY_SIDES) if stag else 0.0
        for k in range(BODY_SIDES):
            ang = 2.0 * math.pi * k / BODY_SIDES + offset
            dr = rnd.uniform(-jitter, jitter) if jitter > 0.0 else 0.0
            rk = r + dr
            verts.append((rk * math.cos(ang), rk * math.sin(ang), z))
        rings.append(start)

    top_center = len(verts)
    verts.append((0.0, 0.0, 0.149))

    faces, fmats = [], []

    # Tapa inferior (z = 0)
    r0 = rings[0]
    for k in range(BODY_SIDES):
        kn = (k + 1) % BODY_SIDES
        faces.append((0, r0 + kn, r0 + k))
        fmats.append(M_MET_D)

    # Bandas laterales entre anillos
    for i in range(len(profile) - 1):
        ra, rb = rings[i], rings[i + 1]
        kind = profile[i + 1][2]
        stag_a, stag_b = profile[i][3], profile[i + 1][3]
        for k in range(BODY_SIDES):
            kn = (k + 1) % BODY_SIDES
            a, b = ra + k, ra + kn
            # Conectar diagonal más corta según el desfasaje de anillos
            if stag_a and not stag_b:
                c, d = rb + kn, rb + k
                tris = [(a, b, c), (a, c, d)]
            else:
                c, d = rb + kn, rb + k
                tris = [(a, b, d), (b, c, d)]

            for t in tris:
                n = face_normal(verts[t[0]], verts[t[1]], verts[t[2]])
                if kind.startswith("wood"):
                    mat = _pick_wood_shade(n, kind, rnd)
                else:
                    mat = _pick_metal_shade(n, kind, rnd)
                faces.append(t)
                fmats.append(mat)

    # Cierre superior del botón de la perilla
    rlast = rings[-1]
    for k in range(BODY_SIDES):
        kn = (k + 1) % BODY_SIDES
        faces.append((rlast + k, rlast + kn, top_center))
        fmats.append(M_MET_L)

    return verts, faces, fmats


def build_tube(frames, sides, mat_fn):
    """Construye un tubo cerrado a partir de una lista de frames:
    (centro_xyz, dir_tangente, radio_y, radio_perp, tag_material, lift_fn)."""
    verts = []
    rings = []

    # Centro tapa inicial
    c0, t0, _ry0, _rp0, _tag0, _lf0 = frames[0]
    verts.append(c0)
    start_cap = 0

    for center, tang, ry, rp, _tag, lift_fn in frames:
        t_unit = unit(tang)
        # Base local ortonormal: U = eje +Y, V = T x U (en plano XZ)
        u_vec = (0.0, 1.0, 0.0)
        v_vec = unit(cross(t_unit, u_vec))
        # Asegurar base derecha (u_vec x v_vec = t_unit)
        u_vec = unit(cross(v_vec, t_unit))

        ring_start = len(verts)
        for k in range(sides):
            ang = 2.0 * math.pi * k / sides
            ca, sa = math.cos(ang), math.sin(ang)
            p = add(center, add(scale(u_vec, ry * ca), scale(v_vec, rp * sa)))
            if lift_fn is not None:
                p = lift_fn(ca, sa, p, t_unit)
            verts.append(p)
        rings.append(ring_start)

    end_cap = len(verts)
    verts.append(frames[-1][0])

    faces, fmats = [], []

    # Tapa inicial
    r0 = rings[0]
    for k in range(sides):
        kn = (k + 1) % sides
        faces.append((start_cap, r0 + kn, r0 + k))
        fmats.append(M_MET_D)

    # Segmentos del tubo
    for i in range(len(frames) - 1):
        ra, rb = rings[i], rings[i + 1]
        tag = frames[i + 1][4]
        for k in range(sides):
            kn = (k + 1) % sides
            t1 = (ra + k, ra + kn, rb + kn)
            t2 = (ra + k, rb + kn, rb + k)
            for t in (t1, t2):
                n = face_normal(verts[t[0]], verts[t[1]], verts[t[2]])
                faces.append(t)
                fmats.append(mat_fn(n, tag))

    # Tapa final
    rlast = rings[-1]
    last_tag = frames[-1][4]
    for k in range(sides):
        kn = (k + 1) % sides
        t = (rlast + k, rlast + kn, end_cap)
        n = face_normal(verts[t[0]], verts[t[1]], verts[t[2]])
        faces.append(t)
        fmats.append(mat_fn(n, last_tag))

    return verts, faces, fmats


def build_spout(rnd):
    """Pico vertedor curvo en -X con corte en bisel y hueco interior oscuro."""
    def lip_shaper(ca, sa, p, t_unit):
        # Estira el labio inferior/exterior del pico para formar el pico vertedor en V
        lip = max(0.0, -sa) * 0.008 + (1.0 - abs(ca)) * 0.003
        return add(p, scale(t_unit, lip))

    frames = [
        # (centro, tangente, radio_y, radio_perp, tag, deformador_labio)
        ((-0.068, 0.0, 0.044), (-0.95, 0.0,  0.30), 0.025, 0.026, "dark",  None),
        ((-0.081, 0.0, 0.047), (-0.92, 0.0,  0.38), 0.024, 0.025, "metal", None),
        ((-0.097, 0.0, 0.054), (-0.75, 0.0,  0.66), 0.017, 0.018, "metal", None),
        ((-0.106, 0.0, 0.066), (-0.28, 0.0,  0.96), 0.014, 0.014, "metal", None),
        ((-0.110, 0.0, 0.083), (-0.22, 0.0,  0.97), 0.012, 0.012, "metal", None),
        ((-0.115, 0.0, 0.099), (-0.38, 0.0,  0.92), 0.011, 0.011, "metal", None),
        ((-0.122, 0.0, 0.110), (-0.52, 0.0,  0.85), 0.011, 0.012, "metal", lip_shaper),
        # Labio interno hundido para simular la boca oscura del pico manteniendo malla cerrada
        ((-0.117, 0.0, 0.102), (-0.52, 0.0,  0.85), 0.008, 0.008, "dark",  None),
    ]
    return build_tube(frames, SPOUT_SIDES, lambda n, tag: _pick_metal_shade(n, tag, rnd))


def build_strap(side_sign, rnd):
    """Soporte metálico lateral del asa (side_sign = -1 izquierda, +1 derecha)."""
    s = float(side_sign)
    z_top = 0.194 if side_sign < 0 else 0.198
    frames = [
        ((s * 0.050, 0.0, 0.090), (s * 0.65, 0.0, 0.76), 0.0090, 0.0025, "dark",  None),
        ((s * 0.060, 0.0, 0.101), (s * 0.70, 0.0, 0.71), 0.0095, 0.0028, "metal", None),
        ((s * 0.071, 0.0, 0.116), (s * 0.35, 0.0, 0.94), 0.0088, 0.0028, "metal", None),
        ((s * 0.077, 0.0, 0.145), (0.00,     0.0, 1.00), 0.0088, 0.0028, "metal", None),
        ((s * 0.070, 0.0, 0.174), (-s * 0.55,0.0, 0.83), 0.0088, 0.0028, "metal", None),
        ((s * 0.056, 0.0, z_top), (-s * 0.85,0.0, 0.52), 0.0095, 0.0030, "metal", None),
        ((s * 0.047, 0.0, z_top), (-s * 1.00,0.0, 0.00), 0.0075, 0.0025, "dark",  None),
    ]
    return build_tube(frames, STRAP_SIDES, lambda n, tag: _pick_metal_shade(n, tag, rnd))


def build_wooden_handle(rnd):
    """Mango superior de madera torneada + remaches metálicos en los extremos."""
    # Eje del mango entre (-0.063, 0, 0.194) y (+0.063, 0, 0.198)
    p0 = (-0.063, 0.0, 0.194)
    p1 = (0.063, 0.0, 0.198)
    axis = sub(p1, p0)
    tang = unit(axis)

    # (fracción_t, radio, tag_material)
    stations = [
        (0.000, 0.0045, "knob_top"),   # cabeza remache izquierdo
        (0.045, 0.0050, "knob_top"),   # cuerpo remache izquierdo
        (0.055, 0.0115, "wood_dark"),  # inicio extremo izquierdo madera
        (0.130, 0.0155, "wood"),       # cono izquierdo del mango
        (0.290, 0.0118, "wood_light"), # garganta izquierda
        (0.415, 0.0160, "wood"),       # primer anillo central
        (0.500, 0.0125, "wood_dark"),  # ranura central
        (0.585, 0.0160, "wood"),       # segundo anillo central
        (0.710, 0.0118, "wood_light"), # garganta derecha
        (0.870, 0.0162, "wood"),       # cono derecho del mango
        (0.945, 0.0120, "wood_dark"),  # fin extremo derecho madera
        (0.955, 0.0050, "knob_top"),   # cuerpo remache derecho
        (1.000, 0.0045, "knob_top"),   # cabeza remache derecho
    ]

    frames = []
    for t, r, tag in stations:
        c = add(p0, scale(axis, t))
        frames.append((c, tang, r, r, tag, None))

    def mat_selector(n, tag):
        if tag.startswith("wood"):
            return _pick_wood_shade(n, tag, rnd)
        return _pick_metal_shade(n, tag, rnd)

    return build_tube(frames, HANDLE_SIDES, mat_selector)


# ------------------------------------------------------------ malla completa
def build_mesh():
    rnd = random.Random(SEED)
    verts, faces, fmats = [], [], []

    for builder in (
        lambda: build_body_and_lid(rnd),
        lambda: build_spout(rnd),
        lambda: build_strap(-1, rnd),
        lambda: build_strap(1, rnd),
        lambda: build_wooden_handle(rnd),
    ):
        v, f, m = builder()
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

    mesh = bpy.data.meshes.new("Pava")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    obj = bpy.data.objects.new("Pava", mesh)
    bpy.context.scene.collection.objects.link(obj)

    for mname, hx, rough, metal in MATERIALS:
        lin = tuple(srgb_to_linear(c) for c in hex_to_rgb(hx)) + (1.0,)
        mat = bpy.data.materials.new(mname)
        mat.use_nodes = True
        nt = mat.node_tree
        bsdf = nt.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Roughness"].default_value = rough
            bsdf.inputs["Base Color"].default_value = lin
            if "Metallic" in bsdf.inputs:
                bsdf.inputs["Metallic"].default_value = metal
        mat.diffuse_color = lin
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