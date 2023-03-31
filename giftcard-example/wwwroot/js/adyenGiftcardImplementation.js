const clientKey = document.getElementById("clientKey").innerHTML;

// Used to finalize a checkout call in case of redirect
const urlParams = new URLSearchParams(window.location.search);
const sessionId = urlParams.get('sessionId'); // Unique identifier for the payment session
const redirectResult = urlParams.get('redirectResult');

var totalAmountToPay = 11000;
var remainingAmountToPay = 11000;

// Typical checkout experience
async function startCheckout() {
  // Used in the demo to know which type of checkout was chosen
  const type = document.getElementById("type").innerHTML;

  try {
    const paymentMethodsResponse = await callServer("/api/paymentMethods");
    console.info(paymentMethodsResponse);

    const sessionResponse = await callServer("/api/sessions/giftcardcomponent");
    console.info(sessionResponse);

    const checkout = await createAdyenCheckout(sessionResponse, paymentMethodsResponse);

    // Adds giftcard container and the eventlistener.
    document.getElementById("add-giftcard-button")
      .addEventListener('click', async () =>
      {
        // Create the giftcard component
        giftcardComponent = checkout.create("giftcard", { 
          // Component configuration overrides all other onError configuration.
          onError: () => { console.log("error creating giftcard") } 
        }).mount("#giftcard-container");
      
        // Show giftcard component
        document.getElementById("giftcard-container").hidden = false;
        // Hide add-gift-card button
        document.getElementById("add-giftcard-button").hidden = true;
      });

    // Create your components
    const cardComponent = checkout.create("card", { 
      // Component configuration overrides all other onError configuration.
      onError: () => { console.log("error creating giftcard") }
    }).mount("#card-container");

    const idealComponent = checkout.create("ideal", { 
      // Component configuration overrides all other onError configuration.
      onError: () => { console.log("error creating giftcard") }
    }).mount("#ideal-container");

  } catch (error) {
    console.error(error);
    alert("Error occurred. Look at console for details");
  }
}

// Some payment methods use redirects. This is where we finalize the operation
async function finalizeCheckout() {
  try {
    const checkout = await createAdyenCheckout({id: sessionId});
    checkout.submitDetails({details: {redirectResult}});
  } catch (error) {
    console.error(error);
    alert("Error occurred. Look at console for details");
  }
}

async function createAdyenCheckout(session, paymentMethodsResponse){
  return new AdyenCheckout(
      {
        paymentMethodsResponse: paymentMethodsResponse,
        clientKey: clientKey,
        locale: "en_US",
        environment: "test",
        session: session,
        showPayButton: true,
        /*onSubmit: () => {
           // Adyen provides a "Pay button". To use the Pay button for each payment method, set `showPayButton` to true. 
           // The Pay button triggers the onSubmit event.
           // If you want to use your own button and then trigger the submit flow on your own, 
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
          appendGiftcardInfo(subtractedGiftcardBalance);
        },
        onRequiringConfirmation: () => {
          // Called when the gift card balance is enough to pay the full payment amount.
          // The shopper must then confirm that they want to make the payment with the gift card
          console.info("onRequiringConfirmation");
        },
        onPaymentCompleted: (result, component) => {
          console.info("onPaymentCompleted");
          console.info(result, component);
          handleServerResponse(result, component);
        },
        onChange: (state, component) =>{
            // state.isValid: True or false. Specifies if all the information that the shopper provided is valid.
            // state.data: Provides the data that you need to pass in the `/payments` call.
            //console.info(state);
            
            // Provides the active component instance that called this event.
            //console.info(component); 
        },
        onError: (error, component) => {
          console.error("onError");
          console.error(error.name, error.message, error.stack, component);
          handleServerResponse(error, component);
        },
      });
}

// Appends a visual cue when a giftcard has been successfully added
// Pass parameter which states how much of the giftcard amount is spent
function appendGiftcardInfo(giftcardSubtractedBalance) {
  // Add some <li> styling
  var overviewList = document.querySelector('.order-overview-list');
  var liElement = document.createElement('li');
  liElement.classList.add('order-overview-list-item');

  // Add some <p> styling
  var pElement = document.createElement('p');
  pElement.classList.add('order-overview-list-item-giftcard-balance');

  // Show 'Giftcard applied -50.00'
  pElement.textContent = 'Giftcard applied -' + (giftcardSubtractedBalance / 100).toFixed(2);

  // Append the child element to the list
  liElement.appendChild(pElement);
  overviewList.appendChild(liElement);
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

if (!sessionId) { startCheckout() } else { finalizeCheckout(); }