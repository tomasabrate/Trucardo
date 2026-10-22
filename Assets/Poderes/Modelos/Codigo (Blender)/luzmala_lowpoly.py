"""
LUZ MALA LOW POLY  (llama azul-turquesa facetada con esquirlas flotando)

Unidades: metros, tamano real aproximado (~30 cm de alto, ~17 cm de ancho; despues
la escalas a mano). Z hacia arriba. El frente es -Y. El origen esta en la base
(la punta de abajo de la llama, z = 0), centrado en x = 0, y = 0.

Un solo objeto "LuzMala" con materiales de color plano, flat shading.
El brillo: los materiales "Glow_*" (el centro blanco-menta de la llama) tienen
EMISION. En Blender se ven brillantes (con Eevee activa Bloom si queres halo).
En Unity la emision del FBX no siempre viaja, asi que en esos materiales
(Glow_1, Glow_2): Emission > activar, color blanco/menta, intensidad ~2.
Para que "lance luz" de verdad: agrega un Point Light hijo en el centro
(color #9EF8F6, rango ~1.5 m) y Bloom en el Post-Processing de la camara.

USO EN BLENDER (2.8 o superior):
    Pestana Scripting > Open > este archivo > Run Script (Alt+P).
    Para exportar a Unity, pone una ruta en FBX_PATH, por ejemplo:
    FBX_PATH = "C:/Users/TuUsuario/Desktop/LuzMala.fbx"

Fuera de Blender (python luzmala_lowpoly.py) solo imprime un resumen de la malla.
"""
import math
import random

# ----------------------------------------------------------------- ajustes
FBX_PATH = ""          # ruta para exportar .fbx desde Blender ("" = no exportar)
SEED = 7               # cambia el reparto de colores de las facetas
GLOW_STRENGTH = 3.0    # fuerza de emision de los materiales Glow_*

# nombre, color sRGB, rugosidad, emisivo
MATERIALS = [
    ("Azul_Hondo",   "#3E7FAE", 0.6, False),
    ("Verde_Azul",   "#4A9FA8", 0.6, False),
    ("Petroleo",     "#327088", 0.6, False),
    ("Verde_Agua",   "#5FD3BC", 0.5, False),
    ("Menta",        "#8BE8C4", 0.5, False),
    ("Celeste",      "#8FDFF0", 0.5, False),
    ("Celeste_Claro", "#C4F6FA", 0.4, False),
    ("Cian",         "#9EF8F6", 0.4, False),
    ("Glow_1",       "#E4FFF6", 0.3, True),
    ("Glow_2",       "#FFFFFF", 0.3, True),
]
MI = {m[0]: i for i, m in enumerate(MATERIALS)}

# Lenguas de llama: (base x,y,z), (punta x,y,z), radio base, punto medio opcional
TONGUES = [
    ((-0.005, 0.000, 0.150), (0.018, 0.000, 0.300), 0.042, (-0.016, 0.000, 0.235)),
    ((-0.042, -0.004, 0.130), (-0.036, -0.004, 0.239), 0.030, (-0.046, -0.004, 0.190)),
    ((0.050, 0.000, 0.125), (0.043, 0.000, 0.221), 0.028, (0.052, 0.000, 0.175)),
    ((-0.066, 0.000, 0.105), (-0.064, 0.000, 0.175), 0.022, (-0.070, 0.000, 0.140)),
    ((0.070, 0.000, 0.090), (0.077, 0.000, 0.138), 0.020, (0.078, 0.000, 0.112)),
    ((-0.022, -0.030, 0.135), (-0.015, -0.026, 0.215), 0.026, (-0.025, -0.030, 0.175)),
    ((-0.005, 0.034, 0.140), (0.006, 0.030, 0.232), 0.028, (-0.012, 0.034, 0.185)),
]

# Esquirlas: (x, y, z, largo, inclinacion en grados)
SHARDS = [
    (0.025, -0.01, 0.282, 0.016, 20), (0.047, 0.01, 0.243, 0.015, -12),
    (-0.050, 0.00, 0.238, 0.010, 30), (-0.075, -0.01, 0.190, 0.014, 25),
    (0.075, 0.01, 0.150, 0.011, -30), (-0.088, 0.00, 0.113, 0.012, -25),
    (0.088, -0.01, 0.127, 0.011, -20), (-0.072, 0.01, 0.063, 0.010, -40),
    (0.062, -0.01, 0.055, 0.010, 35), (-0.062, -0.005, 0.185, 0.005, 60),
]


