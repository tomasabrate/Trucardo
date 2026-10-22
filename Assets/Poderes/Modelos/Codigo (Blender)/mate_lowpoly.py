"""
MATE LOW POLY  (calabaza + boca de metal + yerba + bombilla)

Unidades: metros, tamano real (mate de ~9 cm de alto). Z hacia arriba.
El origen esta en el centro de la base (donde apoya sobre la mesa).
Un solo objeto "Mate" con 6 materiales, flat shading, ~280 triangulos.

USO EN BLENDER (2.8 o superior):
    Pestana Scripting > Open > este archivo > Run Script (Alt+P).
    Para exportar a Unity, pone una ruta en FBX_PATH mas abajo, por ejemplo:
    FBX_PATH = "C:/Users/TuUsuario/Desktop/Mate.fbx"

USO FUERA DE BLENDER (solo Python, sin dependencias):
    python mate_lowpoly.py   ->  genera Mate.obj, Mate.mtl y Mate.glb
"""
import math
import os
import random
import struct
import json

# ----------------------------------------------------------------- ajustes
SEGMENTS = 10          # lados de la calabaza (mas = mas redonda)
SEED = 7               # semilla de las irregularidades de la yerba
FBX_PATH = ""          # ruta para exportar .fbx desde Blender ("" = no exportar)

MATERIALS = [
    ("Calabaza", "#9C5B2C"),
    ("Metal",    "#A9A59D"),
    ("Yerba_A",  "#6F7F2A"),
    ("Yerba_B",  "#8FA03A"),
    ("Yerba_C",  "#B5B546"),
    ("Bombilla", "#C8BFA6"),
]
CALABAZA, METAL, YERBA, BOMBILLA = 0, 1, (2, 3, 4), 5


# ------------------------------------------------------------- utilidades
def hex_to_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


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


# ------------------------------------------------------------- geometria
def lathe(rings, seg, perturb=None):
    """Revolucion de un perfil (r, z) de abajo hacia arriba.
    Los anillos alternos se giran medio paso para dar el aspecto facetado
    triangulado. Devuelve (vertices, caras, banda_de_cada_cara)."""
    verts, ring_idx = [], []
    for i, (r, z) in enumerate(rings):
        if r == 0:
            v = (0.0, 0.0, z)
            if perturb:
                v = perturb(i, v)
            ring_idx.append([len(verts)])
            verts.append(v)
            continue
        off = 0.5 if i % 2 else 0.0
        idxs = []
        for j in range(seg):
            a = 2 * math.pi * (j + off) / seg
            v = (r * math.cos(a), r * math.sin(a), z)
            if perturb:
                v = perturb(i, v)
            idxs.append(len(verts))
            verts.append(v)
        ring_idx.append(idxs)

    faces, bands = [], []
    for i in range(len(rings) - 1):
        A, B = ring_idx[i], ring_idx[i + 1]
        new = []
        if len(A) == 1:                      # abanico desde el polo inferior
            for j in range(seg):
                new.append((A[0], B[(j + 1) % seg], B[j]))
        elif len(B) == 1:                    # abanico hacia el polo superior
            for j in range(seg):
                new.append((A[j], A[(j + 1) % seg], B[0]))
        elif (i + 1) % 2 == 1:               # B girado +medio paso respecto de A
            for j in range(seg):
                j1 = (j + 1) % seg
                new.append((A[j], A[j1], B[j]))
                new.append((A[j1], B[j1], B[j]))
        else:                                # B girado -medio paso respecto de A
            for j in range(seg):
                j1 = (j + 1) % seg
                new.append((A[j], A[j1], B[j1]))
                new.append((A[j], B[j1], B[j]))
        faces += new
        bands += [i] * len(new)
    return verts, faces, bands


