const clientKey = document.getElementById("clientKey").innerHTML;

startSubscriptionPayment();

async function startSubscriptionPayment() {
    try {
        const response = await callServer("/tokenization/subscription");
        console.log(response);
        alert("See console for the response." + response); // WIP show it on a page.
        //handleServerResponse(checkoutSessionResponse);
       
    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}

// Calls your server endpoints
async function callServer(url, data) {
    const res = await fetch(url, {
        method: "POST",
        body: data ? JSON.stringify(data) : "",
        headers: {
            "Content-Type": "application/json",
        },
    });

    return await res.json();
}

function handleServerResponse(res, _component) {
    switch (res.resultCode) {
        case "Authorised":
            window.location.href = "/result/success";
            break;
        case "Pending":
        case "Received":
            window.location.href = "/result/pending";
            break;
        case "Refused":
            window.location.href = "/result/failed";
            break;
        default:
            window.location.href = "/result/error";
            break;
    }
}