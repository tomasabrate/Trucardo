using UnityEngine;
public class TestRot : MonoBehaviour {
    void Start() {
        Quaternion r1 = Quaternion.Euler(0, 90, 0) * Quaternion.Euler(-35, 0, 0);
        Vector3 up1 = r1 * Vector3.up;
        Vector3 forward1 = r1 * Vector3.forward;
        Debug.Log($"P1 (y=90): Up={up1}, Forward={forward1}");

        Quaternion r2 = Quaternion.Euler(0, -90, 0) * Quaternion.Euler(-35, 0, 0);
        Vector3 up2 = r2 * Vector3.up;
        Vector3 forward2 = r2 * Vector3.forward;
        Debug.Log($"P2 (y=-90): Up={up2}, Forward={forward2}");
    }
}
