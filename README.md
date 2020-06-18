# Acumatica Webhook Handler example

This is an example Acumatica customization project that contains a webhook handler implementation to listen for Acumatica push notifications for updates in non-stock items and creates/updates/deletes them in the local instance.

## Installation ##

1. If you just want to deploy and try the demo, you can download the customization file [here](https://github.com/markoan/acumatica-webhook-example/raw/master/Customization/WebhookDemo.zip) and import it into the instance you wish to process the notifications (destination instance).

2. Once published, copy the webhook URL from the webhooks page (screen SM304000). This is your webhook endpoint.

3. In another Acumatica instance, create a new Generic Inquiry (screen SM208000)
4. Add the InventoryItem DAC, add the condition "InventoryItem.ItemType Equals Non-Stock Item" and add the following fields to the result grid and save it:
 - InventoryCD
 - Descr
 - ItemType
 - ItemClassID
 - PostClassID
 - TaxCategoryID
 - NonStockShip
 - NonStockReceipt
 - BaseUnit
 - PurchaseUnit
 - SalesUnit

5. Configure a new push notification (screen SM302000) with a Webhook destination and paste the URL from your webhook in the Address field.
6. Set the Header name to "CustomAuthorization" and the Header Value to "secretValue". This is a placeholder in the demo for more secure Authentication headers.
7. In the Generic Inquiries grid add the GI you just created and save it

Now you are ready to test. Anytime you add, update or delete a non-stock item in this instance it will send a notification to your webhook.

You can review sent notifications in the Process Push Notifications screen (SM502000).

## Working with the code ##

To work on this project, just follow these steps:

1. For the easiest deployment, create a folder called "WebhookDemo" inside your instance "Projects" folder. It should look like this: [your instance folder]\App_Data\Projects\WebhookDemo
2. Clone this repository into the WebhookDemo folder you just created
2. Import the customization project from the zip file located in ./Customization/ folder into your local dev instance
3. Open the customization project and use the "Bind to existing" command to point it to the repository root directory: [your instance folder]\App_Data\Projects\WebhookDemo
4. Open the solution in Visual Studio

## Prerequisites | Supported Versions & Builds ##

***Acumatica 2020R1 (Build 20.100.0095 onward)***


## Support ##

If you have any questions or need assistance, you can post your questions in the [Stackoverflow forum](https://stackoverflow.com/questions/tagged/acumatica).

## More Information
This code was used to demo a webhook integration in the Acumatica DevCon 2020 presentation.

Learn more about how Acumatica in [Acumatica's Site](https://www.acumatica.com/)