def square_tube(points, half):
    """Tubo de seccion cuadrada que sigue una poligonal en el plano XZ."""
    dirs = [unit(sub(points[i + 1], points[i])) for i in range(len(points) - 1)]
    sections = []
    for i, p in enumerate(points):
        if i == 0:
            d, k = dirs[0], 1.0
        elif i == len(points) - 1:
            d, k = dirs[-1], 1.0
        else:
            d = unit((dirs[i - 1][0] + dirs[i][0], 0.0, dirs[i - 1][2] + dirs[i][2]))
            k = 1.0 / dot(d, dirs[i - 1])      # compensa la inglete del codo
        n = (d[2], 0.0, -d[0])                  # perpendicular a la direccion, en XZ
        y = (0.0, 1.0, 0.0)
        sec = []
        for sn, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            sec.append((p[0] + sn * half * k * n[0] + sy * half * y[0],
                        p[1] + sn * half * k * n[1] + sy * half * y[1],
                        p[2] + sn * half * k * n[2] + sy * half * y[2]))
        sections.append(sec)

    verts = [v for s in sections for v in s]
    faces = []
    for k in range(len(sections) - 1):
        a, b = 4 * k, 4 * (k + 1)
        for e in range(4):
            e1 = (e + 1) % 4
            faces.append((a + e, a + e1, b + e1))
            faces.append((a + e, b + e1, b + e))
    faces += [(0, 3, 2), (0, 2, 1)]                         # tapa inferior
    t = 4 * (len(sections) - 1)
    faces += [(t, t + 1, t + 2), (t, t + 2, t + 3)]         # tapa superior
    return verts, faces


def build_mate():
    """Devuelve (vertices, caras, material_de_cada_cara)."""
    rnd = random.Random(SEED)

    # Perfil (radio, altura). Anillos 0-4 calabaza, 5-11 metal, 12-14 yerba.
    rings = [
        (0.000, 0.000),   # 0  polo de la base
        (0.024, 0.000),   # 1  borde de la base plana
        (0.040, 0.011),   # 2
        (0.048, 0.028),   # 3  panza (lo mas ancho)
        (0.047, 0.046),   # 4
        (0.041, 0.062),   # 5  fin de la calabaza
        (0.043, 0.063),   # 6  arranca la boca de metal
        (0.046, 0.074),   # 7
        (0.044, 0.088),   # 8
        (0.034, 0.090),   # 9  labio superior
        (0.031, 0.084),   # 10 pared interior
        (0.031, 0.080),   # 11 contacto con la yerba
        (0.022, 0.079),   # 12 yerba
        (0.011, 0.080),   # 13 yerba
        (0.000, 0.0805),  # 14 centro de la yerba
    ]
    band_mat = [CALABAZA] * 5 + [METAL] * 6 + ["Y"] * 3

    def perturb(i, v):
        # Irregularidad de las hojas de yerba (solo anillos 12, 13 y 14)
        if i in (12, 13):
            return (v[0] + rnd.uniform(-0.002, 0.002),
                    v[1] + rnd.uniform(-0.002, 0.002),
                    v[2] + rnd.uniform(-0.0025, 0.0025))
        if i == 14:
            return (v[0], v[1], v[2] + rnd.uniform(0.0, 0.002))
        return v

    verts, faces, bands = lathe(rings, SEGMENTS, perturb)
    fmats = []
    for b in bands:
        m = band_mat[b]
        fmats.append(rnd.choice(YERBA) if m == "Y" else m)

    # Bombilla: dos tramos rectos con un codo (proporcion tomada de la imagen).
    # Los primeros ~2,5 cm quedan metidos dentro de la yerba.
    path = [(-0.0012, 0.0, 0.0553),
            (0.0251, 0.0, 0.1148),
            (0.0480, 0.0, 0.1360)]
    tv, tf = square_tube(path, 0.0038)
    base = len(verts)
    verts += tv
    faces += [(a + base, b + base, c + base) for a, b, c in tf]
    fmats += [BOMBILLA] * len(tf)
    return verts, faces, fmats


# ---------------------------------------------------------- verificacion
def check_mesh(verts, faces):
    """Malla cerrada, orientada hacia afuera. Devuelve un resumen."""
    edges = {}
    for f in faces:
        for i in range(3):
            e = (f[i], f[(i + 1) % 3])
            edges[e] = edges.get(e, 0) + 1
    # Cada arista dirigida debe aparecer una vez y su inversa tambien.
    closed = all(c == 1 and edges.get((b, a)) == 1 for (a, b), c in edges.items())
    vol = sum(dot(verts[a], cross(verts[b], verts[c])) for a, b, c in faces) / 6.0
    xs, ys, zs = zip(*verts)
    return {
        "triangulos": len(faces),
        "vertices": len(verts),
        "cerrada_y_consistente": closed,
        "volumen_firmado_cm3": round(vol * 1e6, 1),
        "medidas_cm": (round((max(xs) - min(xs)) * 100, 1),
                       round((max(ys) - min(ys)) * 100, 1),
                       round((max(zs) - min(zs)) * 100, 1)),
        "z_min_cm": round(min(zs) * 100, 2),
    }


