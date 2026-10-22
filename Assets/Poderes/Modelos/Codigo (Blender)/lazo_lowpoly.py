r"""
LAZO  (lazo criollo de cuero trenzado enrollado + tientos + argolla metálica)

Unidades: metros, tamaño real (~31.5 cm de ancho, ~29.8 cm de profundidad y
~4.8 cm de alto apoyado sobre la mesa). Eje Z hacia arriba.
El lazo descansa horizontalmente sobre la mesa (base en z = 0, origen en el
centro de apoyo). La argolla metálica queda abajo a la izquierda (-X, -Y) y
la puntera/botón del lazo asoma a la derecha (+X), igual que en la imagen de
referencia.

Un solo objeto "Lazo" con 6 materiales de color plano (flat shading, sin
texturas externas):
    Cuero_Claro, Cuero_Medio, Cuero_Oscuro, Tiento, Metal_Claro, Metal_Oscuro.

USO EN BLENDER (2.8 o superior):
    Pestaña Scripting > Open > este archivo > Run Script (Alt+P).
    Para exportar a Unity, poné una ruta en FBX_PATH más abajo, por ejemplo:
    FBX_PATH = "C:/Users/TuUsuario/Desktop/Lazo.fbx"

Fuera de Blender (python lazo_lowpoly.py) solo imprime un resumen de la malla.
"""
import math
import os
import random

# ----------------------------------------------------------------- ajustes
SEED = 17
COIL_MAJOR_SEGS = 28    # segmentos a lo largo del rollo circular
COIL_MINOR_SEGS = 10    # vértices de sección transversal (5 hebras/crestas)
COIL_TWISTS = 2         # vueltas helicoidales completas de las hebras

FBX_PATH = r"C:\Users\Tomas Abrate\Downloads\Poderes Modelos 3D\Modelos 3D\Lazo.fbx"

MATERIALS = [           # nombre, color sRGB, rugosidad, metalicidad
    ("Cuero_Claro",  "#DFC7AC", 0.62, 0.02),
    ("Cuero_Medio",  "#C5A684", 0.68, 0.02),
    ("Cuero_Oscuro", "#9A7B5C", 0.74, 0.02),
    ("Tiento",       "#B89872", 0.65, 0.02),
    ("Metal_Claro",  "#B8BCC0", 0.35, 0.85),
    ("Metal_Oscuro", "#595D60", 0.45, 0.80),
]
M_CUE_L, M_CUE_M, M_CUE_D = 0, 1, 2
M_TIEN, M_MET_L, M_MET_D = 3, 4, 5


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


def _coil_centerline(theta):
    """Curva central orgánica del rollo de lazo en el plano XY."""
    rx = 0.116 + 0.004 * math.cos(2.0 * theta) - 0.003 * math.sin(3.0 * theta)
    ry = 0.120 - 0.003 * math.cos(2.0 * theta) + 0.003 * math.cos(3.0 * theta)
    cx = rx * math.cos(theta)
    cy = ry * math.sin(theta)
    cz = 0.022 + 0.0015 * math.sin(2.0 * theta + 0.4)
    return cx, cy, cz


def _pick_leather_shade(n, is_ridge, rnd):
    score = 0.50 * n[2] - 0.25 * n[1] + (0.14 if is_ridge else -0.14) + rnd.uniform(-0.14, 0.14)
    if score > 0.18:
        return M_CUE_L
    if score > -0.16:
        return M_CUE_M
    return M_CUE_D


# ----------------------------------------- constructores geométricos cerrados
def build_main_coil(rnd):
    """Rollo principal trenzado como toroide helicoidal facetado 100% cerrado."""
    verts = []
    M = COIL_MAJOR_SEGS
    N = COIL_MINOR_SEGS

    for i in range(M):
        theta = 2.0 * math.pi * i / M
        cx, cy, cz = _coil_centerline(theta)
        ur = (math.cos(theta), math.sin(theta), 0.0)
        uz = (0.0, 0.0, 1.0)

        # Engrosamiento leve en la zona derecha y abajo a la izquierda
        bulk = 1.0 + 0.08 * math.cos(theta + 0.3)

        for j in range(N):
            # Giro helicoidal múltiplo entero de 2*pi para cierre topológico exacto
            phi = (2.0 * math.pi * j / N) + (2.0 * math.pi * COIL_TWISTS * i / M)
            # Alternancia de radios para simular las vueltas individuales de soga/cuero
            is_ridge = (j % 2 == 0)
            lobe = 1.06 if is_ridge else 0.88
            noise = 1.0 + rnd.uniform(-0.04, 0.04)

            rr = 0.028 * bulk * lobe * noise
            rz = 0.020 * bulk * lobe * noise

            px = cx + ur[0] * (rr * math.cos(phi))
            py = cy + ur[1] * (rr * math.cos(phi))
            pz = cz + uz[2] * (rz * math.sin(phi))
            verts.append((px, py, pz))

    faces, fmats = [], []
    for i in range(M):
        inext = (i + 1) % M
        for j in range(N):
            jnext = (j + 1) % N
            a = i * N + j
            b = i * N + jnext
            c = inext * N + jnext
            d = inext * N + j

            is_ridge = (j % 2 == 0)
            for t in ((a, b, c), (a, c, d)):
                n = face_normal(verts[t[0]], verts[t[1]], verts[t[2]])
                faces.append(t)
                fmats.append(_pick_leather_shade(n, is_ridge, rnd))

    return verts, faces, fmats


