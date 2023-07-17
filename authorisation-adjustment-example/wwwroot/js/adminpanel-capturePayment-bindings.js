// Sends POST request to url
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

// Captures payment of the given pspReference
async function sendCapturePaymentRequest(pspReference) {
    try {
        const res = await sendPostRequest("/admin/capture-payment", { pspReference: pspReference});
        console.log(res);
        switch (res.status) {
            case "received":
                window.location.href = "admin/result/received/" + pspReference;
                break;
            default:
                window.location.href = "admin/result/error" + pspReference;
                break;
        };
    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}

// Binds submit buttons to `capture-payment`-endpoint
function bindCapturePaymentFormButtons() { 
    var elements = document.getElementsByName('capturePaymentForm');
    for (var i = 0; i < elements.length;  i++) {
        elements[i].addEventListener('submit', async function(event) {
            event.preventDefault();

            var formData = new FormData(event.target);
            var pspReference = formData.get('pspReference');

            await sendCapturePaymentRequest(pspReference);
        });
    }
}

bindCapturePaymentFormButtons();