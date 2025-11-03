# Adyen [Gift Cards](https://docs.adyen.com/payment-methods/gift-cards) Integration Demo

This repository includes an example of gift card payments with Adyen using the Partial Order API.
Within this demo app, you'll find a simplified version of an e-commerce website. The shopper can choose to use gift cards to complete their purchase or use their preferred payment method to pay the remaining amount.

![Card gift card demo](wwwroot/images/cardgiftcard.gif)

This demo leverages Adyen's API Library for .NET ([GitHub](https://github.com/Adyen/adyen-dotnet-api-library) | [Docs](https://docs.adyen.com/development-resources/libraries?tab=c__5#csharp)).


## Run integration on localhost using a proxy
You will need .NET Core SDK 6.x. to run this application locally.

1. Clone this repository

```
git clone https://github.com/adyen-examples/adyen-dotnet-online-payments.git
```


2. Open your [Adyen Test Account](https://ca-test.adyen.com/ca/ca/overview/default.shtml) and create a set of [API keys](https://docs.adyen.com/user-management/how-to-get-the-api-key). 
    - [`ADYEN_API_KEY`](https://docs.adyen.com/user-management/how-to-get-the-api-key)
    - [`ADYEN_CLIENT_KEY`](https://docs.adyen.com/user-management/client-side-authentication)
    - [`ADYEN_MERCHANT_ACCOUNT`](https://docs.adyen.com/account/account-structure)
    

3. To allow the Adyen Drop-In and Components to load, add `https://localhost:5001` as allowed origin by going to your `ADYEN_MERCHANT_ACCOUNT` in the Customer Area: `Developers` → `API credentials` → Find your `ws_user` → `Client settings` → `Add Allowed origins`.
> **Warning** You should only allow wild card (*) domains in the **test** environment. In a **live** environment, you should specify the exact URL of the application.

This demo provides a simple webhook integration at `/api/webhooks/notifications`. For it to work, you need to provide a way for Adyen's servers to reach your running application and add a standard webhook in the Customer Area.
To expose this endpoint locally, we have highlighted two options in step 4 or 5. Choose one or consider alternative tunneling software.

4. Expose your localhost with Visual Studio using dev tunnels.
     - Add `https://*.devtunnels.ms` to your allowed origins
     - Create your public (temporary/persistent) dev tunnel by following [this guide](https://learn.microsoft.com/en-us/aspnet/core/test/dev-tunnels?view=aspnetcore-7.0)

If you use Visual Studio 17.4 or higher, the webhook URL will be the generated URL (i.e. `https://xd1r2txt-5001.euw.devtunnels.ms`).


5. Expose your localhost with tunneling software (i.e. ngrok).
    - Add `https://*.ngrok.io` to your allowed origins

If you use a tunneling service like ngrok, the webhook URL will be the generated URL (i.e. `https://c991-80-113-16-28.ngrok.io/api/webhooks/notifications/`).

```bash
  $ ngrok http 8080
  
  Session Status                online                                                                                           
  Account                       ############                                                                      
  Version                       #########                                                                                          
  Region                        United States (us)                                                                                 
  Forwarding                    http://c991-80-113-16-28.ngrok.io -> http://localhost:8080                                       
  Forwarding                    https://c991-80-113-16-28.ngrok.io -> http://localhost:8080           
```


6. To receive notifications asynchronously, add a webhook:
    - In the Customer Area go to `Developers` → `Webhooks` and add a new `Standard notification webhook`
    - Define username and password (Basic Authentication) to [protect your endpoint](https://docs.adyen.com/development-resources/webhooks/best-practices#security) - Basic authentication only guarantees that the notification was sent by Adyen, not that it wasn't modified during transmission
    - Generate the [HMAC Key](https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures) - This key is used to [verify](https://docs.adyen.com/development-resources/webhooks/best-practices#security) whether the HMAC signature that is included in the notification, was sent by Adyen and not modified during transmission
    - See script below that allows you to easily set your environmental variables
    - For the URL, enter `https://ngrok.io` for now - We will need to update this webhook URL in step 10
    - Make sure the webhook is **Enabled** to send notifications


7. Set the following environment variables in your terminal environment: `ADYEN_API_KEY`, `ADYEN_CLIENT_KEY`, `ADYEN_MERCHANT_ACCOUNT` and `ADYEN_HMAC_KEY`. Note that some IDEs will have to be restarted for environmental variables to be injected properly.

```shell
export ADYEN_API_KEY=yourAdyenApiKey
export ADYEN_MERCHANT_ACCOUNT=yourAdyenMerchantAccount
export ADYEN_CLIENT_KEY=yourAdyenClientKey
export ADYEN_HMAC_KEY=yourAdyenHmacKey
```

On Windows CMD you can use this command instead.

```shell
set ADYEN_API_KEY=yourAdyenApiKey
set ADYEN_MERCHANT_ACCOUNT=yourAdyenMerchantAccount
set ADYEN_CLIENT_KEY=yourAdyenClientKey
set ADYEN_HMAC_KEY=yourAdyenHmacKey
```


8. In the Customer Area, go to `Developers` → `Additional Settings` → Under `Acquirer` enable `Payment Account Reference` to receive the Payment Account Reference.


9. Start the application and visit localhost.


```shell
dotnet run --project giftcard-example 
```

10. Update your webhook in your Customer Area with the public url that is generated.
  - In the Customer Area go to `Developers` → `Webhooks` → Select your `Webhook` that is created in step 6 → `Server Configuration`
  - Update the URL of your application/endpoint (e.g. `https://c991-80-113-16-28.ngrok.io/api/webhooks/notifications/` or `https://xd1r2txt-5001.euw.devtunnels.ms`)
  - Hit `Apply` → `Save changes`

> **Note** When exiting ngrok or Visual Studio a new URL is generated, make sure to **update the Webhook URL** in the Customer Area as described in the final step. 
> You can find more information about webhooks in [this detailed blog post](https://www.adyen.com/blog/Integrating-webhooks-notifications-with-Adyen-Checkout).


## Supported Integrations

Before testing, please make sure to [add the gift card payment method(s) to your Adyen Account](https://docs.adyen.com/payment-methods#add-payment-methods-to-your-account).


## Usage
To try out this application with test card numbers, visit [Gift card numbers](https://docs.adyen.com/development-resources/testing/test-card-numbers#gift-cards) and [Test card numbers](https://docs.adyen.com/development-resources/test-cards/test-card-numbers). 
We recommend saving some test cards in your browser so you can test your integration faster in the future.

1. Visit the main page, pick the Drop-in or Gift Card component integration, follow the instructions by entering the gift card number, followed by finalizing the payment.

2. Visit the Customer Area `Developers` → `API logs` to view your logs. 
