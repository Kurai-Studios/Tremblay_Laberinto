using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class PlayerControls : IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }

    public PlayerControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""PlayerControls"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""a1b2c3d4-0001-0001-0001-000000000001"",
            ""actions"": [
                { ""name"": ""Move"", ""type"": ""Value"", ""id"": ""a1b2c3d4-0002-0001-0001-000000000001"", ""expectedControlType"": ""Vector2"" },
                { ""name"": ""Look"", ""type"": ""Value"", ""id"": ""a1b2c3d4-0002-0002-0001-000000000001"", ""expectedControlType"": ""Vector2"" },
                { ""name"": ""Jump"", ""type"": ""Button"", ""id"": ""a1b2c3d4-0002-0003-0001-000000000001"" },
                { ""name"": ""Sprint"", ""type"": ""Button"", ""id"": ""a1b2c3d4-0002-0004-0001-000000000001"" },
                { ""name"": ""Crouch"", ""type"": ""Button"", ""id"": ""a1b2c3d4-0002-0005-0001-000000000001"" }
            ],
            ""bindings"": [
                { ""name"": ""WASD"", ""id"": ""b1-01"", ""path"": ""2DVector"", ""action"": ""Move"", ""isComposite"": true },
                { ""name"": ""up"",   ""id"": ""b1-02"", ""path"": ""<Keyboard>/w"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""name"": ""down"", ""id"": ""b1-03"", ""path"": ""<Keyboard>/s"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""name"": ""left"", ""id"": ""b1-04"", ""path"": ""<Keyboard>/a"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""name"": ""right"",""id"": ""b1-05"", ""path"": ""<Keyboard>/d"", ""action"": ""Move"", ""isPartOfComposite"": true },
                { ""name"": """", ""id"": ""b1-06"", ""path"": ""<Mouse>/delta"",       ""action"": ""Look"" },
                { ""name"": """", ""id"": ""b1-07"", ""path"": ""<Keyboard>/space"",     ""action"": ""Jump"" },
                { ""name"": """", ""id"": ""b1-08"", ""path"": ""<Keyboard>/leftShift"", ""action"": ""Sprint"" },
                { ""name"": """", ""id"": ""b1-09"", ""path"": ""<Keyboard>/leftCtrl"",  ""action"": ""Crouch"" }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Keyboard&Mouse"",
            ""bindingGroup"": ""Keyboard&Mouse"",
            ""devices"": [
                { ""devicePath"": ""<Keyboard>"", ""isOptional"": false },
                { ""devicePath"": ""<Mouse>"",    ""isOptional"": false }
            ]
        }
    ]
}");
        m_Player = new PlayerActions(this);
    }

    ~PlayerControls() { Dispose(); }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
        GC.SuppressFinalize(this);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public System.Collections.Generic.IEnumerator<InputAction> GetEnumerator() => asset.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Contains(InputAction action) => asset.Contains(action);

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false) =>
        asset.FindAction(actionNameOrId, throwIfNotFound);

    public int FindBinding(InputBinding bindingMask, out InputAction action) =>
        asset.FindBinding(bindingMask, out action);

    public void Enable() => asset.Enable();
    public void Disable() => asset.Disable();

    public System.Collections.Generic.IEnumerable<InputBinding> bindings => asset.bindings;

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    // --- Player Action Map ---

    private PlayerActions m_Player;
    public PlayerActions Player => m_Player;

    public struct PlayerActions
    {
        private PlayerControls m_Wrapper;

        internal PlayerActions(PlayerControls wrapper) { m_Wrapper = wrapper; }

        public InputAction Move   => m_Wrapper.asset.FindAction("Player/Move",   false);
        public InputAction Look   => m_Wrapper.asset.FindAction("Player/Look",   false);
        public InputAction Jump   => m_Wrapper.asset.FindAction("Player/Jump",   false);
        public InputAction Sprint => m_Wrapper.asset.FindAction("Player/Sprint", false);
        public InputAction Crouch => m_Wrapper.asset.FindAction("Player/Crouch", false);

        public InputActionMap Get() => m_Wrapper.asset.FindActionMap("Player", false);
        public void Enable()  => Get().Enable();
        public void Disable() => Get().Disable();

        public void SetCallbacks(IPlayerActions instance)
        {
            var map = Get();
            if (map == null) return;

            Move.started   += instance.OnMove;
            Move.performed += instance.OnMove;
            Move.canceled  += instance.OnMove;

            Look.started   += instance.OnLook;
            Look.performed += instance.OnLook;
            Look.canceled  += instance.OnLook;

            Jump.started   += instance.OnJump;
            Jump.performed += instance.OnJump;
            Jump.canceled  += instance.OnJump;

            Sprint.started   += instance.OnSprint;
            Sprint.performed += instance.OnSprint;
            Sprint.canceled  += instance.OnSprint;

            Crouch.started   += instance.OnCrouch;
            Crouch.performed += instance.OnCrouch;
            Crouch.canceled  += instance.OnCrouch;
        }

        public void RemoveCallbacks(IPlayerActions instance)
        {
            var map = Get();
            if (map == null) return;

            Move.started   -= instance.OnMove;
            Move.performed -= instance.OnMove;
            Move.canceled  -= instance.OnMove;

            Look.started   -= instance.OnLook;
            Look.performed -= instance.OnLook;
            Look.canceled  -= instance.OnLook;

            Jump.started   -= instance.OnJump;
            Jump.performed -= instance.OnJump;
            Jump.canceled  -= instance.OnJump;

            Sprint.started   -= instance.OnSprint;
            Sprint.performed -= instance.OnSprint;
            Sprint.canceled  -= instance.OnSprint;

            Crouch.started   -= instance.OnCrouch;
            Crouch.performed -= instance.OnCrouch;
            Crouch.canceled  -= instance.OnCrouch;
        }
    }

    public interface IPlayerActions
    {
        void OnMove(InputAction.CallbackContext context);
        void OnLook(InputAction.CallbackContext context);
        void OnJump(InputAction.CallbackContext context);
        void OnSprint(InputAction.CallbackContext context);
        void OnCrouch(InputAction.CallbackContext context);
    }
}
