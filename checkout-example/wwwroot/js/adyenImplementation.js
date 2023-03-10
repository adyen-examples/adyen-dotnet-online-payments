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
    checkout.create(type).mount(document.getElementById("payment"));
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
  const giftcardConfiguration = {
  onOrderCreated: function (orderStatus) {
    // Get the remaining amount to be paid from orderStatus.
    console.log(orderStatus.remainingAmount);
    // Use your existing instance of AdyenCheckout to create payment methods components
    // The shopper can use these payment methods to pay the remaining amount
    const idealComponent = checkout.create('ideal').mount('#ideal-container');
    const cardComponent = checkout.create('card').mount('#card-container');
    // Mount the gift card component to the specified DOM element
    const giftcardComponent = checkout.create('giftcard').mount('#giftcard-container');
    // Add other payment method components that you want to show to the shopper
    }
  };
  return new AdyenCheckout(
  {
    clientKey,
    locale: "en_US",
    environment: "test",
    session: session,
    showPayButton: true,
    paymentMethodsConfiguration: {
      giftcard: {
        amount: {
          value: 10000,
          currency: "EUR",
          giftcardConfiguration: giftcardConfiguration,
          brandsConfiguration: {
            plastix: {
              icon: 'https://mymerchant.com/icons/customIcon.svg'
            }
          }
        },
      },
      ideal: {
        showImage: true,
      },
      card: {
        hasHolderName: true,
        holderNameRequired: true,
        name: "Credit or debit card",
        amount: {
          value: 10000,
          currency: "EUR",
        },
      },
      paypal: {
        amount: {
          value: 10000,
          currency: "USD",
        },
        environment: "test", // Change this to "live" when you're ready to accept live PayPal payments
        countryCode: "US", // Only needed for test. This will be automatically retrieved when you are in production.
      }
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