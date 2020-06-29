var ghPages = require("gh-pages");

var packageUrl = "https://github.com/Shmew/Fable.SignalR.git";

console.log("Publishing to ", packageUrl);

ghPages.publish("docs", {
    repo: packageUrl,
    dotfiles: true
}, function (e) {
    if (e === undefined) {
        console.log("Finished publishing succesfully");
    } else {
        console.log("Error occured while publishing :(", e);
    }
});