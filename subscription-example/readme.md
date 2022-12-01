# Adyen [Tokenization](https://docs.adyen.com/online-payments-tokenization) Integration Demo

## Details

This repository includes an subscription example. Within this demo app, you'll find a simplified version of a website that offers a music subscription service. The shopper can purchase a subscription and administrators can manage the saved (tokenized) payment methods on a separate admin panel. The panel allows admins to make payments on behalf of the shopper using this token (we refer to this token as `recurringDetailReference` in this application). Furthermore, we will show you how to setup webhooks to receive notifications asynchronously. 


## Requirements

- .NET Core SDK 6.x
- A set of [API keys](https://docs.adyen.com/user-management/how-to-get-the-api-key): `ADYEN_API_KEY`, `ADYEN_CLIENT_KEY`, `ADYEN_HMAC_KEY` and `ADYEN_MERCHANT_ACCOUNT`.

## Run integration on [Gitpod](https://gitpod.io/)
1. Open your [Adyen Test Account](https://ca-test.adyen.com/ca/ca/overview/default.shtml) and create a set of [API keys](https://docs.adyen.com/user-management/how-to-get-the-api-key).
2. Go to [gitpod account variables](https://gitpod.io/variables).
3. Set the `ADYEN_API_KEY`, `ADYEN_CLIENT_KEY`, `ADYEN_HMAC_KEY` and `ADYEN_MERCHANT_ACCOUNT variables`.
4. Click the button below!

[![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/adyen-examples/adyen-dotnet-online-payments/tree/main/subscription-example)

5. To allow the Adyen Drop-In and Components to load, add `https://*.gitpod.io` as allowed origin by going to your MerchantAccount in the ca-environment: `Developers` → `API credentials` → Find your `ws_user` → `Client settings` → `Add Allowed origins`.


6. To receive notifications asynchronously, add a webhook: 
* In the Customer Area go to Developers → Webhooks and create a new 'Standard notification' webhook.
* Enter the URL of your application/endpoint (e.g. `https://myorg-myrepo-y8ad7pso0w5.ws-eu75.gitpod.io/api/webhooks/notifications/`).
* Define username and password (Basic Authentication) to protect your endpoint.
* Generate the HMAC Key.
* In Additional Settings, add the data you want to receive. `Recurring Details` for subscriptions.
* Make sure the webhook is **Enabled** (therefore it can receive the notifications).


## Run integration on localhost (+ proxy)

1. Clone this repo:

```
git clone https://github.com/adyen-examples/adyen-dotnet-online-payments.git
```

2. Set the below environment variables in your terminal environment ([API key](https://docs.adyen.com/user-management/how-to-get-the-api-key), [Client Key](https://docs.adyen.com/user-management/client-side-authentication), [Merchant Account](https://docs.adyen.com/account/account-structure), [HMAC Key](https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures)

```shell
export ADYEN_API_KEY=yourAdyenApiKey
export ADYEN_MERCHANT_ACCOUNT=yourAdyenMerchantAccount
export ADYEN_CLIENT_KEY=yourAdyenClientKey
export ADYEN_HMAC_KEY=yourAdyenHmacKey
```

On Windows CMD you can use below commands instead

```shell
set ADYEN_API_KEY=yourAdyenApiKey
set ADYEN_MERCHANT_ACCOUNT=yourAdyenMerchantAccount
set ADYEN_CLIENT_KEY=yourAdyenClientKey
set ADYEN_HMAC_KEY=yourAdyenHmacKey
```

3. To allow the Adyen Drop-In and Components to load, add `https://*.localhost:5001` as allowed origin by going to your MerchantAccount in the ca-environment: `Developers` → `API credentials` → Find your `ws_user` → `Client settings` → `Add Allowed origins`.

Your endpoint that will consume the incoming webhook must be publicly accessible. We will setup the webhooks next by exposing your local endpoint, followed by configuring the webhook in your ca-environment. 

4. To expose your endpoint locally, we have highlighted two options here.  
* Expose your localhost with Visual Studio using [dev tunnels](https://devblogs.microsoft.com/visualstudio/public-preview-of-dev-tunnels-in-visual-studio-for-asp-net-core-projects/) - Add `https://*.devtunnels.ms` to your allowed origins.
If you use Visual Studio 17.4 or higher [dev tunnels](dev-tunnels), the webhook URL will be the generated URL (i.e. `https://xd1r2txt-5001.euw.devtunnels.ms`) when you start the application. We have created a launchSetting profile `adyen_dotnet_checkout_example_port_tunneling` that should do this for you automatically on startup.

* Expose your localhost with tunneling software (i.e. ngrok) - Add `https://*.ngrok.io` to your allowed origins.
If you use a tunneling service like [ngrok](ngrok), the webhook URL will be the generated URL (i.e. `https://c991-80-113-16-28.ngrok.io/api/webhooks/notifications/`).

```bash
  $ ngrok http 8080
  
  Session Status                online                                                                                           
  Account                       ############                                                                      
  Version                       #########                                                                                          
  Region                        United States (us)                                                                                 
  Forwarding                    http://c991-80-113-16-28.ngrok.io -> http://localhost:8080                                       
  Forwarding                    https://c991-80-113-16-28.ngrok.io -> http://localhost:8080           
```


5. To receive notifications asynchronously, add a webhook: 
* In the Customer Area go to Developers → Webhooks and create a new 'Standard notification' webhook.
* Enter the URL of your application/endpoint (e.g. `https://c991-80-113-16-28.ngrok.io/api/webhooks/notifications/` or `https://xd1r2txt-5001.euw.devtunnels.ms`).
* Define username and password (Basic Authentication) to protect your endpoint.
* Generate the HMAC Key.
* In Additional Settings, add the data you want to receive. `Recurring Details` for subscriptions.
* Make sure the webhook is **Enabled** (therefore it can receive the notifications).

**Note:** when restarting ngrok a new URL is generated, make sure to **update the Webhook URL** in the Customer Area, see: [5.-To-Receive-notifications-asynchronously]
You can find more information about webhooks in [this detailed blog post](https://www.adyen.com/blog/Integrating-webhooks-notifications-with-Adyen-Checkout)


## Usage

1. Start the application with the following command:

```
dotnet run --project subscription-example
```

2. Visit `https://localhost:5001/` to buy a subscription.

To try out subscriptions with test card numbers and payment method details, see [Test card numbers](https://docs.adyen.com/development-resources/test-cards/test-card-numbers).

3. Visit 'Shopper View' to test the application, enter one or multiple card details. Once the payment is authorized, you will receive a webhook notification with the recurringDetailReference! Enter multiple cards to receive multiple different recurringDetailReferences.

4. Visit 'Admin Panel' to find the saved recurringDetailReferences and choose to make a payment request or disable the recurringDetailReference.

_Note: We currently store these values in a local memory cache, if you restart/stop the application these values are lost. However, the tokens will still persist on the Adyen Platform. You can view the stored payment details by going to a recent payment of the shopper: `Transactions` → `Payments` → `Shopper Details` → `Recurring: View stored payment details` in the ca-environment._
