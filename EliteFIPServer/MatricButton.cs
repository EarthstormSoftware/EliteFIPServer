using Matric.Integration;
using System.Collections.Generic;

namespace EliteFIPServer {

    class MatricButton {

        public string ButtonName { get; }
        public string ButtonLabel { get; set; }
        public bool IsButton { get; }
        public bool IsIndicator { get; }
        public bool IsWarning { get; }
        public bool IsSwitch { get; }
        public bool IsSlider { get; }
        public bool IsText { get; }
        public bool IsPanel { get; }
        public string OffText { get; set; }
        public string OnText { get; set; }
        public bool ButtonState { get; set; }
        public int SwitchPosition { get; set; }
        public int SliderPosition { get; set; }
        public bool UpdateButtonText { get; set; }

        public bool GameState { get; set; }


        public MatricButton(string buttonName, string buttonLabel, bool isButton = true, bool isIndicator = true, bool isWarning = true, bool isSwitch = true, bool isSlider = false, bool isText = false, bool isPanel = false,
            string offText = "", string onText = "", bool buttonState = false, int switchPosition = 1, int sliderPosition = 0, bool updateButtonText = false, bool gameState = false) {
            ButtonName = buttonName;
            ButtonLabel = buttonLabel;
            IsButton = isButton;
            IsIndicator = isIndicator;
            IsWarning = isWarning;
            IsSwitch = isSwitch;
            IsSlider = isSlider;
            IsText = isText;
            IsPanel = isPanel;
            OffText = offText;
            OnText = onText;
            ButtonState = buttonState;
            SwitchPosition = switchPosition;
            SliderPosition = sliderPosition;
            UpdateButtonText = updateButtonText;
            GameState = gameState;
        }

        public void SetDefaultButtonState() {
            ButtonState = false;
            GameState = false;
            SwitchPosition = 1;
            SliderPosition = 0;
            if (IsText) {
                OffText = "";
            }
        }

        public void UpdateMatricState(Matric.Integration.Matric matric, string ClientId) {
            List<SetButtonsVisualStateArgs> buttons = new List<SetButtonsVisualStateArgs>();
            List<SetControlsStateArgs> controls = new List<SetControlsStateArgs>();

            this.AddToVisualList(buttons);
            if (buttons.Count > 0) {
                matric.SetButtonsVisualState(ClientId, buttons);
            }
            this.AddToControlList(controls);
            if (controls.Count > 0) {
                matric.SetControlsState(ClientId, controls);
            }
            if (IsButton && UpdateButtonText) {
                matric.SetButtonProperties(ClientId, buttonName: MatricConstants.BTN + ButtonName, text: GameState == true ? OnText : OffText);
            }

            if (IsText) {
                matric.SetButtonProperties(ClientId, buttonName: MatricConstants.TXT + ButtonName, text: OffText);
            }

        }

        public List<SetButtonsVisualStateArgs> AddToVisualList(List<SetButtonsVisualStateArgs> buttons) {
            if (IsButton) { buttons.Add(new SetButtonsVisualStateArgs(null, GameState ? "on" : "off", MatricConstants.BTN + ButtonName)); }
            if (IsIndicator) { buttons.Add(new SetButtonsVisualStateArgs(null, GameState ? "on" : "off", MatricConstants.IND + ButtonName)); }
            if (IsWarning) { buttons.Add(new SetButtonsVisualStateArgs(null, GameState ? "on" : "off", MatricConstants.WRN + ButtonName)); }
            if (IsSwitch) { buttons.Add(new SetButtonsVisualStateArgs(null, GameState ? "on" : "off", MatricConstants.SWT + ButtonName)); }
            return buttons;
        }

        public List<SetControlsStateArgs> AddToControlList(List<SetControlsStateArgs> controls) {
            if (IsSwitch) {
                SetControlsStateArgs controlState = new SetControlsStateArgs();
                controlState.ControlName = MatricConstants.SWT + ButtonName;
                controlState.State = new { position = SwitchPosition };
                controls.Add(controlState);
            }
            if (IsSlider) {
                SetControlsStateArgs controlState = new SetControlsStateArgs();
                controlState.ControlName = MatricConstants.SLD + ButtonName;
                controlState.State = new { value = SliderPosition };
                controls.Add(controlState);
            }

            return controls;
        }

        public void SetMatricStateToOff(Matric.Integration.Matric matric, string ClientId) {
            this.SetDefaultButtonState();
            this.UpdateMatricState(matric, ClientId);
        }
    }
}