# ------------------------------------------------------------ geometria
def _sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def _add(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def _mul(a, k): return (a[0] * k, a[1] * k, a[2] * k)
def _dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def _cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def _norm(a):
    n = math.sqrt(_dot(a, a)) or 1.0
    return (a[0] / n, a[1] / n, a[2] / n)


def signed_volume(verts, faces):
    return sum(_dot(verts[a], _cross(verts[b], verts[c])) for a, b, c in faces) / 6.0


def _orient(verts, faces):
    """Da vuelta las caras si la pieza quedo con las normales hacia adentro."""
    if signed_volume(verts, faces) < 0:
        return [(a, c, b) for a, b, c in faces]
    return faces


def icosphere_body(rng):
    """Cuerpo de la llama: icosfera (subdiv 1) irregular, con punta abajo."""
    t = (1 + 5 ** 0.5) / 2
    base = [(-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0), (0, -1, t), (0, 1, t),
            (0, -1, -t), (0, 1, -t), (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1)]
    # polos sobre Z: giramos para que un vertice quede abajo
    ang = math.atan2(1, t)
    ca, sa = math.cos(ang), math.sin(ang)
    v = []
    for x, y, z in base:
        n = _norm((x, y, z))
        v.append((n[0] * ca - n[1] * sa, n[0] * sa + n[1] * ca, n[2]))
    # tras la rotacion el vertice (0,1,t)-> aprox sobre eje; ajuste: buscar el mas bajo en Y
    # y mandarlo a -Z intercambiando ejes (y -> z)
    v = [(p[0], p[2], p[1]) for p in v]
    f = [(0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11), (1, 5, 9), (5, 11, 4),
         (11, 10, 2), (10, 7, 6), (7, 1, 8), (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8),
         (3, 8, 9), (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1)]
    cache = {}

    def mid(a, b):
        k = (min(a, b), max(a, b))
        if k not in cache:
            m = _norm(_mul(_add(v[a], v[b]), 0.5))
            v.append(m)
            cache[k] = len(v) - 1
        return cache[k]

    nf = []
    for a, b, c in f:
        ab, bc, ca_ = mid(a, b), mid(b, c), mid(c, a)
        nf += [(a, ab, ca_), (b, bc, ab), (c, ca_, bc), (ab, bc, ca_)]
    # forma: mas ancho arriba-medio, punta abajo, irregular
    out = []
    for x, y, z in v:
        r = 1.0 + rng.uniform(-0.10, 0.10)
        zz = z
        k = 0.085
        if zz < 0:                      # parte baja: cono afilado
            k *= 1.0 - 0.30 * (-zz)
            zz *= 1.0
        px, py, pz = x * k * r, y * k * r * 0.95, zz * 0.085 * r + 0.085
        out.append((px, py, max(pz, 0.0)))
    return out, _orient(out, nf)


def tongue(base, tip, r, mid, rng):
    """Lengua = piramide de 4 lados doblada (3 anillos + punta), cerrada."""
    p0, p1, p2 = base, mid, tip
    p15 = _add(_mul(_add(p1, p2), 0.5), (0, 0, 0))
    path = [p0, p1, p15]
    radii = [r, r * 0.72, r * 0.36]
    ref = (0.0, 1.0, 0.0)
    off = rng.uniform(0, 1.2)
    verts, faces, rings = [], [], []
    for i, (p, rad) in enumerate(zip(path, radii)):
        nxt = path[i + 1] if i + 1 < len(path) else tip
        prv = path[i - 1] if i > 0 else p
        d = _norm(_sub(nxt, prv))
        u = _norm(_cross(d, ref))
        w = _norm(_cross(d, u))
        ring = []
        for k in range(4):
            a = off + k * math.pi / 2
            s = rad * rng.uniform(0.85, 1.15)
            q = _add(p, _add(_mul(u, math.cos(a) * s * 1.25), _mul(w, math.sin(a) * s * 0.8)))
            verts.append(q)
            ring.append(len(verts) - 1)
        rings.append(ring)
    verts.append(tip)
    it = len(verts) - 1
    verts.append(p0)
    ic = len(verts) - 1
    for A, B in zip(rings, rings[1:]):
        for j in range(4):
            a, b, c, d = A[j], A[(j + 1) % 4], B[(j + 1) % 4], B[j]
            faces += [(a, b, c), (a, c, d)] if j % 2 == 0 else [(a, b, d), (b, c, d)]
    for j in range(4):
        faces.append((rings[-1][j], rings[-1][(j + 1) % 4], it))
        faces.append((ic, rings[0][(j + 1) % 4], rings[0][j]))
    return verts, _orient(verts, faces)


def shard(x, y, z, length, tilt_deg, rng):
    """Esquirla: octaedro alargado."""
    t = math.radians(tilt_deg)
    d = (math.sin(t), 0.0, math.cos(t))
    e1 = (math.cos(t), 0.0, -math.sin(t))
    e2 = (0.0, 1.0, 0.0)
    c = (x, y, z)
    wd, th = length * 0.30, length * 0.12
    top = _add(c, _mul(d, length * rng.uniform(0.9, 1.1)))
    bot = _sub(c, _mul(d, length * rng.uniform(0.8, 1.0)))
    mid = [_add(c, _mul(e1, wd)), _add(c, _mul(e2, th)), _sub(c, _mul(e1, wd)), _sub(c, _mul(e2, th))]
    verts = [top, bot] + mid
    faces = []
    for j in range(4):
        faces.append((0, 2 + j, 2 + (j + 1) % 4))
        faces.append((1, 2 + (j + 1) % 4, 2 + j))
    return verts, _orient(verts, faces)


def build_mesh():
    """Devuelve (vertices, caras, material de cada cara, nombre de cada pieza por cara)."""
    rng = random.Random(SEED)
    V, F, M, P = [], [], [], []

    def add(verts, faces, kind):
        o = len(V)
        V.extend(verts)
        for a, b, c in faces:
            F.append((a + o, b + o, c + o))
            P.append(kind)

    bv, bf = icosphere_body(rng)
    add(bv, bf, "body")
    for base, tip, r, mid in TONGUES:
        tv, tf = tongue(base, tip, r, mid, rng)
        add(tv, tf, "tongue")
    for s in SHARDS:
        sv, sf = shard(*s, rng)
        add(sv, sf, "shard")
    M = assign_materials(V, F, P, rng)
    return V, F, M, P


def assign_materials(V, F, P, rng):
    """Color de cada faceta segun altura, orientacion y cercania al centro."""
    out = []
    pick = lambda names: MI[rng.choice(names)]
    for (a, b, c), kind in zip(F, P):
        pa, pb, pc = V[a], V[b], V[c]
        n = _norm(_cross(_sub(pb, pa), _sub(pc, pa)))
        cx = (pa[0] + pb[0] + pc[0]) / 3
        cy = (pa[1] + pb[1] + pc[1]) / 3
        cz = (pa[2] + pb[2] + pc[2]) / 3
        if kind == "shard":
            out.append(pick(["Celeste", "Verde_Agua", "Verde_Azul", "Celeste_Claro"]))
        elif kind == "body":
            if n[2] < -0.35:
                out.append(pick(["Azul_Hondo", "Petroleo", "Verde_Azul"]))
            elif n[1] < -0.25 and 0.05 < cz < 0.13 and abs(cx) < 0.045:
                out.append(pick(["Glow_1", "Glow_2", "Glow_1"]))
            elif cz < 0.06:
                out.append(pick(["Verde_Azul", "Verde_Agua", "Azul_Hondo", "Menta"]))
            elif cz < 0.10:
                out.append(pick(["Menta", "Verde_Agua", "Cian", "Celeste"]))
            else:
                out.append(pick(["Cian", "Celeste", "Menta", "Celeste_Claro"]))
        else:   # lengua
            if cz > 0.20:
                out.append(pick(["Celeste", "Celeste_Claro", "Celeste"]))
            elif n[0] > 0.3:
                out.append(pick(["Verde_Azul", "Verde_Agua", "Celeste"]))
            elif n[0] < -0.3:
                out.append(pick(["Celeste", "Celeste_Claro", "Cian"]))
            else:
                out.append(pick(["Cian", "Celeste_Claro", "Menta", "Glow_1"]))
    return out


# ---------------------------------------------------------- verificacion
def check_mesh(V, F, P):
    edges = {}
    for f in F:
        for i in range(3):
            e = (f[i], f[(i + 1) % 3])
            edges[e] = edges.get(e, 0) + 1
    closed = all(edges.get((b, a), 0) == c == 1 for (a, b), c in edges.items())
    vol = {}
    for f, k in zip(F, P):
        vol[k] = vol.get(k, 0) + signed_volume(V, [f])
    xs, ys, zs = zip(*V)
    return {
        "triangulos": len(F), "vertices": len(V),
        "cerrada_y_consistente": closed,
        "volumen_por_pieza_cm3": {k: round(v * 1e6, 1) for k, v in vol.items()},
        "medidas_cm": (round((max(xs) - min(xs)) * 100, 1), round((max(ys) - min(ys)) * 100, 1),
                       round((max(zs) - min(zs)) * 100, 1)),
        "z_min_cm": round(min(zs) * 100, 2),
    }


# ---------------------------------------------------------------- Blender
def hex_to_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def build_in_blender(V, F, M):
    import bpy

    mesh = bpy.data.meshes.new("LuzMala")
    mesh.from_pydata(V, [], F)
    mesh.update()
    obj = bpy.data.objects.new("LuzMala", mesh)
    bpy.context.scene.collection.objects.link(obj)

    for name, hx, rough, glow in MATERIALS:
        lin = tuple(srgb_to_linear(c) for c in hex_to_rgb(hx)) + (1.0,)
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = lin
            bsdf.inputs["Roughness"].default_value = rough
            if glow:
                key = "Emission Color" if "Emission Color" in bsdf.inputs else "Emission"
                bsdf.inputs[key].default_value = lin
                if "Emission Strength" in bsdf.inputs:
                    bsdf.inputs["Emission Strength"].default_value = GLOW_STRENGTH
        mat.diffuse_color = lin
        mesh.materials.append(mat)

    for poly, m in zip(mesh.polygons, M):
        poly.material_index = m
        poly.use_smooth = False

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    if FBX_PATH:
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
    V, F, M, P = build_mesh()
    if _in_blender():
        build_in_blender(V, F, M)
    else:
        print(check_mesh(V, F, P))
