//Global Variables
const unsigned long buttonTimeout = 5000;
bool debug = false;
String inputStr;
//Declare Classes
class Button {
  int _pin;
  int _mode; //Default mode is 0 (button toggles _pressed on release), mode 1 will only have _pressed true while button is held.
  unsigned long _timePressed;
  bool _pressed;
  bool _output;
  bool _enabled;
  public:
    Button(int pin) : _pin(pin), _pressed(false), _enabled(true), _mode(0), _output(false), _timePressed(-buttonTimeout) {
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
      if (digitalRead(_pin) != _pressed && _enabled && ((millis() - _timePressed) > buttonTimeout || _output || _mode == 1) && (millis() - _timePressed) > 100) {
        if (_mode == 0) {
          if (digitalRead(_pin) && !_pressed) {
            _timePressed = millis();
            _output = !_output;
            _pressed = !_pressed;
            return true;
          } else {
            _pressed = !_pressed;
            return false;
          }
          
        }
        if (_mode == 1) {
          _timePressed = millis();
          _pressed = !_pressed;
          _output = !_output;
          return true;
        }
        Serial.println("Mode is incorrectly configured.");
      }
      return false;
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
      analogWrite(_pin, 40);
      _isOn = true;
      if (debug) Serial.println("The led on pin: " + String(_pin) + " has been turned on.");
    }
    void off() {
      analogWrite(_pin, 0);
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
Led displayLed(9);
Button button(7);
//Setup
void setup() {
  Serial.begin(9600);
  delay(2000);
//  Serial.println("Type something!");
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
    if (inputStr == "ping!") Serial.println("pong!");
    if (inputStr == "reset") resetFunc();
    if (inputStr == "info") Serial.println("{\"ver\" : 1.0, \"time\" :" + String(millis()) + "}");
    if (inputStr == "led") displayLed.toggle();
    if (inputStr == "ledoff") displayLed.off();
    if (inputStr == "ledon") displayLed.on();
    if (inputStr == "ledState") displayLed.getState();
    if (inputStr == "disableButton") button.disable();
    if (inputStr == "enableButton") button.enable();
    if (inputStr == "getButtonState") button.getState();
    if (inputStr == "getButtonMode") button.getMode();
    inputStr = "";
  }
  if (button.checkChange()){
    displayLed.toggle();
    if (displayLed.getState() == 1) {
      Serial.println("press");
    } else {
      Serial.println("release");
    }
  }
}