def build_closed_tube(frames, sides, mat_fn):
    """Tubo cerrado con tapas en ambos extremos a partir de (centro, tangente, ry, rz, tag)."""
    verts = [frames[0][0]]
    start_cap = 0
    rings = []

    for center, tang, ry, rz, _tag in frames:
        t_unit = unit(tang)
        ref = (0.0, 0.0, 1.0) if abs(t_unit[2]) < 0.9 else (1.0, 0.0, 0.0)
        u_vec = unit(cross(t_unit, ref))
        v_vec = unit(cross(u_vec, t_unit))

        r_start = len(verts)
        for k in range(sides):
            ang = 2.0 * math.pi * k / sides
            p = add(center, add(scale(u_vec, ry * math.cos(ang)),
                                scale(v_vec, rz * math.sin(ang))))
            verts.append(p)
        rings.append(r_start)

    end_cap = len(verts)
    verts.append(frames[-1][0])

    faces, fmats = [], []
    r0 = rings[0]
    for k in range(sides):
        kn = (k + 1) % sides
        t = (start_cap, r0 + kn, r0 + k)
        n = face_normal(verts[t[0]], verts[t[1]], verts[t[2]])
        faces.append(t)
        fmats.append(mat_fn(n, frames[0][4]))

    for i in range(len(frames) - 1):
        ra, rb = rings[i], rings[i + 1]
        tag = frames[i + 1][4]
        for k in range(sides):
            kn = (k + 1) % sides
            for t in ((ra + k, ra + kn, rb + kn), (ra + k, rb + kn, rb + k)):
                n = face_normal(verts[t[0]], verts[t[1]], verts[t[2]])
                faces.append(t)
                fmats.append(mat_fn(n, tag))

    rlast = rings[-1]
    last_tag = frames[-1][4]
    for k in range(sides):
        kn = (k + 1) % sides
        t = (rlast + k, rlast + kn, end_cap)
        n = face_normal(verts[t[0]], verts[t[1]], verts[t[2]])
        faces.append(t)
        fmats.append(mat_fn(n, last_tag))

    return verts, faces, fmats


def build_protruding_tip(rnd):
    """Puntera / botón del lazo que asoma hacia la derecha (+X) como en la foto."""
    frames = [
        ((0.128, -0.005, 0.022), (0.85, -0.50, 0.05), 0.012, 0.011, "dark"),
        ((0.142, -0.014, 0.023), (0.92, -0.38, 0.05), 0.014, 0.012, "mid"),
        ((0.156, -0.019, 0.023), (0.98, -0.18, 0.00), 0.016, 0.013, "light"),
        ((0.168, -0.021, 0.022), (0.98, -0.15, -0.05), 0.014, 0.011, "mid"),
        ((0.175, -0.022, 0.021), (0.98, -0.15, -0.05), 0.008, 0.007, "dark"),
    ]
    return build_closed_tube(
        frames, 6,
        lambda n, tag: _pick_leather_shade(n, tag == "light", rnd)
    )


def build_tiento(theta_deg):
    """Tiento (atadura fina de cuero) que abraza la sección del rollo en theta_deg."""
    theta = math.radians(theta_deg)
    cx, cy, cz = _coil_centerline(theta)
    ur = (math.cos(theta), math.sin(theta), 0.0)
    ut = (-math.sin(theta), math.cos(theta), 0.0)
    uz = (0.0, 0.0, 1.0)

    bulk = 1.0 + 0.08 * math.cos(theta + 0.3)
    rr_path = 0.0295 * bulk
    rz_path = 0.0212 * bulk

    major_segs = 8
    minor_segs = 4
    w_tang = 0.0038   # ancho del tiento a lo largo del rollo
    w_norm = 0.0022   # espesor radial del tiento

    verts = []
    for i in range(major_segs):
        phi = 2.0 * math.pi * i / major_segs
        cphi, sphi = math.cos(phi), math.sin(phi)
        # Centro de la sección del tiento sobre el perímetro del rollo
        px = cx + ur[0] * (rr_path * cphi)
        py = cy + ur[1] * (rr_path * cphi)
        pz = cz + uz[2] * (rz_path * sphi)
        p_center = (px, py, pz)

        # Normal hacia afuera del rollo en ese punto
        n_out = unit((ur[0] * cphi, ur[1] * cphi, uz[2] * sphi))

        for j in range(minor_segs):
            alpha = 2.0 * math.pi * j / minor_segs
            offset = add(scale(ut, w_tang * math.cos(alpha)),
                         scale(n_out, w_norm * math.sin(alpha)))
            verts.append(add(p_center, offset))

    faces, fmats = [], []
    for i in range(major_segs):
        inext = (i + 1) % major_segs
        for j in range(minor_segs):
            jnext = (j + 1) % minor_segs
            a = i * minor_segs + j
            b = i * minor_segs + jnext
            c = inext * minor_segs + jnext
            d = inext * minor_segs + j
            faces.append((a, b, c))
            fmats.append(M_TIEN)
            faces.append((a, c, d))
            fmats.append(M_TIEN)

    return verts, faces, fmats


