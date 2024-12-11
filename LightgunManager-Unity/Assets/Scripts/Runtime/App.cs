using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using GrumpyFoxGames;

public class App : MonoBehaviour
{
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private float cursorSpeed = 0.75f;
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform cursorRect;
    [SerializeField] private InputActionReference moveCursorAction;
    [SerializeField] private InputActionReference shootAction;
    [SerializeField] private InputActionReference reloadAction;

    private bool _shooting;
    private bool _reloading;
    private Vector2 _cursorTarget;
    private string _lastControl;
    private InputAction _cursorAction;
    private InputAction _shootAction;
    private InputAction _reloadAction;

    private void Awake()
    {
        Application.targetFrameRate = -1;
        Application.runInBackground = true;
        // Cursor.visible = false;
        _cursorAction = InputSystem.actions.FindAction("Cursor");
        _shootAction = InputSystem.actions.FindAction("Shoot");
        _reloadAction = InputSystem.actions.FindAction("Reload");
        _playerInput.onActionTriggered += OnActionTriggered;
        LightgunManager.Start();
    }

    private void OnDestroy()
    {
        _playerInput.onActionTriggered -= OnActionTriggered;
        LightgunManager.Stop();
    }

    private void Update()
    {
        debugText.text = $"Port:\t\t {(LightgunManager.IsConnected ? LightgunManager.ConnectedPort : string.Empty)}\n" +
                     $"Connected:\t {LightgunManager.IsConnected}\n" +
                     $"VID:\t {LightgunManager.vid}\n" +
                     $"PID:\t {LightgunManager.pid}\n" +
                     $"Control:\t {_lastControl}\n" +
                     $"Cursor:\t {_cursorTarget}\n" +
                     $"Shoot:\t {_shooting}\n" +
                     $"Reload:\t {_reloading}";
    }

    private void LateUpdate()
    {
        cursorRect.position = Vector2.Lerp(cursorRect.position, _cursorTarget, cursorSpeed);
    }

    private void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (context.action.name == moveCursorAction.name)
        {
            OnMoveCursor(context);
        }
        else if (context.action.name == shootAction.name)
        {
            OnShoot(context);
        }
        else if (context.action.name == reloadAction.name)
        {
            OnReload(context);
        }
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
        
        Debug.Log("Shoot!");
        LightgunManager.SendCommand_Shoot();
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        _reloading = !context.canceled;
        if (!context.performed) return;

        _lastControl = _playerInput.currentControlScheme;
        
        Debug.Log("Reload!");
        LightgunManager.SendCommand_Reload();
    }
}
