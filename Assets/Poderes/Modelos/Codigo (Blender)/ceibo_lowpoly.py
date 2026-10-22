"""
CEIBO LOW POLY  (ramita con 2 flores grandes, 3 flores colgantes, 4 pimpollos y 5 hojas)

Unidades: metros, tamano real aproximado (la ramita mide ~30 cm de alto y ~19 cm
de ancho; una flor grande ~6 cm). Z hacia arriba. El frente es -Y (se ve de
frente con la tecla 1 del teclado numerico en Blender).
El punto mas bajo (la punta de la hoja de abajo) esta en z = 0. El origen esta
en x = 0, y = 0, justo debajo de la base del tallo (el tallo termina ~1,7 cm
mas arriba, en z = 0,017).

Un solo objeto "Ceibo" con 13 materiales de color plano (petalos, calices,
hojas, tallo y puntas crema), flat shading, 2040 triangulos.

A diferencia de la pava o el mate, este modelo NO se arma con formulas: la forma
de una flor no se puede escribir con primitivas. La malla se calculo a partir de
la foto (silueta de cada pieza, volumen inflado, color de cada cara tomado de la
imagen) y viene guardada comprimida dentro de este archivo, asi que el script
sigue funcionando solo, sin ningun archivo de imagen ni de modelo aparte.

USO EN BLENDER (2.8 o superior):
    Pestana Scripting > Open > este archivo > Run Script (Alt+P).
    Para exportar a Unity, pone una ruta en FBX_PATH mas abajo, por ejemplo:
    FBX_PATH = "C:/Users/TuUsuario/Desktop/Ceibo.fbx"

Fuera de Blender (python ceibo_lowpoly.py) solo imprime un resumen de la malla.
"""
import base64
import struct
import zlib

# ----------------------------------------------------------------- ajustes
FBX_PATH = ""          # ruta para exportar .fbx desde Blender ("" = no exportar)

MATERIALS = [          # nombre, color sRGB, rugosidad
    ("Petalo_1", "#8B0D25", 0.6),
    ("Petalo_2", "#B41025", 0.6),
    ("Petalo_3", "#CC2540", 0.6),
    ("Petalo_4", "#DB2532", 0.6),
    ("Petalo_5", "#F35F5A", 0.6),
    ("Petalo_6", "#F88A8A", 0.6),
    ("Caliz_1", "#A22F2A", 0.7),
    ("Caliz_2", "#DF5A4F", 0.7),
    ("Hoja_1", "#656646", 0.8),
    ("Hoja_2", "#7F7E54", 0.8),
    ("Hoja_3", "#9B9E73", 0.8),
    ("Tallo", "#72512E", 0.9),
    ("Crema", "#D4B28D", 0.8),
]

# Piezas (nombre, tipo, primera cara, cantidad de caras), por si queres
# identificarlas o separarlas despues (Edit Mode > Separate > By Loose Parts).
PARTS = [
    ("Bud1", "petal", 0, 52),
    ("Bud2", "petal", 52, 52),
    ("Bud3", "petal", 104, 58),
    ("Bud4", "petal", 162, 58),
    ("CalB1", "calyx", 220, 36),
    ("CalB2", "calyx", 256, 38),
    ("CalB3", "calyx", 294, 44),
    ("CalB4", "calyx", 338, 46),
    ("Hood1", "petal", 384, 60),
    ("Flor1", "petal", 444, 128),
    ("Flor2", "petal", 572, 122),
    ("Flor3", "petal", 694, 94),
    ("Flor4", "petal", 788, 100),
    ("Flor5", "petal", 888, 80),
    ("Cal1", "calyx", 968, 58),
    ("Cal2", "calyx", 1026, 50),
    ("Cal3", "calyx", 1076, 42),
    ("Crema1", "cream", 1118, 24),
    ("Crema2", "cream", 1142, 42),
    ("Crema3", "cream", 1184, 36),
    ("Crema4", "cream", 1220, 40),
    ("Crema5", "cream", 1260, 28),
    ("Hoja1", "leaf", 1288, 68),
    ("Hoja2", "leaf", 1356, 76),
    ("Hoja3", "leaf", 1432, 66),
    ("Hoja4", "leaf", 1498, 88),
    ("Hoja5", "leaf", 1586, 78),
    ("Tallo_main", "stem", 1664, 166),
    ("Tallo_left", "stem", 1830, 66),
    ("Tallo_up", "stem", 1896, 36),
    ("Tallo_t3", "stem", 1932, 26),
    ("Tallo_t4", "stem", 1958, 36),
    ("Tallo_stalk", "stem", 1994, 46),
]

