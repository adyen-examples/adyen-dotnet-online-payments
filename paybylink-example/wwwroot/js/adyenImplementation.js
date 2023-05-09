// Creates new payment link
async function createPaymentLink() {
    try {
        const amount = document.getElementById('amount').value;
        const reference = document.getElementById('reference').value;
        const linksResponse = await sendPostRequest("/api/links", { Amount: amount, Reference: reference});
        console.info(linksResponse);

    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}

// Sends post request to url
async function sendPostRequest(url, data) {
    const res = await fetch(url, {
        method: "POST",
        body: data ? JSON.stringify(data) : "",
        headers: {
            "Content-Type": "application/json",
        },
    });

    return await res.json();
}

// Sends get request to url
//async function sendGetRequest(url) {
//    const res = await fetch(url, {
//        method: "GET",
//        headers: {
//            "Content-Type": "application/json",
//        },
//    });

//    return await res.json();
//}

// TODO
// Handle server response 
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