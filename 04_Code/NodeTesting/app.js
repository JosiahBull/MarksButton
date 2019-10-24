//Dependencies
const five = require('johnny-five');
const robot = require('robotjs');
const board = new five.Board();
//config
const timeOut = 5000;
//Code
board.on('ready', () => {
    const led = new five.Led(9);
    const button = new five.Button(7);
    let activated = false;
    let lastActivated = 0;
    function toggle(){
        led.toggle();
        robot.keyTap("audio_pause");
        activated = !activated;
    }
    button.on("down", function() {
        if (activated) {
            toggle();
        } else {
            if (lastActivated + timeOut < (Date.now())) {
                toggle();
                lastActivated = Date.now();
            }; 
        };
    });
})