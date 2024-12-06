using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using GrumpyFoxGames;

public class LightgunDebug : MonoBehaviour
{
    [SerializeField] private float cursorSpeed = 0.75f;
    [SerializeField] private TextMeshProUGUI debug;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform cursorRect;
    [SerializeField] private LightgunManager lightgunManager;
    [SerializeField] private string shootCommand = "F1.2.1F2.2.1";
    [SerializeField] private string reloadCommand = "F4.2.1";

    private bool _shooting;
    private bool _reloading;
    private Vector2 _cursorTarget;
    private string _lastControl;
    private InputAction _cursorAction;
    private InputAction _shootAction;
    private InputAction _reloadAction;
    private PlayerInput _playerInput;

    private void Awake()
    {
        Application.targetFrameRate = -1;
        Application.runInBackground = true;
        Cursor.visible = false;

        _cursorAction = InputSystem.actions.FindAction("Cursor");
        _shootAction = InputSystem.actions.FindAction("Shoot");
        _reloadAction = InputSystem.actions.FindAction("Reload");
        _playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        debug.text = $"Port:\t {(lightgunManager.IsConnected ? lightgunManager.ConnectedPort : string.Empty)}\n" +
                     $"Connected:\t {lightgunManager.IsConnected}\n" +
                     // $"Control:\t {_lastControl}\n" +
                     $"Cursor:\t {_cursorTarget}\n" +
                     $"Shoot:\t {_shooting}\n" +
                     $"Reload:\t {_reloading}";
    }

    private void LateUpdate()
    {
        cursorRect.position = Vector2.Lerp(cursorRect.position, _cursorTarget, cursorSpeed);
    }

    public void OnMoveCursor(InputAction.CallbackContext context)
    {
        var cursorValue = context.ReadValue<Vector2>();

        switch (context.control.device)
        {
            case Gamepad gamepad:
                if (!context.canceled)
                {
                    _lastControl = _playerInput.currentControlScheme;
                    cursorValue = (cursorValue + Vector2.one) * canvasRect.rect.size * 0.5f;
                }
                else
                {
                    cursorValue = canvasRect.rect.size * 0.5f;
                }

                _cursorTarget = cursorValue;
                break;
            case Mouse mouse:
            case Touchscreen touchscreen:
                if (!context.canceled)
                {
                    _lastControl = _playerInput.currentControlScheme;
                    _cursorTarget = cursorValue;
                }

                break;
        }
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        _shooting = !context.canceled;
        if (!context.performed) return;

        _lastControl = _playerInput.currentControlScheme;
        lightgunManager.SendCommand(shootCommand);
        UnityEngine.Debug.Log("Shoot!");
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        _reloading = !context.canceled;
        if (!context.performed) return;

        _lastControl = _playerInput.currentControlScheme;
        lightgunManager.SendCommand(reloadCommand);
        UnityEngine.Debug.Log("Reload!");
    }
}
