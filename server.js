// //Dependencies
// const five = require('johnny-five');
// const notifier = require('node-notifier');
// const robot = require('robotjs');
// const path = require('path');
// const board = new five.Board();
// //config
// const timeOut = 5000;
// //Code
// board.on('ready', () => {
//     const led = new five.Led(13);
//     const button = new five.Button(8);
//     let activated = false;
//     let lastActivated = 0;
//     function toggle(){
//         led.toggle();
//         robot.keyTap("audio_pause");
//         activated = !activated;
//     }
//     button.on("down", function() {
//         if (activated) {
//             toggle();
//         } else {
//             if (lastActivated + timeOut < (Date.now())) {
//                 toggle();
//                 lastActivated = Date.now();
//                 notifier.notify({
//                     title : "Someone wants you.",
//                     message : "Your music has been paused.",
//                     icon : path.join(__dirname, 'iconSmall.png'),
//                     sound : false,
//                     wait : false
//                 }, err => {
//                     if (err) {
//                         throw new Error(err);
//                     };
//                 });
//             }; 
//         };
//     });
// });


const notifier = require('node-notifier');
notifier.notify(
    {
      appID: "com.myapp.id",
      appName: "com.myapp.id",
      title: "Hello",
      message: "Hello world!",
      wait: true
    },
    function(err, response) {
      // Response is response from notification
      console.log({
          err : err,
          response : response
      })
      console.log("responded...");
    }
); 
setTimeout(function () {
    console.log('timeout completed');
}, 99999); 