def build_argolla():
    """Argolla metálica ovalada en la parte inferior izquierda (-X, -Y)."""
    theta = math.radians(-116.0)
    cx, cy, cz = _coil_centerline(theta)
    # Desplazada hacia el borde exterior inferior izquierdo del rollo
    ring_cx = cx - 0.018
    ring_cy = cy - 0.022
    ring_cz = cz - 0.006

    # Orientación tangencial al rollo en el sector inferior izquierdo
    ang_rot = math.radians(-18.0)
    ux = (math.cos(ang_rot), math.sin(ang_rot), 0.0)
    uy = (-math.sin(ang_rot), math.cos(ang_rot), 0.12)
    uy = unit(uy)
    uz = unit(cross(ux, uy))

    major_segs = 10
    minor_segs = 4
    rx_oval = 0.024   # semieje largo de la argolla (~4.8 cm de largo)
    ry_oval = 0.012   # semieje corto de la argolla (~2.4 cm de ancho)
    r_wire = 0.0040   # grosor del hierro de la argolla

    verts = []
    for i in range(major_segs):
        phi = 2.0 * math.pi * i / major_segs
        cphi, sphi = math.cos(phi), math.sin(phi)
        # Achatamiento en el extremo derecho donde la abraza la presilla de cuero
        flat = 0.82 if cphi > 0.4 else 1.0
        p_center = add((ring_cx, ring_cy, ring_cz),
                       add(scale(ux, rx_oval * cphi),
                           scale(uy, ry_oval * flat * sphi)))

        n_out = unit(add(scale(ux, cphi), scale(uy, sphi)))
        for j in range(minor_segs):
            alpha = 2.0 * math.pi * j / minor_segs
            offset = add(scale(n_out, r_wire * math.cos(alpha)),
                         scale(uz, r_wire * 0.85 * math.sin(alpha)))
            verts.append(add(p_center, offset))

    faces, fmats = [], []
    for i in range(major_segs):
        inext = (i + 1) % major_segs
        for j in range(minor_segs):
            jnext = (j + 1) % minor_segs
            a = i * minor_segs + j
            b = i * minor_segs + jnext
            c = inext * minor_segs + jnext
            d = inext * minor_segs + j
            for t in ((a, b, c), (a, c, d)):
                n = face_normal(verts[t[0]], verts[t[1]], verts[t[2]])
                mat = M_MET_L if (n[2] > 0.15 or n[1] < -0.4) else M_MET_D
                faces.append(t)
                fmats.append(mat)

    return verts, faces, fmats


# ------------------------------------------------------------ malla completa
def build_mesh():
    rnd = random.Random(SEED)
    verts, faces, fmats = [], [], []

    # 1. Rollo principal de cuero trenzado
    v, f, m = build_main_coil(rnd)
    _append_component(verts, faces, fmats, v, f, m)

    # 2. Puntera / botón que sobresale a la derecha
    v, f, m = build_protruding_tip(rnd)
    _append_component(verts, faces, fmats, v, f, m)

    # 3. Los 4 tientos (ataduras) ubicados como en la foto (arriba, izq, abajo-izq, der)
    for ang_deg in (88.0, 178.0, -104.0, -19.0):
        v, f, m = build_tiento(ang_deg)
        _append_component(verts, faces, fmats, v, f, m)

    # 4. Argolla de hierro inferior izquierda
    v, f, m = build_argolla()
    _append_component(verts, faces, fmats, v, f, m)

    # Centrar en X e Y y apoyar exactamente sobre la mesa en z = 0
    xs, ys, zs = zip(*verts)
    cx = 0.5 * (min(xs) + max(xs))
    cy = 0.5 * (min(ys) + max(ys))
    min_z = min(zs)
    verts = [(x - cx, y - cy, z - min_z) for x, y, z in verts]

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

    mesh = bpy.data.meshes.new("Lazo")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    obj = bpy.data.objects.new("Lazo", mesh)
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