_NV, _NF = 1086, 2040      # vertices, triangulos

# Vertices: int16 en unidades de 0,1 mm (x, y, z). Caras: uint16 (3 indices).
# Material de cada cara: uint8.
_V_DATA = """
eNot13d4FUX3B/Cze3dndvdSpAoYxEDohiJFFAGlQxJDe2mRIiCIdARiaCIGEVGaEF4EA1JFBUSQgAqCiIgvYqUrvUiRktCS
nZnz+948v38+z3nOzM7MzsydndsoPMnLo7Xg1Ghj+Gp0VniGO0Q/D89yfPRoeJ7zA6ku8Z6gmrrM7wePq394atAITgx6q6s8
OZiprvPiYI26wduCDfCPYCU8FyxW/7ITzURpQrStusJDo3fDi+gr1uam6BL0sj36SniKt0WT1AY+HySpanQ+iKpPuFk0qipT
s+g74QbOhCUpM/p1gUcHg11wS7C/wKdPgryCKL0fpIQPUXowBXU6BRvC0lQz2B+WpWjwXViGnGAfYiuwVHkqHtRVj1DLoD7s
ENRQFahNUAr5IcH58GG0sxLPfh+MDUvQyaBRWIyuBn5YhP4JrNCmC7AMPF4Q0LnC3k8Ef4RXOBW2otRgeHiZP4XP0KfBYf0L
vxnNgUOjH8L60UyYH6TDPYUOD6bBtGAUfLbQaDAFHvdnw3V+7Kn/+p/Dxf5+ONO/DjP8KuYXHu3XgOP9BDjV7wD3+gOgHbwA
mwTd4cigAdwUGDx1IbgLnWjMXtGrcHL0kh7HXnBJ1yQv+EFP4Y/gY/RR0I9v8t/Bc3yLTwYN+Tb/GSTAb4OinMfvBB5sEzww
d7h88Lu5x3/5n5n7nO1/YB7wIH+myeea/iRTwEX9cdDz1yN/zFMmj//yYu1oryLnsvDbov2W/mC+wXP8Gfwvn/AX8XUuGizj
a2h/KeJ3glgmO5iC0oPBLfMFp/m3TGNK82vwJt4JG9BOvz/ncEbQn6tRBvYb0dkgC/4ZzIXbgilwbdANzg8S4SKsONHYoBns
EjSB7YIXYaVgEEwMFsD4YDcsGxyClYMD8IngD9gjOAqzg31wb7BM/ciDYRMajD1WgUpiv1XAftsNtf8/eMm3sLt2+fXhZ34/
VYo2wcrw5cLMKLjFHwSPFMp+HyiC7vCRIPZUu//fn1VgcmDQZkJwAJYPauKXeM2vqVLpmj9L5/I27Jlc7JlRcKY/FPbyJ8L6
hfnAXwiL+mug7X8JS/qnYHlfmFx+zI+Dtf36sKXfDA7AvsrliX41OAO7LpfnYR/m8kbsyVze5M+B3/hbdQ73g4nUzw/MLqxO
YKphdZqaYjTSS4L9vblwlPcHnOrdgfO90lyMVnn14R9eLViA/VCMbD8eRv1SMPAZNe94N+FNbyP815sCy/oToOePMLE6PeFt
rz3c57WB2V5LmO7NNEd4A2xHG7w/zXHeBVvTLm+saseJ3uuwZmF8SWbAH+USuFi+B9+Xn8OxcgccL4/DQTLQ7XiojIdJMgW2
lJlwgPwVjpTXYaY8CxfJg3COzID7ZDd4u7C+5/WE9bxJsL83DI72ysAx3q5YX162iuWvqjvmiLyqlvAR2VP7GElPnQn/Vja3
8v5Ws+FGc9809YbBtl5DWM8rC09Jpe+bn+Rl+IE8BGfIBXCc7A4HyEqwnLyi7ptD4j7cI/Lga+IWrCtclJKoAH9zn4UfuYPh
aHczHOzGWk5zE9DLIjcJ/uouhMY9BouKm7CGuBsbjzgPh4pKfN8sFnHwgigP68mycLIsAocU+pXMQ03HO6eTzFJxTq/mpaKX
ed5o+BFrsV13RpvbdTbXEBmmr5kkM8x8niQn6N5mvpigF/J80dWkmTFwIY8Ra3U/E8K5HIrvzBizA07lHUKacSZZSjORk+VS
PdVsc5fq0bzN/cVMNXXkL2YM15E7zFJzEXbli7KKmWbYrWJGMbsdzGpzRXYwSXxF1tFzTJyoo1/kOPG1mWuSYD9OEkX5Y1NW
KvOxiUgb8SHhwSXiJ2SGiWOwhpgKA1Ef/uo+BJe6N/XHmNUbMME9Csu5V+BhR8EFzgPY2bkLizulUH9/JPbs65HuMDuyEq6N
3IQbIk3R19bIFHgjsgq2dw7AVc5NaJwLMNU9Dj9yD8E8dyMsIT6BvcWaWE0xHu4Q7eEnohY8I26ba7qKe9u8xFXcg+Zf/bFz
0Azgj52mnK/fdptyD37bzTT5WjuZpjtrZ7UJdRP4H27idOdQ58P/cL6TY8i87eaYTqhflh2T7JTlJE52WnARzHALbo0Zft7E
mT6wIfdxk01FM8NJNk/wDGcMp2EXjeE78FNuZPq6n3IF7us2NK1MbqShKcq5kVS1Db/Qh9VWnizPhFt4hswOP+NXZFq4llNl
n3Ad95F1wuVcS5qCLL4nDhXM4b1iW8F0HimWF6RzBbGuYBwrdx+86e4vmMS1xamCTB4i7ha8y6tEYvg+/yAGhR/wP6JFOBf7
s3z4JqeIvILXMPIrBSN5g3uqYCCnudcLerHlyrATr3fiwlRe6jRAvMQZFfbkvc7KcCB7bk44jJu5B8OxPNY9GU7mb92r4Vsc
FfnhPO4pmqilvExMUat5v5ikNnGCPBc+yw/EudChB+KlMJ5PuC+Fv8OuYUkeCr+HWWETjDYLt7IhIhn5Aic5XA/Tw878qEwP
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
/g==
"""
_F_DATA = """
eNo9lmV0lle3RY+8EFwCSYAQJAkWIEAIFtzdneLSEqAQKE5xl2DFrVAcihR3iktwihYvGtyCkzvPhvv92HOttc8+8rwZjEES
lVT5qiTKTzSdSoBLoNLjk6pk+GTST6AS4hOiyVRylQKmQH1F/SQrpZWBBnX0gT5oAs70QA9nZlBpVFrlTzn1IbvkI3MZoL+o
URZnUW/2poap2evNngzQX9SK80Y9zHhDb5mx4jxoKvq+0I9+anwqmArvRTchdN/jpRLz5V5UYqEv9EMTSU4k6ynppoDuO1Ph
U8KU+MwqiwqFoWhulUdlhVnRzCpMZYKZRMNUQVgQzU9lVgVkPZfKrnLAHKI5xeVCg1Q2OkFUNjmrMCwimkVcVrQwOS8+Lxoq
OZSUhRyKdyuhKlzeUuj7mwLJLgWqEHI4LCSanTsLyVty0QuXFCK3h8NCooHigtBg3hQE3RsLqowqAAagmUQzSc4nv0c++V3y
U/n43nyi7vtdClPVVHVVHzYQrSSuGlpflVZl8GVEy5JdKqtqqlqqIWwkWl9cze97qsKqopVUZVhZtIq4qmh97qjBbA20JqkG
dLmhKq5KcE4JtBT31YcN0FK4hrCRzNRStXG1RYuTXSqu6lC1oetVlHdXZLqSqsBby8FyqGN5WB6tL7mBrFckVYAul+TEEtC9
oxS+JCyJr6eKqmKwGFpXtK7kCO6sC+uiEdKLkLUIeVOEvKubaqx6wl6iXcV1QzurLuTOJKfNxXUWba5awBaincR1RluolqoH
/EW0kzi33kH9qH6CP6FRot2/545klzpyfhf1M+f/LNqV7FJX9QN39IS90NZMt4Kt2NGDO1pxfivRKLJL3VUbfGvYWnx7bmlD
tRdGwe6iHVUkLhLtQYpk57fciexSJ+5srJpxazP0B1Iz6HJb1Y7T2lLthG2gu8fNNYVNRRurJrAJOlKNUmPgWHSA+lX1h/3R
gXT7w/6sfMsuufUxqh/sR38ofggcgh+CHwgH4kfiR8AR+BH4oXCozA8TN0J0mBoOh6OD1GB2DqIGywmDJLkXjFJ92dcX7Ufq
C/8/j4ajRXurPrAP2le0r+Txap4aB8ehc6nx0PUmqflqIpyoFqjp+GlwGn4afhKchJ+Lnwlnih+vomE0Ol5NEI1GJzITDaOZ
icbPlakFagZ+OpyOn4mfAWfgZ6s5rM+m5ghnQneHu3EynCI6SU3FTRX/m3SnoVOpybLym+yaBWehi9QfaiPchC7CLYQLSevw
m+EW/Gb8CtwK/Aq1kuzSSrp/qMX0FosuUUvhUnQ9K+vgOtYW4tfD9bJ3o1oOl+OX4xfDb/uXiluOLlXLRJejazlhDVwjuo57
13Cv0z/FrUFXU5vhFvQvuecvue8vtUH9Dn8XXSjOrW/m3auYXoWuJq2CLu9Qp9QJeFKdVgfwu+Au/An8QboH8QfxB+AB/HZm
T8CT6E76jjvo78LvhDvxx5k4Bo8xtR1/HB7HH8MfhUfx+9VutQfuQXdR++F+0QPi3DsOM38QunccxR+Gh/FHRI9IPqgOycRh
NEbuiZH7tlExMEZ0q7htqLtxL9wruk/cfnSv+lu4D32mHqpH6jn1UD2gnsHnonfFPUCfqH/VNfWUcnqb7NJtdZX8BD4Vva3u
4O6gj9QZdRaeFX0mzt1xn7PuwXty9l3cA+n9Rz2Dz+WuOyqWcx6j/9GNhY9ZiZX8mHLr59R5/Hk0lpmz+LPMnJXs0jl1i3dc
g+69jtfhdencUDfhTfQ6dUtWbqh/1AV1EV5EYzn5H075B41Vl9UV/BXRJ2SXnsKrpCu4q7LrErwkGivO7b/Enlj4GA3WSmud
jVL6k/qsMkB/7dRXe2t/nUb7aY9OoENgbtSbfgjd3PRDdEZyblIA3lcH47PRD2Y2BJcbDeZkgzeilo6hrDBYVjw6TgXqtypO
vVVBaKBOB9PrIPSNcu6teqPe0Y+Dcczk1Kn1C5WLcvpS5ZT0UqXT2XVKnZ7KoZPhs8Cs+Cw6qbhkaDz/IQ6G2fjeUP1FfVX5
9Ff1RYXyzV/wX+Q3+Ez21/lEfXUGnD/fFcpkPDPx7AqmH8rr89GPJ4Vqt5KNXy+D/qA+qQ/8ih/UR3Gf0BB+u4T8IglFM/L7
uBSg8/DuVyqvfiXf8VLl0bl0XtF0fGdeUnqdXKfgm5JTKWB2viM5lYOcXbrJ8TmZT8V0KuZT4VPClOJTs+ZWUvM7ZNdp+R18
mE+nA/Hp8UFodrIPKQc+I3M+TAXwt81ITkMK0En47bLArGgm/Z6vysz3Oc1AdslfvyNngpm10wzsz8xuf9YDpftOubsCyT6s
BKHe5MykNPwOr9RrfofXyukb/pav+cu/wadjza2k5/6MOj8vKCDvyYJPgs+KZuTXLEAFUF5kl5x66US4ROIT6cT4xGh+6HYm
QYvpNrooLKrb6g74pvpH3QxfTpfXFWAFtJ2urKvo9lRlXYlqB9uLVtFVcVXRCF1c/wQ7irbhxI5UW3wx6UagNTn9Z9iVG2rh
a8Ka4mtzdy1udtpUXC36Hcid8V3QFroit7akKuIrcXdLbnZaVbfCtRatzvmtOd9pU3JXUjN8dV0DX0O0JtmlmpxdW9fh9Dqi
dcku1dWldGkdCTuh1djTCrYWrSquGlpCl2SiBFWSbyyO60gVZ19pXYadZdDOnFWPE+uh9Un1oMuF+LLOsAvfVghXH9YndePL
InUUu1vqsvz23WAUWgbtBqNEI8W5O7rJ3yfq+9+pItklp+43iuKUivoHXhipmzNdCv8Tb2z+/a0uu+S0DSvN6bbVRXhbIViI
txWlXwQWkX4bVorQbavDeW0D2OD7y8Oh6zXmr9FKN+EXas/97cgtSe25vwW+E74ld0Xim+M7yS2FYWG0gQ7TBWFBNFw0XHJj
TmzIdCNObcjuxrgm7G/Ie37AN+c9TfEN+Ts3wrciNWS2EXmcHq/HwrGi4/UC+Ds6SU/Ws+BsdAF5At0JotFkl6L1VD1PT4FT
9HxmJ6OzKafzyC7N1311Pz0EDhEdKq4vOoZ7FsDf0TG4OXAuaY4eLW4MuoA75tCbKxqtJ+Imip/E+XM5fxJ5EtmtTNILdU/d
Sy+ieurpurueAWeKdtc9cD3ET5PudPQ3agacifbQv+DdlNNfOGEm5XxPzp3JqT3x8/jamdR8/FTZ+Rs6h9eM4gWjeNEo3j1H
0mj29dJ92NkH7aV7i/ZB3ff3gX3QYZw5A86UMxeKG8ae4fSHwWH0v831oZwuFOfOHcW9I+FI7nW/b3/YHx3B3uFwuPxd5uFm
U/OZm4WOlDxYZgd/3zNAD4KD0MGig/+Xf4W/yvqveiAciP6hF+vj8Di6lFoCl6Ab9Dq9Hq4X/UvcBvSC3qf3w/3oGn1a/wn/
1Gf0WuY2wA2ip8Wtlf5pJtZSZ/RG/Aa4AR+D36hPUM6v1Kvwq9A/6a+Gq+mvxsfAE+JjWF8tc3u4/QA88P0le6DrLdfn9TK4
TP/D9BZ9ltlzoivJ50gr9SbcRujuPqyP6O1wO3qSOqpPUUe+5+0kp+fFbefMZfiT8BTe/V4n4SlR99udkt/vJHQTy9BjrJ+E
p9Cj6Em54Rhnndfb4DbO2YY/C8/hV/C+s/AcuoL+cric/lm8667Ab+Z7YuAJdDNuE9xE2qEP8SU7qEPyRTskbWfvFr2VvVvR
baSt0OW9/P0uQPf33MmeHdCdsVMfFB5C3S+8G+5G/2ZuL3T7drG2E7rZA9Qu6Hq7RXdLfqAf6vvwPhqr76Gx1L3v2aWHUo/g
I/S9fq5f6A/Uc32Xua8wXvQeE/FyRqx+THbpsY7TL/Vn/U5/Qa/oq+QrpKvsf4l/gX/JmS/wH/Av9Ft9UV+Cl0Q/i3vL7s/M
vUJfobf1R/bcZscnfQd/G97GP8M/hU/FP2fmGTNOP4p7Rv8p/gl8gv/K+57wyieiH8kufdL/4e/AOzLzERdPuf5dskt39WVe
dAle4k2X+aY4+E70s7jL9K9xcxx8x92v6b+C7hve4l/D1/jX+o24t+ht5m/BW8zf0DfRG9RNzriq/+WUf9FrpH+hy9eZvwav
MX8Lfx1ex1+XfddlfwrjZ/xNCpPRpDOJTRJ8YnwSk5B+AMxEPwDvg/PB+xhfsku+Jj39FCYD5fp+ZB9SOjStOB/UyyTiRC8q
kdAfZpSOH6d7Ueno+UnXC5+Ajgd6RBNyl4e7Epo0nJUeZhBNYVLiUqJpSCmhy8ZYpg3lNECcYXcAb9WoFvU1CirUsKKhW/Pn
m5PyhqRoMs71hxnR5FQymEzUn/VkMpdK7kwld6cyqY039EYd08iqtyloMpssMAvqmBVmRXOaXKYwLIJmM+EmGAabQjA7/Wz0
nYaLy0Y/P74oLIYvis+Ny40PM4EmCAahWakwGCZaUJy7rwDz+WF+5oPxBWABfJBokOQw0TDJeU0od+TlhlDekN3k4A05RHOS
XcrJ3eEmBIZwTgi+MCyCL8z3OBeCFmV/Pk7JJ5qf7JLT3CYPLg+al5QHulzTVDAVTS3KaSWyS04rmcq4ymh1UjVYjVwbX93U
oWqZ8uypCWuh5XHlYDlSOXxZWBZfFl8b1sFXMVU5owrltKa4KtKvyT1V5L7anFyD6RpoaVOGnaUpp7XFlWatNL4ELIEvYUqJ
K41G0K8B3f4IU5xuBOW0trgI+iWZKwHdviamqWkBW4r+YJrjmqPNqBawpWhTcc3QJqTGsDG5oWmENqScthDXkH59fD1YD18P
3xx+O7euuHpoA/r1YX05pwWuIdXS9DA/mY7mF8ppJNmlSNPadDOtYCu0KxUFu4v+LK4r2s60J7cjtTdtTFtcG8pplLg2rESx
x2kb0W6c2EbO7sJNnWFnbuvBfU47i0aaTrAT+iNv6gF/QX/EdYFdSI4dYAe0C+/oCXuhLveEvUTbi+uA9mQtCt9d1OXupPZm
kPnVDIQD0cHUIOh6AyQPkN5gM8QMhUPR3qaP6Qv7omNEx37P/cguOR1hRuJGivYmu9TbDCePgWNFh4kbjg6lRsHR6Bj2j6I/
WtTl0SS33s/0x/cXHUB2yal742h56yh542h56zgz3kyAE9Cp5jczBU5BZ5qJZgacgc4288wsOMvMN9H4CXACfhY+GkbjJ+Dn
wrn42WYOOptyOk/cbPpz5Z65cp+7Yxqchro7JsFJ6AJqpvld7l8AZ+FnodGSo0lOZ4lzd0+XfdNlv+NkOFnUnT1Z7nCcLivT
zCKzxiyEC9E/zTKzGq5GF5k/zBK4BP3DLBZdgi5lbS1cJ7qM6XWybwm9tXCd6CJxbv9azl2EXyTnryG7HWvMcrPCrIQrUXff
KrhKdBndVdQy4Uro5k6Yk2Y9XI+eMlvMVnOa2mI2U6fgaeltw7sVp9vMOdx50W1mO247GiNnxMhZx8xx5o4xddwcMUdxRyin
p8QdYeWM2cTpZ6lN+M2snKXr9Dj5NOm42ShrG/FOj3P6Weq4OcTNO+AO7t6B3w7dO3aag7id1EGpQzJ10Ow2+80BeADdb/42
++A+dL35y2yAG9AY0ZjveSP3beA2pzHi3Nph7joED3HXKfxh3nkYf4R0GLq8x+zl5D3UXuF+uF86+3nBHnnLLt60E7r3HqB2
wV2iu8W5t14z100cfIe+Nf+YC/CC6EVxb0UvmjfwDXrJvEYvUa+/Z5cumn85Iw6+Q++Z/8xdeBe9zNwl6PbFcccNJm6gH0k3
zCfJN0k3oMsPzSNWHtJ9BONYeUj3nbmFuwlvku5w7i14C70telvyY/PcxMJY9Bn1BD4RfU73iaw/MU+l+wyN444XnP1C9BH7
XlCPZP8LSc/Ne3OVr/pAOY0ju/QOvULnA3UFH8fsh+9nvSS79JKvdmuX8U5fmle4V7L+Wrrut3lFvZeV13zpR/MAPuAbH+Bv
wVt495334X30geiD/+V70P3e6W0am9ZmoNJYaz02Pcwgmtb64HxQP/vZfDF+9ov5bLyZS2+9WUljE9skNhFMhFq6BhpW0jHv
B92+5DaFTWiTUylsKpva+sOMaHru8Gc2I+pN8ocZyf6sOeeNunOTwqRoADM+NpO8yeADYCa8O9kLeokmE5ccTUp5QS/RROLc
WSm5PwFMIJqK+xJwWyp5XwKYQDSFTKRE/Xmfm0iABnC7L7f6ivqRXfKzmrcEwEyo+5Xi+fp4frEA6zSTjZff7qtxvXhRxXS8
0dZpgDg3l9lmsVVhNdEsNisuK1reFrThtgJV0Ja1wTa7LUcF20K2sK0Mq6DhaGVYRbS8OLensC1CLkwqwp4ctjqsgZZFq3NO
DTSMc8vDCmhx7q8Kq6ElqOLwWy+rDaQbiJYlBbL3Ww4muxQsb8oGs6GVua8it1ZCc9s8Ni/Mi1bn7pzcmhMNYi4QujMiOKUi
rMRJEZxfFpbj/OL4CBiBL8mpFW0pZqrgy5NK4Srgy3JuKU4tx3pZciVSORtqc9kQGIK620NgiGhe6YaiOVkrDcugYZxZGpbh
zNI2n82Py4+W5nQ34d4dKjmU5DSXuFA5ozz3luFe957q5FKkGry7oi0Gi/Gm0pxWgG4B0TCyS2FMFLFFWS+KFiMVhS7Xtw1s
V9gN7WHr4X6h6uHr0/+FvtMGtiGuIRplf7IdbXfKaVeyS93Qrqx3l7ku9mfbGXYW/dnWgrXQSGY6wo7M9MBHcn4k/kfOioLd
0fa2g+0Je4m2E9cerStvqsuOerCHrQPrkNy5NWFNtA792rA2/dr4zrAz3t1eW5J7SQ/bCXai3wkfCd07onh5I17QCG1MagRd
bmOb2xawBdqUN/0If0Sb2ma8rymvc9pB3Lf1KJloyt6m+MawsfjGtglsgvZkzw/M/yDanOxSc9uWagPbiG9Hvy19p83FufXW
vKQlbPn9Va3ht14r6bZGe9s+9lf4qx2A62sHwoGkvrafHQQHkfrZ/nYwHEzqz+wAOIA0gLmhcKgdghtoh8FhpIHMDYfDSYOY
GwFHkNyOIXAIaQhzo+AoOxI3zI6Go0nDmBsDx5CGMzcWjiW5HSPhSNJI5sbD8XYcbrSdACeQRjMXDaNJY5ibCCeS3I5xcBxp
HHOT4WQ7CTfBToFTSBOYmwqnkqKZ+w3+RnI7JsFJpEnMTYfT7TTcFDsDziBNYW4mnEmaytwsOIvkdkyD00jTmJsD59jZuBl2
LpxLmsHcPDiPNJO5+XA+ye2YDWeTZjP3O/zdLsDNtQvhQtJc5hbBRaR5zP0B/yC5HQvgAtIC5pbAJXYxbqFdCpeSFjK3DC4j
LWJuOVxOcjsWw8WkxcythCvtCtxSuwquIi1lbjVcTVrG3J/wT5LbsQKuIK1gbi1ca9fgVtl1cB1pFXPr4XrSaub+gn+R3I41
cA1pDXMb4Ua7AbfOboKbSOuY2ww3k9YztwVuIbkdG+AG0gbmtsFtdituk90Ot5M2MbcD7iBtZm4n3ElyO7bCraStzO2Gu+0u
3Ha7B+4hbWduL9xL2sHc3/BvktuxC+4i7WJuP9xv9+H22APwAGkPcwfhQdJe5g7BQyS3Yx/cR9rH3BF4xB7GHbBH4VHSAeaO
wWOkg8wdh8dJbsdheJjUm3+jfXBHmOnNv9G++KPM9ObfaD/8MWZi7Al7Fp61Z3An7Tl4jnTSnrLn4XnSKXva/gP/IZ1m9gw8
QzrD3EV40V7AnbOX4CXSOeYuw8uk88xdgVdIbscFeIF0gbl/4b/2Ku6SvQavkS4xdx1eJ11m7ga8QXI7rsKrpKvM3YK37E3c
NXsb3iZdY+4OvEO6ztx/8D+S23ET3iTdZO4evGfv4m7b+/A+6TZzD+AD0h3mHsKHJLfjLrxLustcLIy1j3D37WP4mHSfuSfw
CekBc0/hU5Lb8Qg+IsXwW57AxTITw295Ev+YmRh+y1P4J8w8s8/tG/jGvsa9sG/hW9IL+9LGwTjSS/vKvoPvSK+YfQ1fk14z
9wF+sO9xb+1H+JH0lrlP8BMpjrnP8DPJ7XgP35PeM/cVfrVfcB9tPIwnfWROeT5S8bjPVns+UcrjdnwhfyE9413PcV/Z8Yx3
vcDHs+MZ73qJVx7tMR7rSQQTebxwHk9imJjk8STwJIFJSAk8CT1JYVJSQma9oBfJi7nkMLknGS6xJwVMQUrMXEqYkpSEuVQw
FcntSAaTkQxnW1xyZgxne/ApmDGcnQCfkpnUHm+PH/Tz+OLSeNLBdKQ0nrSe9DA9Ka3Hx5MBZiD5MOsLfUm+zGWEGT3+uHSe
ABhASsdcJpiJlJ65zDAzye3wh/4kf+aywqyeLLgATyAMJAUwFwSDSJmYC4bBJLcjC8xCSs27vHFZmUnNu9LgA5lJzbvS4oOY
yebJ7skNc3tCcDk8eWAeUg5PTk9emJeU05PLEwpDSbmYDYEhpBDm8sP8nny4PJ4CsAApD3NhMIyUl7mCsCDJ7cgH85HyMVcI
FvKE4wp4CsPCpALMFYFFSGHMFYVFSW5HOAwnhTMXASM8xXCFPcVhcVJh5krAEqQizJWEJUluRzFYjJSNb8qOi2AmG9+UA1+c
mWx8U058CWb+D7wWoas=
"""
_M_DATA = """
eNrtk9tOxDAMRJsmoW3g/78XTU4Hu1VX8IaQGGk3tuPL2HFL6b3WUlqrdZ1ozb/W+oSkdS0TyL3jKUut2RtrZONGd7IJnMqh
ymi6VybBp6xUq7UnmIXvVRMWcIWpKm/b2wTntukMGWT5rj3jJz7fZ3AWJkhPYh3dB0pZTtBz78viHtd1WTily26bPMsJPGqV
3R7YS9HMeGP5O4NjDDhgj3xiq/krHuZ6F16DlyW/JGrwb17ag9gLZWRzFEcFbwub5P0wR05z9ySZojoRZ3jhjeS9MSMmEGCD
2cn2Bb8ROxhbre79TqrtHfV+5q/Id4pXdwHPK/fLd4iWmTPzmIhuYqu96d59a3n/XiFu7+c1w/M3ca0sfPwZ7PtxjHGc2CfG
hDTZkJDxGUNaROKNF3LodygWj8iIhv0pJu6pazb7C5hTILgBc88euUtXCpYRnfk4Dv06P88oMmef93/8Cj4BBKU2AQ==
"""


