using System;
using UnityEngine;


[RequireComponent (typeof(Rigidbody))]
public class Glider : MonoBehaviour
{
    [SerializeField] private Transform _wingCP;

    [Header("Плотность воздуха")]
    [SerializeField] private float _airDensity = 1.225f;

    [Header("Аэродиномические характеристики крыла")]
    [SerializeField] private float _wingArea = 1.5f;
    [SerializeField] private float _wingAspect = 8.0f;

    [SerializeField] private float _wingCDD = 0.02f;

    [SerializeField] private float _wingClaplha = 5.5f;

    private Rigidbody _rigidbody;


    private Vector3 _vPoint;
    private Vector3 _worldVelocity;
    private float _speadMS;
    private float _alphaRad;

    private float _cl, _cd, _qDyn, _lMag, _dMag, _qlidek;
    private bool IsGround;
    private float _startPosition;
    
    private JetEngine _jetEngine;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        
        if (_jetEngine == null)
        {
            _jetEngine = GetComponent<JetEngine>();
        }
    }


    private void FixedUpdate()
    {
        // скорость в точке крыла

        if (transform.position.y - 0.5f > _startPosition && IsGround)
        {
            //_wingCP.localEulerAngles = new Vector3(0, 180, 0);
        }
        
        _vPoint = _rigidbody.GetPointVelocity(_wingCP.position);
        _speadMS = _vPoint.magnitude;

        Vector3 flowDir = (-_vPoint).normalized;
        Vector3 xChord = _wingCP.forward;
        Vector3 zUP = _wingCP.up;
        Vector3 ySpan = _wingCP.right;


        float flowX = Vector3.Dot(lhs:flowDir, rhs:xChord);
        float flowZ = Vector3.Dot(lhs:flowDir, rhs:zUP);
        _alphaRad = Mathf.Atan2(y: flowZ, flowX);

        _cl = _wingClaplha * _alphaRad;
        _cd = _wingCDD + _cl * _cl / (Mathf.PI*_wingAspect * 0.85f);


        _qDyn = 0.5f * _airDensity * _speadMS * _speadMS;
        _lMag = _qDyn * _wingArea * _cl;
        _dMag = _qDyn * _wingArea * _cd;


        Vector3 Ddir = -flowDir;


        Vector3 liftDir = Vector3.Cross(lhs: flowDir, rhs:ySpan);
        liftDir.Normalize();
        

        Vector3 L = _lMag * liftDir;
        Vector3 D = _dMag * Ddir;


        _rigidbody.AddForceAtPosition(L + D, _wingCP.position, ForceMode.Force);

        // _worldVelocity = _rigidbody.linearVelocity;
        //_speadMS = _worldVelocity.magnitude;

    }

    private void StepOne()
    {
        Vector3 xChord = _wingCP.forward;//вдоль хорды
        Vector3 zUP = _wingCP.up;// нормаль к поверхности

        Vector3 flowDir = _speadMS > 0 ? _worldVelocity.normalized : _wingCP.forward;


        float flowX = Vector3.Dot(lhs: flowDir, rhs: xChord);
        float flowZ = Vector3.Dot(lhs: flowDir, rhs: zUP);

        _alphaRad = Mathf.Atan2(y: flowZ, flowX);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            _startPosition = transform.position.y;
            IsGround = true;
        }
    }

    private void OnGUI()
    {
        // Основная панель
        GUIStyle panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = Texture2D.blackTexture;
        panelStyle.padding = new RectOffset(12, 12, 12, 12);

        // Стиль заголовков
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        };

        // Стиль обычного текста
        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = Color.white }
        };

        GUI.Box(new Rect(10, 10, 360, 520), "", panelStyle);
        GUILayout.BeginArea(new Rect(20, 20, 340, 500));

        // ===== БЛОК 1: ДИНАМИКА ПОЛЁТА =====
        GUILayout.Label("ПОЛЁТНЫЕ ХАРАКТЕРИСТИКИ", titleStyle);
        GUILayout.Space(6);

        GUILayout.Label($"Текущая скорость: {_speadMS:F1} м/с", textStyle);
        GUILayout.Label($"Скорость (км/ч): {(int)(_speadMS * 3.6f)}", textStyle);
        GUILayout.Label($"Положение по высоте: {transform.position.y:F1} м", textStyle);
        GUILayout.Label($"Вертикальная составляющая: {_rigidbody.linearVelocity.y:F1} м/с", textStyle);

        GUILayout.Space(15);

        // ===== БЛОК 2: АЭРОФИЗИКА =====
        GUILayout.Label("АЭРОФИЗИКА КРЫЛА", titleStyle);
        GUILayout.Space(6);

        GUILayout.Label($"Angle of Attack: {_alphaRad * Mathf.Rad2Deg:F1}°", textStyle);
        GUILayout.Label($"Lift Coeff (Cl): {_cl:F2}", textStyle);
        GUILayout.Label($"Drag Coeff (Cd): {_cd:F3}", textStyle);
        GUILayout.Label($"L/D Ratio: {_qlidek:F1}", textStyle);
        GUILayout.Space(4);
        GUILayout.Label($"Lift Force: {(int)_lMag} Н", textStyle);
        GUILayout.Label($"Drag Force: {(int)_dMag} Н", textStyle);
        GUILayout.Label($"Dynamic Pressure: {(int)_qDyn} Па", textStyle);

        GUILayout.Space(15);

        // ===== БЛОК 3: ДВИЖОК =====
        if (_jetEngine != null)
        {
            GUILayout.Label("РАБОТА ДВИГАТЕЛЯ", titleStyle);
            GUILayout.Space(6);

            GUILayout.Label($"Положение газа: {_jetEngine._throttle01:P0}", textStyle);

            string afterburnerState = _jetEngine._afterBurner ? "<color=orange>ACTIVE</color>" : "<color=grey>OFF</color>";
            GUIStyle abStyle = new GUIStyle(textStyle) { richText = true };

            GUILayout.Label($"Afterburner: {afterburnerState}", abStyle);
            GUILayout.Label($"Thrust Output: {(int)_jetEngine._lastAppliedThrust} Н", textStyle);
        }

        GUILayout.EndArea();
    }
}