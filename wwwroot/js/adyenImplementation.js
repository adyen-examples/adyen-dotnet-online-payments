const clientKey = document.getElementById("clientKey").innerHTML;
const type = document.getElementById("type").innerHTML;

async function initCheckout() {
  try {
    const paymentMethodsResponse = await callServer("/api/getPaymentMethods", {
      merchantAccount: "",
    });
    const configuration = {
      paymentMethodsResponse: filterUnimplemented(paymentMethodsResponse),
      clientKey,
      locale: "en_US",
      environment: "test",
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
            value: 1000,
            currency: "EUR",
          },
        },
      },
      onSubmit: (state, component) => {
        if (state.isValid) {
          handleSubmission(state, component, "/api/initiatePayment");
        }
      },
      onAdditionalDetails: (state, component) => {
        handleSubmission(state, component, "/api/submitAdditionalDetails");
      },
    };

    const checkout = new AdyenCheckout(configuration);
    checkout.create(type).mount(document.getElementById(type));
  } catch (error) {
    console.error(error);
    alert("Error occurred. Look at console for details");
  }
}

function filterUnimplemented(pm) {
  pm.paymentMethods = pm.paymentMethods.filter((it) =>
    [
      "ach",
      "scheme",
      "dotpay",
      "giropay",
      "ideal",
      "directEbanking",
      "klarna_paynow",
      "klarna",
      "klarna_account",
    ].includes(it.type)
  );
  return pm;
}

// Event handlers called when the shopper selects the pay button,
// or when additional information is required to complete the payment
async function handleSubmission(state, component, url) {
  try {
    const res = await callServer(url, state.data);
    handleServerResponse(res, component);
  } catch (error) {
    console.error(error);
    alert("Error occurred. Look at console for details");
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

// Handles responses sent from your server to the client
function handleServerResponse(res, component) {
  if (res.action) {
    component.handleAction(res.action);
  } else {
    switch (res.resultCode) {
      case "Authorised":
        window.location.href = "/Home/Result/success";
        break;
      case "Pending":
      case "Received":
        window.location.href = "/Home/Result/pending";
        break;
      case "Refused":
        window.location.href = "/Home/Result/failed";
        break;
      default:
        window.location.href = `/Home/Result/error?reason=${res.resultCode}`;
        break;
    }
  }
}

initCheckout();
