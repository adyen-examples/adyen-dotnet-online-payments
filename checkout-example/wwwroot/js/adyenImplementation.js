const clientKey = document.getElementById("clientKey").innerHTML;
const { AdyenCheckout, Dropin } = window.AdyenWeb;

// Used to finalize a checkout call in case of redirect
const urlParams = new URLSearchParams(window.location.search);
const sessionId = urlParams.get('sessionId'); // Unique identifier for the payment session
const redirectResult = urlParams.get('redirectResult');

// Typical checkout experience
async function startCheckout() {
    // Used in the demo to know which type of checkout was chosen
    const type = document.getElementById("type").innerHTML;

    try {
        const checkoutSessionResponse = await sendPostRequest("/api/sessions");
        
        //console.log(JSON.stringify(checkoutSessionResponse, null, 2));
        
        const configuration = {
            id: sessionId,
            session: checkoutSessionResponse,
            clientKey: clientKey,
            locale: "en_US",
            environment: "test",
            countryCode: 'NL',
            showPayButton: true,
            onPaymentCompleted: (result, component) => {
                console.info("onPaymentCompleted");
                console.info(result, component);
                handleServerResponse(result, component);
            },
            onError: (error, component) => {
                console.error("onError");
                console.error(error.name, error.message, error.stack, component);
                handleServerResponse(error, component);
            },
        };
        
        const adyenCheckoutInstance = await AdyenCheckout(configuration);
        
        const paymentMethodsConfiguration = {
            card: {
                showBrandIcon: true,
                hasHolderName: true,
                holderNameRequired: true,
                amount: {
                    value: 10000,
                    currency: 'EUR',
                },
                placeholders: {
                    cardNumber: "1234 5678 9012 3456",
                    expiryDate: "MM/YY",
                    securityCodeThreeDigits: "123",
                    securityCodeFourDigits: "1234",
                    holderName: "J. Smith",
                },
            },
        };
        
        dropinInstance = new Dropin(adyenCheckoutInstance, { 
            paymentMethodsConfiguration: paymentMethodsConfiguration,
        }).mount(document.getElementById("payment"));

    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}

// Some payment methods use redirects. This is where we finalize the operation
async function finalizeCheckout() {
    try {
        const configuration = {
            id: sessionId,
            locale: "en_US",
            environment: "test",
            countryCode: 'NL',
            showPayButton: true,
            onPaymentCompleted: (result, component) => {
                console.info("onPaymentCompleted");
                console.info(result, component);
                handleServerResponse(result, component);
            },
            onError: (error, component) => {
                console.error("onError");
                console.error(error.name, error.message, error.stack, component);
                handleServerResponse(error, component);
            }
        };

        const adyenCheckoutInstance = await AdyenCheckout(configuration);

        adyenCheckoutInstance.submitDetails({
            details: {
                redirectResult
            }
        });
    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}


// Sends POST request
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

if (!sessionId) {
    startCheckout()
} else {
    finalizeCheckout();
}