const clientKey = document.getElementById("clientKey").innerHTML;

// Used to finalize a checkout call in case of redirect
const urlParams = new URLSearchParams(window.location.search);
const sessionId = urlParams.get('sessionId'); // Unique identifier for the payment session
const redirectResult = urlParams.get('redirectResult');

var remainingAmountToPay = 11000;

// Start checkout experience
async function startCheckout() {
    console.info('Start checkout...');
    try {
        // Call /paymentMethods endpoint to retrieve payment methods
        const paymentMethodsResponse = await callServer("/api/paymentMethods");
        console.info(paymentMethodsResponse);

        // Call /sessions endpoint to start session
        const sessionResponse = await callServer("/api/sessions/giftcardcomponent");
        console.info(sessionResponse);

        const checkout = await createAdyenCheckout(sessionResponse, paymentMethodsResponse);
        // Create and mount giftcard component
        createGiftcardComponent(checkout);

        // Create and mount other payment method components (e.g. 'ideal', 'scheme' etc)
        createPaymentMethodsComponent(checkout, paymentMethodsResponse);

    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}

// Create and mount giftcard component
function createGiftcardComponent(checkout) {
    // Adds giftcard container and the eventlistener
    document.getElementById("add-giftcard-button")
        .addEventListener('click', async () => {
            // Create the giftcard component
            giftcardComponent = checkout.create("giftcard").mount("#giftcard-container");

            // Show giftcard component
            document.getElementById("giftcard-container").hidden = false;
            // Hide add-gift-card button
            document.getElementById("add-giftcard-button").hidden = true;
        });

}

// Create and mount payment methods that we have retrieved from the paymentMethodsResponse
// We're using components here, hence why we create and mount the individual <div> elements for every payment method that we want to support
// Read more about components in the documentation: https://docs.adyen.com/online-payments/web-components
function createPaymentMethodsComponent(checkout, paymentMethodsResponse) {
    // You'd want to instantiate the components individually, here's an example of how to instantiate the components from the paymentMethodResponse:
    
    // 1. Filter giftcard payment methods, so we only show the payment methods that are not gift cards
    // const paymentMethods = paymentMethodsResponse.paymentMethods.filter((paymentMethod) => paymentMethod.type != 'giftcard')

    // 2. Loop over the payment method types, create & mount by using checkout.create(paymentMethodType).mount(...)
    //for (var i = 0; i < paymentMethods.length; i++) {
    //    const paymentMethodType = paymentMethods[i].type;
    //    try {
    //        const containerName =  paymentMethodType + '-container';
    //        checkout.create(paymentMethodType).mount('#' + containerName);
    //    } catch (error) {
    //        console.error('Could not find type: "' + paymentMethodType + '". Please add your payment component by adding <div id="{paymentMethodType}-container"></div> in Checkout.cshtml.');
    //        console.error(error);
    //    }
    //}

    // For this demo, we use an example of 'scheme' (card or debit card).
    checkout.create('scheme').mount('#scheme-container');
}

// Appends a visual cue when a giftcard has been successfully added
// Pass parameter which states how much of the giftcard amount is spent
function appendGiftcardInformation(giftcardSubtractedBalance) {
    var overviewList = document.querySelector('.order-overview-list');

    // Add <li>
    var liElement = document.createElement('li');
    liElement.classList.add('order-overview-list-item');

    // Add <p>
    var pElement = document.createElement('p');
    pElement.classList.add('order-overview-list-item-giftcard-balance');

    // Show 'Giftcard applied -50.00' (example)
    pElement.textContent = 'Giftcard applied -' + (giftcardSubtractedBalance / 100).toFixed(2);

    // Append the child element to the list
    liElement.appendChild(pElement);
    overviewList.appendChild(liElement);
}

// Some payment methods use redirects, this is where we finalize the operation
async function finalizeCheckout() {
    console.info('Finalize checkout...');
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

async function createAdyenCheckout(session, paymentMethodsResponse) {
    return new AdyenCheckout({
        paymentMethodsResponse: paymentMethodsResponse,
        clientKey: clientKey,
        locale: "en_US",
        environment: "test",
        session: session,
        showPayButton: true,
        /*onSubmit: () => {
           // Adyen provides a "Pay button", to use the Pay button for each payment method, set `showPayButton` to true
           // The 'Pay button'' triggers this onSubmit() event
           // If you want to use your own button and then trigger the submit flow on your own
           // Set `showPayButton` to false and call the .submit() method from your own button implementation, for example: component.submit()
        },*/
        paymentMethodsConfiguration: {
            ideal: {
                showImage: true,
            },
            card: {
                hasHolderName: true,
                holderNameRequired: true,
                name: "Credit or debit card",
            },
        },
        onOrderCreated: (orderStatus) => {
            console.info('Created an order')
            console.info(orderStatus);
            // Calculate how much balance is spent of the giftcard
            var subtractedGiftcardBalance = remainingAmountToPay - orderStatus.remainingAmount.value;

            // Show/set what the shopper still has to pay and show it in two decimals
            remainingAmountToPay = orderStatus.remainingAmount.value;
            const spanElement = document.getElementById('remaining-due-amount');
            spanElement.textContent = (remainingAmountToPay / 100).toFixed(2);

            // Hide giftcard component
            document.getElementById("giftcard-container").hidden = true;
            // Show add-gift-card button
            document.getElementById("add-giftcard-button").hidden = false;

            // Show the subtracted balance of the giftcard to thes shopper in two decimals
            appendGiftcardInformation(subtractedGiftcardBalance);
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

function handleServerResponse(res, _component) {
    console.info(res);
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