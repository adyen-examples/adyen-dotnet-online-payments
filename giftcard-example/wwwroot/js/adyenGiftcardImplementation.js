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
    const checkoutSessionResponse = await callServer("/api/sessions");
    const checkout = await createAdyenCheckout(checkoutSessionResponse);
    
    //giftcardComponent.isAvailable()
    //.then(() => {
    //    giftcardComponent.mount("#giftcard-container");
    //})
    //.catch(e => {
    //    //gift cards is not available
    //    log.error(e);
    //});
    
    const giftcardComponent = checkout.create("giftcard").mount("#giftcard-container");
    document.getElementById("checkbalance-button")
      .addEventListener('click', async () => 
      {
        const balanceCheckResponse = await callServer("/api/balancecheck");
        console.info(balanceCheckResponse);
      });

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

const giftcardConfiguration = {
  onOrderCreated: async function (orderStatus) {
    // Get the remaining amount to be paid from orderStatus.
    console.info(orderStatus);
    // Use your existing instance of AdyenCheckout to create payment methods components
    // The shopper can use these payment methods to pay the remaining amount
    //const idealComponent = await checkout.create("ideal").mount("#ideal-container");
    //const cardComponent = await checkout.create("card").mount("#card-container");
  },
  onRequiringConfirmation: () => {
    document.getElementById("pay-button")
    .addEventListener('click', () => {
        console.log("click");
        window.giftcardComponent.submit();
    });
  },
};

async function createAdyenCheckout(session){
  return new AdyenCheckout(
  {
    clientKey: clientKey,
    locale: "en_US",
    environment: "test",
    session: session,
    showPayButton: true,
    paymentMethodsConfiguration: {        
      card: {
        hasHolderName: true,
        holderNameRequired: true,
        name: "Credit or debit card",
        amount: {
          value: 11000,
          currency: "EUR",
        },
      },
      giftcardConfiguration: {
        giftcardConfiguration
      },
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