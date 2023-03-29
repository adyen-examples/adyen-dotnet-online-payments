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
    const checkout = await createAdyenCheckout(checkoutSessionResponse, type);
    checkout.create(type).mount("#payment");
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

async function createAdyenCheckout(session, type){
  const giftcardConfiguration = {
    onBalanceCheck: async function (resolve, reject, data) {
        // Make a POST /paymentMethods/balance request
        const balanceCheckResponse = await callServer("/api/balancecheck");
        console.info(balanceCheckResponse);
        //resolve(BalanceResponse);
    },
    onOrderRequest: async function (resolve, reject, data) {
        const createOrderResponse = await callServer("/api/createorder");
        console.info(createOrderResponse);
        //resolve(createOrderResponse);
    },
    onOrderCancel: async function(Order) {
        // Make a POST /orders/cancel request
        // Call the update function and pass the payment methods response to update the instance of checkout
        const cancelOrderResponse = await callServer("/api/cancelorder");
        checkout.update(cancelOrderResponse, cancelOrderResponse.amount);
    },
  };
  return new AdyenCheckout(
  {
    clientKey,
    locale: "en_US",
    environment: "test",
    session: session,
    showPayButton: true,
    paymentMethodsConfiguration: {
      giftcardConfiguration: {
        giftcardConfiguration
      },
      ideal: {
        showImage: true,
      },
      card: {
        hasHolderName: true,
        holderNameRequired: true,
        name: "Credit or debit card",
        amount: {
          value: 11000,
          currency: "EUR",
        },
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