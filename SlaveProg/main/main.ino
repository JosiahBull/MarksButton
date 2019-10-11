//Global Variables
const int buttonTimeout = 5000;
bool debug = false;
String inputStr;
//Declare Classes
class Button {
  int _pin;
  int _mode; //Default mode is 0 (button toggles _pressed on release), mode 1 will only have _pressed true while button is held.
  int _timePressed;
  bool _pressed;
  bool _output;
  bool _enabled;
  public:
    Button(int pin) : _pin(pin), _pressed(false), _enabled(true), _mode(1), _output(false), _timePressed(-buttonTimeout) {
      pinMode(_pin, INPUT);
    }
    void disable() {
      _enabled = false;
      if (debug) Serial.println("The button on pin: " + String(_pin) + " has been disabled.");
    }
    void enable() {
      _enabled = true;
      if (debug) Serial.println("The button on pin: " + String(_pin) + " has been enabled.");
    }
    void mode(int mode) {
      _mode = mode;
    }
    getMode() {
      if (debug) Serial.println("The button on pin: " + String(_pin) + " has it's mode set to: " + String(_mode));
      return _mode;
    }
    getState() {
      if (debug) Serial.println("The button on pin: " + String(_pin) + " has the output state of: " + _output + " the button pressed state is: " + _pressed);
      return _output;
    }
    checkChange() {
      if (digitalRead(_pin) != _pressed && _enabled && ((millis() - _timePressed) > buttonTimeout || _output || _mode == 1)) {
        _timePressed = millis();
        if (_mode == 0) {
          ;
        }
        if (_mode == 1) {
          _pressed = !_pressed;
          _output = _pressed;
        }
        return true;
      } else {
        return false;
      }
    }
};
class Led {
  int _pin;
  bool _isOn;
  public:
    Led(int pin) : _pin(pin), _isOn(false) {
      pinMode(_pin, OUTPUT);
    }
    void on() {
      digitalWrite(_pin, HIGH);
      _isOn = true;
      if (debug) Serial.println("The led on pin: " + String(_pin) + " has been turned on.");
    }
    void off() {
      digitalWrite(_pin, LOW);
      _isOn = false;
      if (debug) Serial.println("The led on pin: " + String(_pin) + " has been turned off.");
    }
    void toggle() {
      if (_isOn) {
        off();
      } else {
        on();
      }
    }
    getState() {
      return _isOn;
      if (debug) Serial.println("The state of the led on pin: " + String(_pin) + " is: " + _isOn);
    }
};
void(* resetFunc) (void) = 0;
//Instantiate Things
Led displayLed(8);
Button button(11);
//Setup
void setup() {
  Serial.begin(9600);
  delay(2000);
  Serial.println("Type something!");
}
//Loopy loop
void loop() {
  if (Serial.available() > 0) {
    inputStr = Serial.readString();
    if (inputStr == "debug") {
      debug = !debug;
      Serial.println("Debug toggled.");
    }
    if (debug) Serial.println("I received: " + inputStr);
    if (inputStr == "reset") resetFunc();
    if (inputStr == "info") Serial.print("{\"ver\" : 0.1, \"time\" :" + String(millis()) + "}");
    if (inputStr == "led") displayLed.toggle();
    if (inputStr == "ledoff") displayLed.off();
    if (inputStr == "ledon") displayLed.on();
    if (inputStr == "ledState") displayLed.getState();
    if (inputStr == "disableButton") button.disable();
    if (inputStr == "enableButton") button.enable();
    if (inputStr == "buttonState") button.getState();
    if (inputStr == "buttonMode") button.getMode();
    inputStr = "";
  }
  if (button.checkChange()){
    displayLed.toggle();
  }
}