# ----------------------------------------------------------- exportadores
def write_obj(base_path, verts, faces, fmats):
    name = os.path.basename(base_path)
    with open(base_path + ".mtl", "w") as f:
        for mname, hx in MATERIALS:
            r, g, b = hex_to_rgb(hx)
            f.write("newmtl %s\nKd %.4f %.4f %.4f\nKa 0 0 0\nKs 0 0 0\nd 1\n\n"
                    % (mname, r, g, b))
    with open(base_path + ".obj", "w") as f:
        f.write("mtllib %s.mtl\no Mate\n" % name)
        for v in verts:
            f.write("v %.6f %.6f %.6f\n" % v)
        for m in range(len(MATERIALS)):
            sel = [fc for fc, fm in zip(faces, fmats) if fm == m]
            if not sel:
                continue
            f.write("usemtl %s\ns off\n" % MATERIALS[m][0])
            for a, b, c in sel:
                f.write("f %d %d %d\n" % (a + 1, b + 1, c + 1))


def write_glb(path, verts, faces, fmats):
    def conv(v):                      # Blender (Z arriba) -> glTF (Y arriba)
        return (v[0], v[2], -v[1])

    prims = {}
    for fc, m in zip(faces, fmats):
        prims.setdefault(m, []).append(fc)

    blob = bytearray()
    views, accessors, primitives = [], [], []
    for m in sorted(prims):
        pos, nor = [], []
        for fc in prims[m]:
            a, b, c = [conv(verts[i]) for i in fc]
            n = face_normal(a, b, c)
            for p in (a, b, c):
                pos += p
                nor += n
        for data, is_pos in ((pos, True), (nor, False)):
            views.append({"buffer": 0, "byteOffset": len(blob),
                          "byteLength": len(data) * 4, "target": 34962})
            blob += struct.pack("<%df" % len(data), *data)
            acc = {"bufferView": len(views) - 1, "componentType": 5126,
                   "count": len(data) // 3, "type": "VEC3"}
            if is_pos:
                cols = list(zip(*[data[i:i + 3] for i in range(0, len(data), 3)]))
                acc["min"] = [min(c) for c in cols]
                acc["max"] = [max(c) for c in cols]
            accessors.append(acc)
        primitives.append({"attributes": {"POSITION": len(accessors) - 2,
                                          "NORMAL": len(accessors) - 1},
                           "material": m})

    gltf = {
        "asset": {"version": "2.0", "generator": "mate_lowpoly.py"},
        "scene": 0,
        "scenes": [{"nodes": [0]}],
        "nodes": [{"name": "Mate", "mesh": 0}],
        "meshes": [{"name": "Mate", "primitives": primitives}],
        "materials": [{"name": n,
                       "pbrMetallicRoughness": {
                           "baseColorFactor": [srgb_to_linear(c) for c in hex_to_rgb(h)] + [1.0],
                           "metallicFactor": 0.0, "roughnessFactor": 0.9}}
                      for n, h in MATERIALS],
        "bufferViews": views,
        "accessors": accessors,
        "buffers": [{"byteLength": len(blob)}],
    }
    js = json.dumps(gltf, separators=(",", ":")).encode()
    js += b" " * (-len(js) % 4)
    blob += b"\0" * (-len(blob) % 4)
    total = 12 + 8 + len(js) + 8 + len(blob)
    with open(path, "wb") as f:
        f.write(struct.pack("<III", 0x46546C67, 2, total))
        f.write(struct.pack("<II", len(js), 0x4E4F534A) + js)
        f.write(struct.pack("<II", len(blob), 0x004E4942) + bytes(blob))


# ---------------------------------------------------------------- Blender
def build_in_blender(verts, faces, fmats):
    import bpy

    mesh = bpy.data.meshes.new("Mate")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Mate", mesh)
    bpy.context.scene.collection.objects.link(obj)

    for mname, hx in MATERIALS:
        rgb = hex_to_rgb(hx)
        lin = tuple(srgb_to_linear(c) for c in rgb) + (1.0,)
        mat = bpy.data.materials.new(mname)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = lin
            bsdf.inputs["Roughness"].default_value = 0.9
        mat.diffuse_color = lin
        mesh.materials.append(mat)

    for poly, m in zip(mesh.polygons, fmats):
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
    V, F, M = build_mate()
    if _in_blender():
        build_in_blender(V, F, M)
    else:
        out = os.path.dirname(os.path.abspath(__file__))
        write_obj(os.path.join(out, "Mate"), V, F, M)
        write_glb(os.path.join(out, "Mate.glb"), V, F, M)
        print(check_mesh(V, F))
