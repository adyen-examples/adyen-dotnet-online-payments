const clientKey = document.getElementById("clientKey").innerHTML;

// Used to finalize a checkout call in case of redirect
const urlParams = new URLSearchParams(window.location.search);
const sessionId = urlParams.get('sessionId'); // Unique identifier for the payment session
const redirectResult = urlParams.get('redirectResult');

let remainingAmountToPay = 11000;

// Typical checkout experience
async function startCheckout() {
  // Used in the demo to know which type of checkout was chosen
  const type = document.getElementById("type").innerHTML;

  try {
    const sessionResponse = await callServer("/api/sessions/giftcardcomponent");
    console.info(sessionResponse);
    const checkout = await createAdyenCheckout(sessionResponse);

    // Adds giftcard container when clicked
    const addGiftcardButton = document.getElementById("add-giftcard-button")
      .addEventListener('click', async () => 
      {
        giftcardComponent = checkout.create("giftcard").mount("#giftcard-container");
        document.getElementById("add-giftcard-button").remove();
      });
    
    // Create your components
    const cardComponent = checkout.create("card").mount("#card-container");
    const idealComponent = await checkout.create("ideal").mount("#ideal-container");
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

async function createAdyenCheckout(session){
  return new AdyenCheckout(
  {
    clientKey: clientKey,
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
        amount: {
          value: remainingAmountToPay,
          currency: "EUR",
        },
      },
    },
    onOrderCreated: (orderStatus) => {
      console.info(orderStatus);
      const spanElement = document.getElementById('remaining-due-amount');
      remainingAmountToPay = orderStatus.remainingAmount.value / 100;
      spanElement.textContent = remainingAmountToPay.toFixed(2);
    },
    onRequiringConfirmation: () => {
      console.info("Confirming the final payment... You're one click away.");
      //document.getElementById('pay-button')
      //  .addEventListener('click', () => this.submit());
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

if (!sessionId) { startCheckout() } else { finalizeCheckout(); }