# ------------------------------------------------------------- utilidades
def hex_to_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def _decode(text):
    return zlib.decompress(base64.b64decode("".join(text.split())))


def build_mesh():
    """Devuelve (vertices en metros, caras, material de cada cara)."""
    v = struct.unpack("<%dh" % (_NV * 3), _decode(_V_DATA))
    f = struct.unpack("<%dH" % (_NF * 3), _decode(_F_DATA))
    m = list(_decode(_M_DATA))
    verts = [(v[i] / 10000.0, v[i + 1] / 10000.0, v[i + 2] / 10000.0)
             for i in range(0, len(v), 3)]
    faces = [(f[i], f[i + 1], f[i + 2]) for i in range(0, len(f), 3)]
    return verts, faces, m


# ---------------------------------------------------------- verificacion
def _dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def _cross(a, b):
    return (a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0])


def signed_volume(verts, faces):
    return sum(_dot(verts[a], _cross(verts[b], verts[c])) for a, b, c in faces) / 6.0


def check_mesh(verts, faces):
    """Sin bordes abiertos y orientada hacia afuera. Devuelve un resumen.
    "cerrada" = cada arista tiene tantas caras en un sentido como en el otro (no
    hay agujeros). Donde el frente y el dorso de una pieza se tocan (la orilla de
    una flor o una hoja) hay aristas compartidas por mas de 2 caras: se cuentan
    aparte y no afectan al modelo en el juego."""
    edges = {}
    for f in faces:
        for i in range(3):
            e = (f[i], f[(i + 1) % 3])
            edges[e] = edges.get(e, 0) + 1
    closed = all(edges.get((b, a), 0) == c for (a, b), c in edges.items())
    pinched = sum(1 for (a, b), c in edges.items() if a < b and (c + edges[(b, a)]) > 2)
    xs, ys, zs = zip(*verts)
    return {
        "triangulos": len(faces),
        "vertices": len(verts),
        "cerrada_sin_bordes_abiertos": closed,
        "aristas_pinzadas": pinched,
        "volumen_firmado_cm3": round(signed_volume(verts, faces) * 1e6, 1),
        "medidas_cm": (round((max(xs) - min(xs)) * 100, 1),
                       round((max(ys) - min(ys)) * 100, 1),
                       round((max(zs) - min(zs)) * 100, 1)),
        "z_min_cm": round(min(zs) * 100, 2),
    }


# ---------------------------------------------------------------- Blender
def build_in_blender(verts, faces, fmats):
    import bpy

    mesh = bpy.data.meshes.new("Ceibo")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Ceibo", mesh)
    bpy.context.scene.collection.objects.link(obj)

    for mname, hx, rough in MATERIALS:
        lin = tuple(srgb_to_linear(c) for c in hex_to_rgb(hx)) + (1.0,)
        mat = bpy.data.materials.new(mname)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = lin
            bsdf.inputs["Roughness"].default_value = rough
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
    V, F, M = build_mesh()
    if _in_blender():
        build_in_blender(V, F, M)
    else:
        print(check_mesh(V, F))
