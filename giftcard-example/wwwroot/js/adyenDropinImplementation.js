const clientKey = document.getElementById("clientKey").innerHTML;

// Used to finalize a checkout call in case of redirect
const urlParams = new URLSearchParams(window.location.search);
const sessionId = urlParams.get('sessionId'); // Unique identifier for the payment session
const redirectResult = urlParams.get('redirectResult');

// Typical checkout experience
async function startCheckout() {
    // Used in the demo to know which type of checkout was chosen
    const type = document.getElementById("type").innerHTML;

    try {
        const checkoutSessionResponse = await callServer("/api/sessions/dropin");
        const checkout = await createAdyenCheckout(checkoutSessionResponse);
        checkout.create(type).mount("#payment");
    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}

// Some payment methods use redirects. This is where we finalize the operation
async function finalizeCheckout() {
    try {
        const checkout = await createAdyenCheckout({
            id: sessionId
        });
        checkout.submitDetails({
            details: {
                redirectResult
            }
        });
    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}

// Create Adyen Checkout configuration
async function createAdyenCheckout(session) {
    return new AdyenCheckout({
        clientKey,
        locale: "en_US",
        environment: "test",
        session: session,
        showPayButton: true,
        paymentMethodsConfiguration: {
            ideal: {
                showImage: true,
            },
            card: {
                hasHolderName: true,
                holderNameRequired: true,
                name: "Credit or debit card",
            },
            // You can specify a custom logo for a gift card brand when creating a configuration object
            // See https://docs.adyen.com/payment-methods/gift-cards/web-drop-in#optional-customize-logos
        },
        onOrderCreated: (orderStatus) => {
            // Called when a partial order is created and the shopper has to select another payment method to finalize the payment
            // See https://docs.adyen.com/payment-methods/gift-cards/web-component#required-configuration
            console.info('onOrderCreated');
            console.info(orderStatus);
        },
        onRequiringConfirmation: () => {
            // Called when the gift card balance is enough to pay the full payment amount
            // The shopper must then confirm that they want to make the payment with the gift card
            console.info("onRequiringConfirmation");
        },
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
    });
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
    startCheckout();
} else {
    finalizeCheckout();
}