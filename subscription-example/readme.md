# Adyen [online payment](https://docs.adyen.com/checkout) integration demos

## Details

This repository includes an subscription example. Within this demo app, you'll find a simplified version of a website that offers a subscription service. You'll learn about tokenization and making recurring payments using this token.

## Requirements

- .NET Core SDK 6.x
- A set of [API keys](https://docs.adyen.com/user-management/how-to-get-the-api-key).


## Run this integration in seconds using [Gitpod](https://gitpod.io/)

* Open your [Adyen Test Account](https://ca-test.adyen.com/ca/ca/overview/default.shtml) and create a set of [API keys](https://docs.adyen.com/user-management/how-to-get-the-api-key).
* Go to [gitpod account variables](https://gitpod.io/variables).
* Set the `ADYEN_API_KEY`, `ADYEN_CLIENT_KEY`, `ADYEN_HMAC_KEY` and `ADYEN_MERCHANT_ACCOUNT variables`.
* Click the button below!

[![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/adyen-examples/adyen-dotnet-online-payments/)

* To exit the application, go to the Terminal and press `Ctrl + C`.
* To run the application, use the command: `dotnet run --project subscription-example`.

_NOTE: To allow the Adyen Drop-In and Components to load, you have to add `https://*.gitpod.io` as allowed origin for your chosen set of [API Credentials](https://ca-test.adyen.com/ca/ca/config/api_credentials_new.shtml)_


## Installation

1. Clone this repo:

```
git clone https://github.com/adyen-examples/adyen-dotnet-online-payments.git
```

2. Set the below environment variables in your terminal environment ([API key](https://docs.adyen.com/user-management/how-to-get-the-api-key), [Client Key](https://docs.adyen.com/user-management/client-side-authentication), [Merchant Account](https://docs.adyen.com/account/account-structure), [HMAC Key](https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures) - Remember to add `https://localhost:5001` as an origin for client key, and merchant account name, all credentials are in string format)

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

## Usage

1. Start the application by navigating to the respective example:

```
dotnet run
```

2. Visit [https://localhost:5001/](https://localhost:5001/) to initiate a subscription.

To try out subscriptions with test card numbers and payment method details, see [Test card numbers](https://docs.adyen.com/development-resources/test-cards/test-card-numbers).

3. Make sure your webhook is reachable (see instructions [below](#testing-webhooks))!

**[!]** Make sure to **enable** `Recurring Details` (under the `Additional Data` → `Payments` section) in your ca-environment. If this is not enabled, you will **not** receive the `recurringDetailReference` from the webhook!

4. To test the application, enter one or multiple card details. Once the payment is authorized, you will receive a webhook notification with the recurringDetailReference!

5. These references are shown under 'Manage Tokens' which you can use to make a payment request (or choose to disable them).

_Note: We currently store these values in a local memory cache, if you restart/stop the application these values are lost. However, the tokens will still persist on the Adyen Platform. You can view the stored payment details by going to a recent payment of the shopper: `Transactions` → `Payments` → `Shopper Details` → `Recurring: View stored payment details` in the ca-environment._


## Testing webhooks

Webhooks deliver asynchronous notifications and it is important to test them during the setup of your integration. You can find more information about webhooks in [this detailed blog post](https://www.adyen.com/blog/Integrating-webhooks-notifications-with-Adyen-Checkout).

This sample application provides a simple webhook integration exposed at `/api/webhooks/notifications`. For it to work, you need to:

1. Provide a way for the Adyen platform to reach your running application
2. Add a Standard webhook in your Customer Area

### Making your server reachable

Your endpoint that will consume the incoming webhook must be publicly accessible.

There are typically 3 options:
* deploy on your own cloud provider
* deploy on Gitpod
* expose your localhost with tunneling software (i.e. ngrok)

#### Option 1: cloud deployment
If you deploy on your cloud provider (or your own public server) the webhook URL will be the URL of the server 
```
  https://{cloud-provider}/api/webhooks/notifications
```

#### Option 2: Gitpod
If you use Gitpod the webhook URL will be the host assigned by Gitpod
```
  https://myorg-myrepo-y8ad7pso0w5.ws-eu75.gitpod.io/api/webhooks/notifications
```
**Note:** when starting a new Gitpod workspace the host changes, make sure to **update the Webhook URL** in the Customer Area

#### Option 3: localhost via tunneling software
If you use a tunneling service like [ngrok](ngrok) the webhook URL will be the generated URL (ie `https://c991-80-113-16-28.ngrok.io`)

```bash
  $ ngrok http 8080
  
  Session Status                online                                                                                           
  Account                       ############                                                                      
  Version                       #########                                                                                          
  Region                        United States (us)                                                                                 
  Forwarding                    http://c991-80-113-16-28.ngrok.io -> http://localhost:8080                                       
  Forwarding                    https://c991-80-113-16-28.ngrok.io -> http://localhost:8080           
```

Do not forget to add the host url (`https://*.ngrok.io`) to the [allowed origins](https://ca-test.adyen.com/ca/ca/config/api_credentials_new.shtml) in the Customer Area.

**Note:** when restarting ngrok a new URL is generated, make sure to **update the Webhook URL** in the Customer Area.

If you use [dev tunnels](https://devblogs.microsoft.com/visualstudio/public-preview-of-dev-tunnels-in-visual-studio-for-asp-net-core-projects/) in Visual Studio 17.4 or higher, the webhook URL will be the generated URL (ie `https://xd1r2txt-5001.euw.devtunnels.ms`)

When you're logged-in Visual Studio, set the launchSetting profile to `adyen_dotnet_checkout_example_port_tunneling` and the tunnel should be automatically generated. 

Do not forget to add the host url (`https://*.devtunnels.ms`) to the [allowed origins](https://ca-test.adyen.com/ca/ca/config/api_credentials_new.shtml) in the Customer Area.

**Note:** when closing or restarting visual studio a new URL will be generated, make sure to **update the Webhook URL** in the Customer Area.

### Set up a webhook

* In the Customer Area go to Developers → Webhooks and create a new 'Standard notification' webhook.
* Enter the URL of your application/endpoint (see options [above](#making-your-server-reachable))
* Define username and password for Basic Authentication
* Generate the HMAC Key
* In Additional Settings, add the data you want to receive. A good example is 'Payment Account Reference' and the mandatory `Recurring Details` for subscriptions.
* Make sure the webhook is **Enabled** (therefore it can receive the notifications)

That's it! Every time you perform a new payment, your application will receive a notification from the Adyen platform.
