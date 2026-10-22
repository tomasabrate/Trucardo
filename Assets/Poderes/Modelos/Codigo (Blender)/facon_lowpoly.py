"""
FACON LOW POLY  (hoja facetada + virola + mango de madera + pomo de metal)

Unidades: metros, tamano real (largo total 40 cm: hoja ~27 cm, mango ~12 cm,
hoja de ~5 cm de ancho). El cuchillo esta apoyado de costado sobre la mesa:
- eje X = largo (la punta mira hacia -X y el pomo hacia +X)
- eje Y = ancho (el lomo de la hoja queda hacia +Y y el filo hacia -Y)
- eje Z = hacia arriba (la cara plana de la hoja mira hacia arriba, como en la foto)
La parte mas baja del mango y la punta de la hoja tocan z = 0. El origen esta en
el centro del cuchillo visto desde arriba.

Un solo objeto "Facon" con varios materiales de color plano (acero, metal del
pomo y la virola, madera), flat shading, ~470 triangulos.

USO EN BLENDER (2.8 o superior):
    Pestana Scripting > Open > este archivo > Run Script (Alt+P).
    Para exportar a Unity, pone una ruta en FBX_PATH mas abajo, por ejemplo:
    FBX_PATH = "C:/Users/TuUsuario/Desktop/Facon.fbx"

Los colores de cada cara (FACE_MATS) se tomaron de la foto. Si cambias
LADOS_MANGO o las tablas de medidas, la cantidad de caras cambia, los colores
guardados ya no coinciden y cada pieza (hoja, virola, mango, pomo) toma un
unico color de su familia.

Fuera de Blender (python facon_lowpoly.py) solo imprime un resumen de la malla.
"""
import math
import random

# ----------------------------------------------------------------- ajustes
FBX_PATH = ""          # ruta para exportar .fbx desde Blender ("" = no exportar)
SEED = 3               # irregularidad de las facetas de la madera
LADOS_MANGO = 8        # lados de la seccion del mango, la virola y el pomo

TOTAL_LENGTH = 0.40    # largo total (m)
TS_MM = 1.8            # semiespesor del lomo (mm)
TR_MM = 2.3            # semiespesor en la arista central de la hoja (mm)
RATIO_ESPESOR = 0.80   # espesor del mango / ancho del mango

# Colores: nombre, color sRGB, rugosidad  (se calculan desde la foto)
MATERIALS = [
    ("Acero_1", "#5A5754", 0.4),
    ("Acero_2", "#7C7978", 0.4),
    ("Acero_3", "#9D9C9E", 0.4),
    ("Acero_4", "#BEBCBA", 0.4),
    ("Acero_5", "#DBDBDE", 0.4),
    ("Metal_1", "#86796B", 0.4),
    ("Metal_2", "#BDB9B4", 0.4),
    ("Madera_1", "#65432A", 0.8),
    ("Madera_2", "#836C57", 0.8),
    ("Madera_3", "#AF874C", 0.8),
    ("Madera_4", "#A78D63", 0.8),
    ("Madera_5", "#9B928B", 0.8),
]
# Un caracter por cara (0-9, A-Z) = indice de material. Vacio = color por pieza.
FACE_MATS = (
    "02201102020101101010101111211111211231111233333121233334421214433441212444444222"
    "24433332222333322212122331115556666666565555556666666666555565665655656655556555"
    "6555555588BAAAAABABB888888A99AAAA99A88888799A9AA9A9978777799AAAAA899777777A9A9AA"
    "AA987777778899AA998877777788A8AA8A88777777878AAAA8777777777AA8AA8A77777777978AAA"
    "A879777777798A88A89777778898A8888AA98877888888BBBBB85555555555555555565655565566"
    "5555556555665666555566566666665666566666666666666666666666555555"
)
PART_DEFAULT = {"blade": "Acero", "ferrule": "Metal", "wood": "Madera", "pomo": "Metal"}

# ---------------------------------------------------- medidas de la foto (px)
# Eje u a lo largo del cuchillo (punta = U_TIP, final del pomo = U_END), v a lo
# ancho (negativo = lomo, positivo = filo). Todo medido sobre la silueta.
U_TIP, U_END, U_HEEL = -1263.0, 1111.0, 388.0
V_CENTER = 17.0
U_CENTER = (U_TIP + U_END) / 2.0
K = TOTAL_LENGTH / (U_END - U_TIP)        # metros por pixel de la foto

# hoja: (u, v del lomo, v del filo)
BLADE = [
    (-1173, -66, -2), (-1083, -87, 30), (-963, -97, 62), (-813, -112, 91),
    (-662, -117, 113), (-482, -120, 127), (-302, -122, 139), (-122, -123, 151),
    (58, -121, 163), (238, -118, 175), (388, -120, 185),
]
TIP_V = -43.5
# virola: (u, semiancho) con eje en v = -46
FERRULE = [(388, 70), (400, 76), (468, 76), (478, 79)]
FERRULE_VC = -46.0
# mango de madera: (u, v del centro, semiancho)
HANDLE = [
    (476, -46.5, 82.5), (508, -47, 85), (556, -49.5, 92.5), (604, -52.5, 96.5),
    (652, -50.5, 99.5), (700, -48, 103), (748, -43, 104), (796, -32.5, 102.5),
    (844, -18.5, 104.5), (892, -4.5, 105.5), (940, 9.5, 107.5), (988, 18.5, 120.5),
    (1020, 21, 126),
]
# pomo de metal: (u, v del centro, semiancho)
POMO = [(1020, 21, 126), (1036, 19, 125), (1060, 19, 118), (1084, 22, 96.5),
        (1100, 28, 55), (1111, 34, 22)]


