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
        // Call /sessions endpoint to start session
        const sessionResponse = await callServer("/api/sessions/giftcardcomponent");
        console.info(sessionResponse);

        const checkout = await createAdyenCheckout(sessionResponse);

        // Create and mount gift card component
        createGiftcardComponent(checkout);

        // Create and mount your supported payment method components (e.g. 'ideal', 'scheme' etc)
        createPaymentMethodButton(checkout, 'ideal');
        createPaymentMethodButton(checkout, 'scheme');

        // Hide all payment method buttons, once a gift card is added, we show it for this demo
        hideAllPaymentMethodButtons();
    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}

// Create and mount giftcard component
function createGiftcardComponent(checkout) {
    // Adds gift card container and the eventlistener
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

// Creates the buttons for your specified paymentMethodType
// We then mount the respective component when the shopper clicks on it.
// Parameters: `paymentMethodType` (type of the payment method)
function createPaymentMethodButton(checkout, paymentMethodType) {
    let paymentMethodList = document.querySelector('.payment-method-list');

    // Add <li> root container for the `paymentMethod`
    let liElement = document.createElement('li');
    liElement.classList.add(paymentMethodType + '-container');
    
    // Add <p> sub-element for `paymentMethod`
    let pPaymentMethodElement = document.createElement('p');
    pPaymentMethodElement.classList.add(paymentMethodType + '-container-item');
    pPaymentMethodElement.setAttribute('hidden', true);

    // Add <button> that allows a shopper to select its `paymentMethod`
    let buttonElement = document.createElement('button');
    buttonElement.classList.add(paymentMethodType + '-button-selector');
    buttonElement.classList.add('payment-method-selector-button');

    // Add <button> text
    buttonElement.textContent = paymentMethodType;

    // Add event listener to <button>
    buttonElement.addEventListener('click', async () => {
        const className = '.' + paymentMethodType + '-container-item';
        // Create the paymentMethodType component
        try {
            checkout.create(paymentMethodType).mount(className);
            hideAllPaymentMethodButtons();
            pPaymentMethodElement.hidden = false;
        } catch (error) {
            console.warn('Unable to mount: "' + paymentMethodType + '" to the `<div class={paymentMethodType}-container-item></div>`.');
        }
    });
    
    // Append child elements to HTML
    liElement.appendChild(buttonElement);
    liElement.appendChild(pPaymentMethodElement);
    paymentMethodList.appendChild(liElement);
}

// Show all payment method buttons
function showAllPaymentMethodButtons() {
    const buttons = document.getElementsByClassName('payment-method-selector-button');
    for (let i = 0; i < buttons.length; i++) {
        buttons[i].hidden = false;
    }
}

// Hides all payment method buttons
function hideAllPaymentMethodButtons() {
    const buttons = document.getElementsByClassName('payment-method-selector-button');
    for (let i = 0; i < buttons.length; i++) {
        buttons[i].hidden = true;
    }
}

// Appends a visual cue when a gift card has been successfully applied
// Pass parameter which states how much of the gift card amount is spent
function showGiftcardAppliedMessage(giftcardSubtractedBalance) {
    let overviewList = document.querySelector('.order-overview-list');

    // Add <li>
    let liElement = document.createElement('li');
    liElement.classList.add('order-overview-list-item');

    // Add <p>
    let pElement = document.createElement('p');
    pElement.classList.add('order-overview-list-item-giftcard-balance');

    // Show 'Gift card applied -50.00' (example)
    pElement.textContent = 'Gift card applied -' + (giftcardSubtractedBalance / 100).toFixed(2);
    
    // Append the child element to the list
    liElement.appendChild(pElement);
    overviewList.appendChild(liElement);
}

// Shows an error message when gift card is invalid
function showGiftCardErrorMessage(errorMessage) {
    let giftcardErrorMessageComponent = document.querySelector('#giftcard-error-message');
    // Show the error message
    giftcardErrorMessageComponent.textContent = errorMessage;
}

// Clears any error messages
function clearGiftCardErrorMessages() {
    let giftcardErrorMessageComponent = document.querySelector('#giftcard-error-message');
    giftcardErrorMessageComponent.textContent = '';
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

async function createAdyenCheckout(session) {
    return new AdyenCheckout({
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
            // You can specify a custom logo for a gift card brand when creating a configuration object
            // See https://docs.adyen.com/payment-methods/gift-cards/web-drop-in#optional-customize-logos
        },
        onOrderCreated: (orderStatus) => {
            // Called when a partial order is created and the shopper has to select another payment method to finalize the payment
            // See https://docs.adyen.com/payment-methods/gift-cards/web-component#required-configuration
            console.info('onOrderCreated')
            console.info(orderStatus);
            handleOnOrderCreated(orderStatus);
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

// Called when onOrderCreated is fired.
function handleOnOrderCreated(orderStatus) {
    // Calculate how much balance is spent of the gift card
    let subtractedGiftcardBalance = remainingAmountToPay - orderStatus.remainingAmount.value;

    // Calculate and set what the shopper still has to pay and show it in two decimals
    remainingAmountToPay = orderStatus.remainingAmount.value;
    const remainingAmountElement = document.getElementById('remaining-due-amount');
    remainingAmountElement.textContent = (remainingAmountToPay / 100).toFixed(2);

    // Hide gift card component
    document.getElementById("giftcard-container").hidden = true;
    // Show add-gift-card button
    document.getElementById("add-giftcard-button").hidden = false;

    // Show the subtracted balance of the gift card to the shopper if there are any changes
    if (subtractedGiftcardBalance > 0) {
        clearGiftCardErrorMessages();
        showGiftcardAppliedMessage(subtractedGiftcardBalance);
        showAllPaymentMethodButtons();
    } else { 
        showGiftCardErrorMessage('Invalid gift card');
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
    console.info(res);
    switch (res?.resultCode) {
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