# ------------------------------------------------------------- utilidades
def hex_to_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0])


def signed_volume(verts, faces):
    return sum(dot(verts[a], cross(verts[b], verts[c])) for a, b, c in faces) / 6.0


def X(u):
    return (u - U_CENTER) * K


def Y(v):
    return -(v - V_CENTER) * K


# ------------------------------------------------------------- geometria
def loft(rings, tip=None):
    """Une anillos consecutivos (misma cantidad de vertices). 'tip' = punto unico
    al inicio. Cierra los extremos. Devuelve (vertices, caras) con las caras
    orientadas hacia afuera."""
    verts, idx = [], []
    if tip is not None:
        verts.append(tip)
        idx.append([0])
    for r in rings:
        idx.append(list(range(len(verts), len(verts) + len(r))))
        verts.extend(r)
    faces = []
    for i in range(len(idx) - 1):
        A, B = idx[i], idx[i + 1]
        if len(A) == 1:
            for j in range(len(B)):
                faces.append((A[0], B[(j + 1) % len(B)], B[j]))
            continue
        n = len(A)
        for j in range(n):
            j1 = (j + 1) % n
            a, b, c, d = A[j], A[j1], B[j1], B[j]
            if (i + j) % 2:                  # diagonales alternadas: facetas en rombo
                faces += [(a, b, d), (b, c, d)]
            else:
                faces += [(a, b, c), (a, c, d)]
    last = idx[-1]
    for j in range(1, len(last) - 1):
        faces.append((last[0], last[j], last[j + 1]))
    if tip is None:
        first = idx[0]
        for j in range(1, len(first) - 1):
            faces.append((first[0], first[j + 1], first[j]))
    if signed_volume(verts, faces) < 0:
        faces = [(a, c, b) for a, b, c in faces]
    return verts, faces


def blade_ring(u, vs, ve):
    k = min(1.0, (u - U_TIP) / 260.0) ** 0.85
    ts, tr = TS_MM / 1000.0 * k, TR_MM / 1000.0 * k
    z_heel = RATIO_ESPESOR * FERRULE[0][1] * K
    zmid = max(z_heel * (u - U_TIP) / (U_HEEL - U_TIP), tr)
    vr = vs + 0.42 * (ve - vs)
    return [(X(u), Y(vs), zmid + ts), (X(u), Y(vr), zmid + tr), (X(u), Y(ve), zmid),
            (X(u), Y(vr), zmid - tr), (X(u), Y(vs), zmid - ts)]


def oval_ring(u, vc, a, rnd=None, jitter=0.0):
    b = RATIO_ESPESOR * a
    ring = []
    for j in range(LADOS_MANGO):
        th = 2 * math.pi * (j + 0.5) / LADOS_MANGO
        r = 1.0 + (rnd.uniform(-jitter, jitter) if rnd else 0.0)
        ring.append((X(u), Y(vc + a * r * math.cos(th)), (b + b * r * math.sin(th)) * K))
    return ring


def build_mesh():
    """Devuelve (vertices, caras, pieza de cada cara)."""
    rnd = random.Random(SEED)
    verts, faces, parts = [], [], []

    def add(v, f, name):
        base = len(verts)
        verts.extend(v)
        faces.extend((a + base, b + base, c + base) for a, b, c in f)
        parts.extend([name] * len(f))

    # hoja
    rings = [blade_ring(u, vs, ve) for u, vs, ve in BLADE]
    v, f = loft(rings, tip=(X(U_TIP), Y(TIP_V), 0.0))
    add(v, f, "blade")
    # virola
    v, f = loft([oval_ring(u, FERRULE_VC, a) for u, a in FERRULE])
    add(v, f, "ferrule")
    # mango
    v, f = loft([oval_ring(u, vc, a, rnd, 0.045) for u, vc, a in HANDLE])
    add(v, f, "wood")
    # pomo
    v, f = loft([oval_ring(u, vc, a, rnd, 0.02) for u, vc, a in POMO])
    add(v, f, "pomo")
    return verts, faces, parts


def face_material_indices(parts):
    """Indice de material de cada cara."""
    if FACE_MATS and len(FACE_MATS) == len(parts):
        return [int(ch, 36) for ch in FACE_MATS]
    names = [m[0] for m in MATERIALS]
    out = []
    for p in parts:
        base = PART_DEFAULT[p]
        k = next((i for i, n in enumerate(names) if n.startswith(base)), 0)
        out.append(k)
    return out


# ---------------------------------------------------------- verificacion
def check_mesh(verts, faces):
    """Malla cerrada y orientada hacia afuera. Devuelve un resumen."""
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
        "z_min_cm": round(min(zs) * 100, 2),
    }


# ---------------------------------------------------------------- Blender
def build_in_blender(verts, faces, parts):
    import bpy

    mesh = bpy.data.meshes.new("Facon")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Facon", mesh)
    bpy.context.scene.collection.objects.link(obj)

    for mname, hx, rough in MATERIALS:
        lin = tuple(srgb_to_linear(c) for c in hex_to_rgb(hx)) + (1.0,)
        mat = bpy.data.materials.new(mname)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = lin
            bsdf.inputs["Roughness"].default_value = rough
            if mname.startswith(("Acero", "Metal")):
                bsdf.inputs["Metallic"].default_value = 0.6
        mat.diffuse_color = lin
        mesh.materials.append(mat)

    for poly, m in zip(mesh.polygons, face_material_indices(parts)):
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
    V, F, P = build_mesh()
    if _in_blender():
        build_in_blender(V, F, P)
    else:
        print(check_mesh(V, F))
