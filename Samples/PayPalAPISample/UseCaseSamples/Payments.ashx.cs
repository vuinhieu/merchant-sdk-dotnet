﻿using System;
using System.Collections.Generic;
using System.Web;
using System.Collections.Specialized;
using PayPal.PayPalAPIInterfaceService;
using PayPal.PayPalAPIInterfaceService.Model;
using System.Configuration;
using System.Web.SessionState;
using PayPal.Permissions.Model;

namespace PayPalAPISample.UseCaseSamples
{
    /// <summary>
    /// Summary description for Payments
    /// </summary>
    public class Payments : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/html";
            string strCall = context.Request.Params["ButtonPayments"];

            if (strCall.Equals("SetExpressCheckout"))
            {
                SetExpressCheckoutForRecurringPayments(context);
            }
            else if (strCall.Equals("CreateRecurringPaymentsProfile"))
            {
                RecurringPaymentsUsingCreditCard(context);
            }
            else if (strCall.Equals("SetExpressCheckoutPaymentAuthorization"))
            {
                SetExpressCheckoutPaymentAuthorization(context);
            }
            else if (strCall.Equals("SetExpressCheckoutPaymentOrder"))
            {
                SetExpressCheckoutPaymentOrder(context);
            }
            else if (strCall.Equals("ParallelPayment"))
            {
                ParallelPayment(context);
            }
            else if (strCall.Equals("DoExpressCheckout"))
            {
                DoExpressCheckout(context);
            }
            else if (strCall.Equals("DoAuthorization"))
            {
                DoAuthorization(context);
            }
            else if (strCall.Equals("DoCapture"))
            {
                DoCapture(context);
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Handles Set Express Checkout For Recurring Payments
        /// </summary>
        /// <param name="context"></param>
        private void SetExpressCheckoutForRecurringPayments(HttpContext context)
        {
            NameValueCollection parameters = context.Request.Params;

            // Configuration map containing signature credentials and other required configuration.
            // For a full list of configuration parameters refer at 
            // [https://github.com/paypal/merchant-sdk-dotnet/wiki/SDK-Configuration-Parameters]
            Dictionary<string, string> configurationMap = Configuration.GetSignatureConfig();

            // Create the PayPalAPIInterfaceServiceService service object to make the API call
            PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(configurationMap);

            SetExpressCheckoutRequestType setExpressCheckoutReq = new SetExpressCheckoutRequestType();
            SetExpressCheckoutRequestDetailsType details = new SetExpressCheckoutRequestDetailsType();

            string requestUrl = ConfigurationManager.AppSettings["HOSTING_ENDPOINT"].ToString();

            // (Required) URL to which the buyer's browser is returned after choosing to pay with PayPal. For digital goods, you must add JavaScript to this page to close the in-context experience.
            // Note:
            // PayPal recommends that the value be the final review page on which the buyer confirms the order and payment or billing agreement.
            UriBuilder uriBuilder = new UriBuilder(requestUrl);
            uriBuilder.Path = context.Request.ApplicationPath
                + (context.Request.ApplicationPath.EndsWith("/") ? string.Empty : "/")
                + "UseCaseSamples/SetExpressCheckoutForRecurringPayments.aspx";
            string returnUrl = uriBuilder.Uri.ToString();

            //(Required) URL to which the buyer is returned if the buyer does not approve the use of PayPal to pay you. For digital goods, you must add JavaScript to this page to close the in-context experience.
            // Note:
            // PayPal recommends that the value be the original page on which the buyer chose to pay with PayPal or establish a billing agreement.
            uriBuilder = new UriBuilder(requestUrl);
            uriBuilder.Path = context.Request.ApplicationPath
                + (context.Request.ApplicationPath.EndsWith("/") ? string.Empty : "/")
                + "UseCaseSamples/SetExpressCheckoutForRecurringPayments.aspx";
            string cancelUrl = uriBuilder.Uri.ToString();

            /*
             *  (Required) URL to which the buyer's browser is returned after choosing 
             *  to pay with PayPal. For digital goods, you must add JavaScript to this 
             *  page to close the in-context experience.
              Note:
                PayPal recommends that the value be the final review page on which the buyer 
                confirms the order and payment or billing agreement.
                Character length and limitations: 2048 single-byte characters
             */
            details.ReturnURL = returnUrl + "?currencyCodeType=" + parameters["currencyCode"];
            details.CancelURL = cancelUrl;

            /*
             *  (Optional) Email address of the buyer as entered during checkout.
             *  PayPal uses this value to pre-fill the PayPal membership sign-up portion on the PayPal pages.
             *	Character length and limitations: 127 single-byte alphanumeric characters
             */
            details.BuyerEmail = parameters["buyerMail"];

            decimal itemTotal = 0.0M;
            decimal orderTotal = 0.0M;

            // populate line item details
            //Cost of item. This field is required when you pass a value for ItemCategory.
            string amountItems = parameters["itemAmount"];

            /*
             * Item quantity. This field is required when you pass a value for ItemCategory. 
             * For digital goods (ItemCategory=Digital), this field is required.
               Character length and limitations: Any positive integer
               This field is introduced in version 53.0. 
             */
            string qtyItems = parameters["itemQuantity"];

            /*
             * Item name. This field is required when you pass a value for ItemCategory.
                Character length and limitations: 127 single-byte characters
                This field is introduced in version 53.0. 
             */
            string names = parameters["itemName"];

            List<PaymentDetailsItemType> lineItems = new List<PaymentDetailsItemType>();
            PaymentDetailsItemType item = new PaymentDetailsItemType();
            BasicAmountType amt = new BasicAmountType();

            //PayPal uses 3-character ISO-4217 codes for specifying currencies in fields and variables. 
            amt.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);
            amt.value = amountItems;
            item.Quantity = Convert.ToInt32(qtyItems);
            item.Name = names;
            item.Amount = amt;

            /*
             * Indicates whether an item is digital or physical. For digital goods, this field is required and must be set to Digital. It is one of the following values:
                1.Digital
                2.Physical
               This field is available since version 65.1. 
             */
            item.ItemCategory = (ItemCategoryType)Enum.Parse(typeof(ItemCategoryType), parameters["itemCategory"]);

            /*
             *  (Optional) Item description.
                Character length and limitations: 127 single-byte characters
                This field is introduced in version 53.0. 
             */
            item.Description = parameters["itemDescription"];
            lineItems.Add(item);

            /*
             * (Optional) Item sales tax.
                Note: You must set the currencyID attribute to one of 
                the 3-character currency codes for any of the supported PayPal currencies.
                Character length and limitations: Value is a positive number which cannot exceed $10,000 USD in any currency.
                It includes no currency symbol. It must have 2 decimal places, the decimal separator must be a period (.), 
                and the optional thousands separator must be a comma (,).
             */
            if (parameters["salesTax"] != string.Empty)
            {
                item.Tax = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), parameters["salesTax"]);
            }

            itemTotal += Convert.ToDecimal(qtyItems) * Convert.ToDecimal(amountItems);
            orderTotal += itemTotal;

            List<PaymentDetailsType> payDetails = new List<PaymentDetailsType>();
            PaymentDetailsType paydtl = new PaymentDetailsType();
            /*
             * How you want to obtain payment. When implementing parallel payments, 
             * this field is required and must be set to Order.
             *  When implementing digital goods, this field is required and must be set to Sale.
             *   If the transaction does not include a one-time purchase, this field is ignored. 
             *   It is one of the following values:

                Sale – This is a final sale for which you are requesting payment (default).
                Authorization – This payment is a basic authorization subject to settlement with PayPal Authorization and Capture.
                Order – This payment is an order authorization subject to settlement with PayPal Authorization and Capture.
             */
            paydtl.PaymentAction = (PaymentActionCodeType)Enum.Parse(typeof(PaymentActionCodeType), parameters["paymentType"]);

            /*
             *  (Optional) Total shipping costs for this order.
                Note:
                You must set the currencyID attribute to one of the 3-character currency codes 
                for any of the supported PayPal currencies.
                Character length and limitations: 
                Value is a positive number which cannot exceed $10,000 USD in any currency.
                It includes no currency symbol. 
                It must have 2 decimal places, the decimal separator must be a period (.), 
                and the optional thousands separator must be a comma (,)
             */
            if (parameters["shippingTotal"] != string.Empty)
            {
                BasicAmountType shippingTotal = new BasicAmountType();
                shippingTotal.value = parameters["shippingTotal"];
                shippingTotal.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);
                orderTotal += Convert.ToDecimal(parameters["shippingTotal"]);
                paydtl.ShippingTotal = shippingTotal;
            }

            /*
             *  (Optional) Total shipping insurance costs for this order. 
             *  The value must be a non-negative currency amount or null if you offer insurance options.
                 Note:
                 You must set the currencyID attribute to one of the 3-character currency 
                 codes for any of the supported PayPal currencies.
                 Character length and limitations: 
                 Value is a positive number which cannot exceed $10,000 USD in any currency. 
                 It includes no currency symbol. It must have 2 decimal places,
                 the decimal separator must be a period (.), 
                 and the optional thousands separator must be a comma (,).
                 InsuranceTotal is available since version 53.0.
             */
            if (parameters["insuranceTotal"] != string.Empty)
            {
                paydtl.InsuranceTotal = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), parameters["insuranceTotal"]);
                paydtl.InsuranceOptionOffered = "true";
                orderTotal += Convert.ToDecimal(parameters["insuranceTotal"]);
            }

            /*
             *  (Optional) Total handling costs for this order.
                 Note:
                 You must set the currencyID attribute to one of the 3-character currency codes 
                 for any of the supported PayPal currencies.
                 Character length and limitations: Value is a positive number which 
                 cannot exceed $10,000 USD in any currency.
                 It includes no currency symbol. It must have 2 decimal places, 
                 the decimal separator must be a period (.), and the optional 
                 thousands separator must be a comma (,). 
             */
            if (parameters["handlingTotal"] != string.Empty)
            {
                paydtl.HandlingTotal = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), parameters["handlingTotal"]);
                orderTotal += Convert.ToDecimal(parameters["handlingTotal"]);
            }

            /*
             *  (Optional) Sum of tax for all items in this order.
                 Note:
                 You must set the currencyID attribute to one of the 3-character currency codes
                 for any of the supported PayPal currencies.
                 Character length and limitations: Value is a positive number which 
                 cannot exceed $10,000 USD in any currency. It includes no currency symbol.
                 It must have 2 decimal places, the decimal separator must be a period (.),
                 and the optional thousands separator must be a comma (,).
             */
            if (parameters["taxTotal"] != string.Empty)
            {
                paydtl.TaxTotal = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), parameters["taxTotal"]);
                orderTotal += Convert.ToDecimal(parameters["taxTotal"]);
            }

            /*
             *  (Optional) Description of items the buyer is purchasing.
                 Note:
                 The value you specify is available only if the transaction includes a purchase.
                 This field is ignored if you set up a billing agreement for a recurring payment 
                 that is not immediately charged.
                 Character length and limitations: 127 single-byte alphanumeric characters
             */
            if (parameters["orderDescription"] != string.Empty)
            {
                paydtl.OrderDescription = parameters["orderDescription"];
            }


            BasicAmountType itemsTotal = new BasicAmountType();
            itemsTotal.value = Convert.ToString(itemTotal);

            //PayPal uses 3-character ISO-4217 codes for specifying currencies in fields and variables. 
            itemsTotal.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);

            paydtl.OrderTotal = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), Convert.ToString(orderTotal));
            paydtl.PaymentDetailsItem = lineItems;

            paydtl.ItemTotal = itemsTotal;
            /*
             *  (Optional) Your URL for receiving Instant Payment Notification (IPN) 
             *  about this transaction. If you do not specify this value in the request, 
             *  the notification URL from your Merchant Profile is used, if one exists.
                Important:
                The notify URL applies only to DoExpressCheckoutPayment. 
                This value is ignored when set in SetExpressCheckout or GetExpressCheckoutDetails.
                Character length and limitations: 2,048 single-byte alphanumeric characters
             */
            paydtl.NotifyURL = parameters["notifyURL"];

            payDetails.Add(paydtl);
            details.PaymentDetails = payDetails;

            if (parameters["billingAgreementText"] != string.Empty)
            {
                /*
                 *  (Required) Type of billing agreement. For recurring payments,
                 *   this field must be set to RecurringPayments. 
                 *   In this case, you can specify up to ten billing agreements. 
                 *   Other defined values are not valid.
                     Type of billing agreement for reference transactions. 
                     You must have permission from PayPal to use this field. 
                     This field must be set to one of the following values:
                        1. MerchantInitiatedBilling - PayPal creates a billing agreement 
                           for each transaction associated with buyer.You must specify 
                           version 54.0 or higher to use this option.
                        2. MerchantInitiatedBillingSingleAgreement - PayPal creates a 
                           single billing agreement for all transactions associated with buyer.
                           Use this value unless you need per-transaction billing agreements. 
                           You must specify version 58.0 or higher to use this option.

                 */
                BillingAgreementDetailsType billingAgreement = new BillingAgreementDetailsType((BillingCodeType)Enum.Parse(typeof(BillingCodeType), parameters["billingType"]));

                /*
                 * Description of goods or services associated with the billing agreement. 
                 * This field is required for each recurring payment billing agreement.
                 *  PayPal recommends that the description contain a brief summary of 
                 *  the billing agreement terms and conditions. For example,
                 *   buyer is billed at "9.99 per month for 2 years".
                   Character length and limitations: 127 single-byte alphanumeric characters
                 */
                billingAgreement.BillingAgreementDescription = parameters["billingAgreementText"];
                List<BillingAgreementDetailsType> billList = new List<BillingAgreementDetailsType>();
                billList.Add(billingAgreement);
                details.BillingAgreementDetails = billList;
            }

            setExpressCheckoutReq.SetExpressCheckoutRequestDetails = details;
            SetExpressCheckoutReq expressCheckoutReq = new SetExpressCheckoutReq();
            expressCheckoutReq.SetExpressCheckoutRequest = setExpressCheckoutReq;

            SetExpressCheckoutResponseType resp = null;
            try
            {
                resp = service.SetExpressCheckout(expressCheckoutReq);
            }
            catch (System.Exception e)
            {
                context.Response.Write(e.StackTrace);
                return;
            }

            // Display response values. 
            Dictionary<string, string> keyResponseParams = new Dictionary<string, string>();
            string redirectUrl = null;
            if (!(resp.Ack.Equals(AckCode.FAILURE) && !(resp.Ack.Equals(AckCode.FAILUREWITHWARNING))))
            {
                redirectUrl = ConfigurationManager.AppSettings["PAYPAL_REDIRECT_URL"].ToString() + "_express-checkout&token=" + resp.Token;
                keyResponseParams.Add("Acknowledgement", resp.Ack.ToString());
            }
            displayResponse(context, "SetExpressCheckoutForRecurringPayments", keyResponseParams, service.getLastRequest(), service.getLastResponse(), resp.Errors, redirectUrl);
        }

        /// <summary>
        /// Handles Recurring Payments Using CreditCard
        /// </summary>
        /// <param name="context"></param>
        private void RecurringPaymentsUsingCreditCard(HttpContext context)
        {
            NameValueCollection parameters = context.Request.Params;

            // Configuration map containing signature credentials and other required configuration.
            // For a full list of configuration parameters refer at 
            // [https://github.com/paypal/merchant-sdk-dotnet/wiki/SDK-Configuration-Parameters]
            Dictionary<string, string> configurationMap = Configuration.GetSignatureConfig();

            // Create the PayPalAPIInterfaceServiceService service object to make the API call
            PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(configurationMap);

            CreateRecurringPaymentsProfileReq req = new CreateRecurringPaymentsProfileReq();
            CreateRecurringPaymentsProfileRequestType reqType = new CreateRecurringPaymentsProfileRequestType();
           
            /*
             *  (Required) The date when billing for this profile begins.
                Note:
                The profile may take up to 24 hours for activation.
                Character length and limitations: Must be a valid date, in UTC/GMT format
             */
            RecurringPaymentsProfileDetailsType profileDetails = new RecurringPaymentsProfileDetailsType(parameters["billingStartDate"] + "T00:00:00:000Z");
            
            /*
             *  (Optional) Full name of the person receiving the product or service paid for
             *   by the recurring payment. If not present, the name in the buyer's PayPal
             *   account is used.
                Character length and limitations: 32 single-byte characters
             */
            if (parameters["subscriberName"] != string.Empty)
            {
                profileDetails.SubscriberName = parameters["subscriberName"];
            }
            else if (parameters["shippingName"] != string.Empty)
            {
                AddressType shippingAddr = new AddressType();
                
                /*
                 * Person's name associated with this shipping address. 
                 * It is required if using a shipping address.
                   Character length and limitations: 32 single-byte characters
                 */
                shippingAddr.Name = parameters["shippingName"];

                /*
                 * First street address. It is required if using a shipping address.
                   Character length and limitations: 100 single-byte characters
                 */
                shippingAddr.Street1 = parameters["shippingStreet1"];

                /*
                 *  (Optional) Second street address.
                    Character length and limitations: 100 single-byte characters
                 */
                shippingAddr.Street2 = parameters["shippingStreet2"];

                /*
                 * Optional) Phone number.
                   Character length and limitations: 20 single-byte characters
                 */
                shippingAddr.Phone = parameters["shippingPhone"];

                /*
                 * Name of city. It is required if using a shipping address.
                   Character length and limitations: 40 single-byte characters
                 */
                shippingAddr.CityName = parameters["shippingCity"];

                /*
                 * State or province. It is required if using a shipping address.
                   Character length and limitations: 40 single-byte characters
                 */
                shippingAddr.StateOrProvince = parameters["shippingState"];

                /*
                 * Country code. It is required if using a shipping address.
                  Character length and limitations: 2 single-byte characters
                 */
                shippingAddr.CountryName = parameters["shippingCountry"];
                /*
                 * U.S. ZIP code or other country-specific postal code. 
                 * It is required if using a U.S. shipping address; may be required 
                 * for other countries.
                   Character length and limitations: 20 single-byte characters
                 */
                shippingAddr.PostalCode = parameters["shippingPostalCode"];
                profileDetails.SubscriberShippingAddress = shippingAddr;
            }

            // Populate schedule details
            ScheduleDetailsType scheduleDetails = new ScheduleDetailsType();

            /*
             *  (Required) Description of the recurring payment.
                Note:
                You must ensure that this field matches the corresponding billing agreement 
                description included in the SetExpressCheckout request.
                Character length and limitations: 127 single-byte alphanumeric characters
             */
            scheduleDetails.Description = parameters["profileDescription"];

            /*
             *  (Optional) Number of scheduled payments that can fail before the profile 
             *  is automatically suspended. An IPN message is sent to the merchant when the 
             *  specified number of failed payments is reached.
                 Character length and limitations: Number string representing an integer
             */
            if (parameters["maxFailedPayments"] != string.Empty)
            {
                scheduleDetails.MaxFailedPayments = Convert.ToInt32(parameters["maxFailedPayments"]);
            }
           
            /*
             *  (Optional) Indicates whether you would like PayPal to automatically bill 
             *  the outstanding balance amount in the next billing cycle. 
             *  The outstanding balance is the total amount of any previously failed 
             *  scheduled payments that have yet to be successfully paid. 
             *  It is one of the following values:
                NoAutoBill – PayPal does not automatically bill the outstanding balance.
                AddToNextBilling – PayPal automatically bills the outstanding balance.
             */
            if (parameters["autoBillOutstandingAmount"] != string.Empty)
            {
                scheduleDetails.AutoBillOutstandingAmount = (AutoBillType)Enum.Parse(typeof(AutoBillType), parameters["autoBillOutstandingAmount"]);
            }

            CurrencyCodeType currency = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), "USD");

            /*
             *  (Optional) Initial non-recurring payment amount due immediately upon profile creation.
             *   Use an initial amount for enrolment or set-up fees.
                 Note:
                 All amounts included in the request must have the same currency.
                 Character length and limitations:
                  Value is a positive number which cannot exceed $10,000 USD in any currency.
                  It includes no currency symbol. 
                  It must have 2 decimal places, the decimal separator must be a period (.),
                  and the optional thousands separator must be a comma (,). 
             */
            if (parameters["initialAmount"] != string.Empty)
            {
                ActivationDetailsType activationDetails = new ActivationDetailsType(new BasicAmountType(currency, parameters["initialAmount"]));
                /*
                 *  (Optional) Action you can specify when a payment fails. 
                 *  It is one of the following values:
                    1. ContinueOnFailure – By default, PayPal suspends the pending profile in the event that
                     the initial payment amount fails. You can override this default behavior by setting 
                     this field to ContinueOnFailure. Then, if the initial payment amount fails, 
                     PayPal adds the failed payment amount to the outstanding balance for this 
                     recurring payment profile. When you specify ContinueOnFailure, a success code is
                     returned to you in the CreateRecurringPaymentsProfile response and the recurring
                     payments profile is activated for scheduled billing immediately. 
                     You should check your IPN messages or PayPal account for updates of the
                     payment status.
                    2. CancelOnFailure – If this field is not set or you set it to CancelOnFailure,
                     PayPal creates the recurring payment profile, but places it into a pending status
                     until the initial payment completes. If the initial payment clears, 
                     PayPal notifies you by IPN that the pending profile has been activated. 
                     If the payment fails, PayPal notifies you by IPN that the pending profile 
                     has been canceled.

                 */
                if (parameters["failedInitialAmountAction"] != string.Empty)
                {
                    activationDetails.FailedInitialAmountAction = (FailedPaymentActionType)Enum.Parse(typeof(FailedPaymentActionType), parameters["failedInitialAmountAction"]);
                }
                scheduleDetails.ActivationDetails = activationDetails;
            }
            if (parameters["trialBillingAmount"] != string.Empty)
            {
                /*
                 * Number of billing periods that make up one billing cycle; 
                 * required if you specify an optional trial period.
                   The combination of billing frequency and billing period must be 
                   less than or equal to one year. For example, if the billing cycle is Month,
                   the maximum value for billing frequency is 12. Similarly, 
                   if the billing cycle is Week, the maximum value for billing frequency is 52.
                   Note:
                   If the billing period is SemiMonth, the billing frequency must be 1.

                 */
                int frequency = Convert.ToInt32(parameters["trialBillingFrequency"]);

                /*
                 * Billing amount for each billing cycle during this payment period; 
                 * required if you specify an optional trial period. 
                 * This amount does not include shipping and tax amounts.
                   Note:
                    All amounts in the CreateRecurringPaymentsProfile request must have 
                    the same currency.
                    Character length and limitations: 
                    Value is a positive number which cannot exceed $10,000 USD in any currency. 
                    It includes no currency symbol. 
                    It must have 2 decimal places, the decimal separator must be a period (.),
                    and the optional thousands separator must be a comma (,).
                 */
                BasicAmountType paymentAmount = new BasicAmountType(currency, parameters["trialBillingAmount"]);

                /*
                 * Unit for billing during this subscription period; 
                 * required if you specify an optional trial period. 
                 * It is one of the following values: [Day, Week, SemiMonth, Month, Year]
                   For SemiMonth, billing is done on the 1st and 15th of each month.
                   Note:
                   The combination of BillingPeriod and BillingFrequency cannot exceed one year.
                 */
                BillingPeriodType period = (BillingPeriodType)Enum.Parse(typeof(BillingPeriodType), parameters["trialBillingPeriod"]);

                /*
                 * Number of billing periods that make up one billing cycle; 
                 * required if you specify an optional trial period.
                   The combination of billing frequency and billing period must be 
                   less than or equal to one year. For example, if the billing cycle is Month,
                   the maximum value for billing frequency is 12. Similarly, 
                   if the billing cycle is Week, the maximum value for billing frequency is 52.
                  Note:
                    If the billing period is SemiMonth, the billing frequency must be 1.
                 */
                int numCycles = Convert.ToInt32(parameters["trialBillingCycles"]);

                BillingPeriodDetailsType trialPeriod = new BillingPeriodDetailsType(period, frequency, paymentAmount);
                trialPeriod.TotalBillingCycles = numCycles;

                /*
                 *  (Optional) Shipping amount for each billing cycle during this payment period.
                    Note:
                    All amounts in the request must have the same currency.
                 */
                if (parameters["trialShippingAmount"] != string.Empty)
                {
                    trialPeriod.ShippingAmount = new BasicAmountType(currency, parameters["trialShippingAmount"]);
                }

                /*
                 *  (Optional) Tax amount for each billing cycle during this payment period.
                    Note:
                    All amounts in the request must have the same currency.
                    Character length and limitations: 
                    Value is a positive number which cannot exceed $10,000 USD in any currency.
                    It includes no currency symbol. It must have 2 decimal places, 
                    the decimal separator must be a period (.), and the optional 
                    thousands separator must be a comma (,).
                 */
                if (parameters["trialTaxAmount"] != string.Empty)
                {
                    trialPeriod.TaxAmount = new BasicAmountType(currency, parameters["trialTaxAmount"]);
                }

                scheduleDetails.TrialPeriod = trialPeriod;
            }

            if (parameters["billingAmount"] != string.Empty)
            {
                /*
                 *  (Required) Number of billing periods that make up one billing cycle.
                    The combination of billing frequency and billing period must be less than 
                    or equal to one year. For example, if the billing cycle is Month, 
                    the maximum value for billing frequency is 12. Similarly, 
                    if the billing cycle is Week, the maximum value for billing frequency is 52.
                    Note:
                    If the billing period is SemiMonth, the billing frequency must be 1.
                 */
                int frequency = Convert.ToInt32(parameters["billingFrequency"]);

                /*
                 *  (Required) Billing amount for each billing cycle during this payment period. 
                 *  This amount does not include shipping and tax amounts.
                    Note:
                    All amounts in the CreateRecurringPaymentsProfile request must have the same 
                    currency.
                    Character length and limitations: Value is a positive number which cannot 
                    exceed $10,000 USD in any currency. It includes no currency symbol. 
                    It must have 2 decimal places, the decimal separator must be a period (.), 
                    and the optional thousands separator must be a comma (,). 
                 */
                BasicAmountType paymentAmount = new BasicAmountType(currency, parameters["billingAmount"]);

                /*
                 *  (Required) Unit for billing during this subscription period. 
                 *  It is one of the following values:
                     [Day, Week, SemiMonth, Month, Year]
                    For SemiMonth, billing is done on the 1st and 15th of each month.
                    Note:
                    The combination of BillingPeriod and BillingFrequency cannot exceed one year.
                 */
                BillingPeriodType period = (BillingPeriodType)Enum.Parse(typeof(BillingPeriodType), parameters["billingPeriod"]);

                /*
                 *  (Optional) Number of billing cycles for payment period.
                    For the regular payment period, if no value is specified or the value is 0, 
                    the regular payment period continues until the profile is canceled or deactivated.
                    For the regular payment period, if the value is greater than 0, 
                    the regular payment period will expire after the trial period is 
                    finished and continue at the billing frequency for TotalBillingCycles cycles.

                 */
                int numCycles = Convert.ToInt32(parameters["totalBillingCycles"]);

                BillingPeriodDetailsType paymentPeriod = new BillingPeriodDetailsType(period, frequency, paymentAmount);

                paymentPeriod.TotalBillingCycles = numCycles;
                /*
                 *  (Optional) Shipping amount for each billing cycle during this payment period.
                    Note:
                    All amounts in the request must have the same currency.
                 */
                if (parameters["shippingAmount"] != string.Empty)
                {
                    paymentPeriod.ShippingAmount = new BasicAmountType(currency, parameters["shippingAmount"]);
                }
                /*
                 *  (Optional) Tax amount for each billing cycle during this payment period.
                    Note:
                    All amounts in the request must have the same currency.
                    Character length and limitations: 
                    Value is a positive number which cannot exceed $10,000 USD in any currency.
                    It includes no currency symbol. It must have 2 decimal places, 
                    the decimal separator must be a period (.), and the optional 
                    thousands separator must be a comma (,).
                 */
                if (parameters["taxAmount"] != string.Empty)
                {
                    paymentPeriod.TaxAmount = new BasicAmountType(currency, parameters["taxAmount"]);
                }
                scheduleDetails.PaymentPeriod = paymentPeriod;
            }

            CreateRecurringPaymentsProfileRequestDetailsType reqDetails = new CreateRecurringPaymentsProfileRequestDetailsType(profileDetails, scheduleDetails);

            /*
             * credit card number is required for CreateRecurringPaymentsProfile.  
             * Each CreateRecurringPaymentsProfile request creates 
             * a single recurring payments profile.
                Note:
             */
            CreditCardDetailsType cc = new CreditCardDetailsType();
            /*
             *  (Required) Credit card number.
                Character length and limitations: Numeric characters only with no spaces 
                or punctuation. The string must conform with modulo and length required 
                by each credit card type.
             */
            cc.CreditCardNumber = parameters["creditCardNumber"];

            /*
             * Card Verification Value, version 2. 
             * Your Merchant Account settings determine whether this field is required.
             * To comply with credit card processing regulations, you must not store this 
             * value after a transaction has been completed.
               Character length and limitations: 
               For Visa, MasterCard, and Discover, the value is exactly 3 digits. 
               For American Express, the value is exactly 4 digits.
             */
            cc.CVV2 = parameters["cvv"];

            //Expiry Month
            cc.ExpMonth = Convert.ToInt32(parameters["expMonth"]);

            //Expiry Year
            cc.ExpYear = Convert.ToInt32(parameters["expYear"]);
            PayerInfoType payerInfo = new PayerInfoType();

            /*
             *  (Required) Email address of buyer.
                Character length and limitations: 127 single-byte characters
             */
            payerInfo.Payer = parameters["BuyerEmailId"];
            cc.CardOwner = payerInfo;

            /*
             * (Optional) Type of credit card. 
             * For UK, only Maestro, MasterCard, Discover, and Visa are allowable. 
             * For Canada, only MasterCard and Visa are allowable and 
             * Interac debit cards are not supported. It is one of the following values:
                [ Visa, MasterCard, Discover, Amex, Maestro: See note.]
             Note:
              If the credit card type is Maestro, you must set CURRENCYCODE to GBP. 
              In addition, you must specify either STARTDATE or ISSUENUMBER.
             */
            CreditCardTypeType type =  (CreditCardTypeType)Enum.Parse(typeof(CreditCardTypeType), parameters["creditCardType"]);
            cc.CreditCardType = type;
                       

            reqDetails.CreditCard = cc;

            reqType.CreateRecurringPaymentsProfileRequestDetails = reqDetails;
            req.CreateRecurringPaymentsProfileRequest = reqType;

            CreateRecurringPaymentsProfileResponseType resp = null;
            try
            {
                resp = service.CreateRecurringPaymentsProfile(req);
            }
            catch (System.Exception e)
            {
                context.Response.Write(e.StackTrace);
                return;
            }


            // Display response values. 
            Dictionary<string, string> keyResponseParams = new Dictionary<string, string>();
            string redirectUrl = null;
            if (!(resp.Ack.Equals(AckCode.FAILURE) && !(resp.Ack.Equals(AckCode.FAILUREWITHWARNING))))
            {
                keyResponseParams.Add("Acknowledgement", resp.Ack.ToString());
            }
            displayResponse(context, "RecurringPaymentsUsingCreditCard", keyResponseParams, service.getLastRequest(), service.getLastResponse(), resp.Errors, redirectUrl);
        }

        /// <summary>
        /// Handles Set ExpressCheckout Payment Authorization
        /// </summary>
        /// <param name="context"></param>
        private void SetExpressCheckoutPaymentAuthorization(HttpContext context)
        {
            NameValueCollection parameters = context.Request.Params;

            // Configuration map containing signature credentials and other required configuration.
            // For a full list of configuration parameters refer at 
            // [https://github.com/paypal/merchant-sdk-dotnet/wiki/SDK-Configuration-Parameters]
            Dictionary<string, string> configurationMap = Configuration.GetSignatureConfig();

            // Create the PayPalAPIInterfaceServiceService service object to make the API call
            PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(configurationMap);

            SetExpressCheckoutRequestType setExpressCheckoutReq = new SetExpressCheckoutRequestType();
            SetExpressCheckoutRequestDetailsType details = new SetExpressCheckoutRequestDetailsType();

            string requestUrl = ConfigurationManager.AppSettings["HOSTING_ENDPOINT"].ToString();

            // (Required) URL to which the buyer's browser is returned after choosing to pay with PayPal. For digital goods, you must add JavaScript to this page to close the in-context experience.
            // Note:
            // PayPal recommends that the value be the final review page on which the buyer confirms the order and payment or billing agreement.
            UriBuilder uriBuilder = new UriBuilder(requestUrl);
            uriBuilder.Path = context.Request.ApplicationPath
                + (context.Request.ApplicationPath.EndsWith("/") ? string.Empty : "/")
                + "UseCaseSamples/DoExpressCheckout.aspx";
            string returnUrl = uriBuilder.Uri.ToString();

            //(Required) URL to which the buyer is returned if the buyer does not approve the use of PayPal to pay you. For digital goods, you must add JavaScript to this page to close the in-context experience.
            // Note:
            // PayPal recommends that the value be the original page on which the buyer chose to pay with PayPal or establish a billing agreement.
            uriBuilder = new UriBuilder(requestUrl);
            uriBuilder.Path = context.Request.ApplicationPath
                + (context.Request.ApplicationPath.EndsWith("/") ? string.Empty : "/")
                + "UseCaseSamples/SetExpressCheckoutPaymentAuthorization.aspx";
            string cancelUrl = uriBuilder.Uri.ToString();

            /*
              *  (Required) URL to which the buyer's browser is returned after choosing 
              *  to pay with PayPal. For digital goods, you must add JavaScript to this 
              *  page to close the in-context experience.
               Note:
                 PayPal recommends that the value be the final review page on which the buyer 
                 confirms the order and payment or billing agreement.
                 Character length and limitations: 2048 single-byte characters
              */
            details.ReturnURL = returnUrl + "?currencyCodeType=" + parameters["currencyCode"] + "&paymentType=" + parameters["paymentType"];
            details.CancelURL = cancelUrl;

            /*
            *  (Optional) Email address of the buyer as entered during checkout.
            *  PayPal uses this value to pre-fill the PayPal membership sign-up portion on the PayPal pages.
            *	Character length and limitations: 127 single-byte alphanumeric characters
            */
            details.BuyerEmail = parameters["buyerMail"];

            decimal itemTotal = 0.0M;
            decimal orderTotal = 0.0M;

            // populate line item details
            //Cost of item. This field is required when you pass a value for ItemCategory.
            string amountItems = parameters["itemAmount"];

            /*
             * Item quantity. This field is required when you pass a value for ItemCategory. 
             * For digital goods (ItemCategory=Digital), this field is required.
               Character length and limitations: Any positive integer
               This field is introduced in version 53.0. 
             */
            string qtyItems = parameters["itemQuantity"];

            /*
             * Item name. This field is required when you pass a value for ItemCategory.
                Character length and limitations: 127 single-byte characters
                This field is introduced in version 53.0. 
             */
            string names = parameters["itemName"];

            List<PaymentDetailsItemType> lineItems = new List<PaymentDetailsItemType>();
            PaymentDetailsItemType item = new PaymentDetailsItemType();
            BasicAmountType amt = new BasicAmountType();

            //PayPal uses 3-character ISO-4217 codes for specifying currencies in fields and variables. 
            amt.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);
            amt.value = amountItems;
            item.Quantity = Convert.ToInt32(qtyItems);
            item.Name = names;
            item.Amount = amt;
           
            /*
             * Indicates whether an item is digital or physical. For digital goods, this field is required and must be set to Digital. It is one of the following values:
                1.Digital
                2.Physical
               This field is available since version 65.1. 
             */
            item.ItemCategory = (ItemCategoryType)Enum.Parse(typeof(ItemCategoryType), parameters["itemCategory"]);

            /*
             *  (Optional) Item description.
                Character length and limitations: 127 single-byte characters
                This field is introduced in version 53.0. 
             */
            item.Description = parameters["itemDescription"];
            lineItems.Add(item);

            /*
             * (Optional) Item sales tax.
                Note: You must set the currencyID attribute to one of 
                the 3-character currency codes for any of the supported PayPal currencies.
                Character length and limitations: Value is a positive number which cannot exceed $10,000 USD in any currency.
                It includes no currency symbol. It must have 2 decimal places, the decimal separator must be a period (.), 
                and the optional thousands separator must be a comma (,).
             */
            if (parameters["salesTax"] != string.Empty)
            {
                item.Tax = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), parameters["salesTax"]);
            }

            itemTotal += Convert.ToDecimal(qtyItems) * Convert.ToDecimal(amountItems);
            orderTotal += itemTotal;

            List<PaymentDetailsType> payDetails = new List<PaymentDetailsType>();
            PaymentDetailsType paydtl = new PaymentDetailsType();
            /*
             * How you want to obtain payment. When implementing parallel payments, 
             * this field is required and must be set to Order.
             *  When implementing digital goods, this field is required and must be set to Sale.
             *   If the transaction does not include a one-time purchase, this field is ignored. 
             *   It is one of the following values:

                Sale – This is a final sale for which you are requesting payment (default).
                Authorization – This payment is a basic authorization subject to settlement with PayPal Authorization and Capture.
                Order – This payment is an order authorization subject to settlement with PayPal Authorization and Capture.
             */
            paydtl.PaymentAction = (PaymentActionCodeType)Enum.Parse(typeof(PaymentActionCodeType), parameters["paymentType"]);

            /*
             *  (Optional) Total shipping costs for this order.
                Note:
                You must set the currencyID attribute to one of the 3-character currency codes 
                for any of the supported PayPal currencies.
                Character length and limitations: 
                Value is a positive number which cannot exceed $10,000 USD in any currency.
                It includes no currency symbol. 
                It must have 2 decimal places, the decimal separator must be a period (.), 
                and the optional thousands separator must be a comma (,)
             */
            if (parameters["shippingTotal"] != string.Empty)
            {
                BasicAmountType shippingTotal = new BasicAmountType();
                shippingTotal.value = parameters["shippingTotal"];
                shippingTotal.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);
                orderTotal += Convert.ToDecimal(parameters["shippingTotal"]);
                paydtl.ShippingTotal = shippingTotal;
            }

            /*
             *  (Optional) Total shipping insurance costs for this order. 
             *  The value must be a non-negative currency amount or null if you offer insurance options.
                 Note:
                 You must set the currencyID attribute to one of the 3-character currency 
                 codes for any of the supported PayPal currencies.
                 Character length and limitations: 
                 Value is a positive number which cannot exceed $10,000 USD in any currency. 
                 It includes no currency symbol. It must have 2 decimal places,
                 the decimal separator must be a period (.), 
                 and the optional thousands separator must be a comma (,).
                 InsuranceTotal is available since version 53.0.
             */
            if (parameters["insuranceTotal"] != string.Empty)
            {
                paydtl.InsuranceTotal = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), parameters["insuranceTotal"]);
                paydtl.InsuranceOptionOffered = "true";
                orderTotal += Convert.ToDecimal(parameters["insuranceTotal"]);
            }

            /*
            *  (Optional) Total handling costs for this order.
                Note:
                You must set the currencyID attribute to one of the 3-character currency codes 
                for any of the supported PayPal currencies.
                Character length and limitations: Value is a positive number which 
                cannot exceed $10,000 USD in any currency.
                It includes no currency symbol. It must have 2 decimal places, 
                the decimal separator must be a period (.), and the optional 
                thousands separator must be a comma (,). 
            */
            if (parameters["handlingTotal"] != string.Empty)
            {
                paydtl.HandlingTotal = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), parameters["handlingTotal"]);
                orderTotal += Convert.ToDecimal(parameters["handlingTotal"]);
            }

            /*
             *  (Optional) Sum of tax for all items in this order.
                 Note:
                 You must set the currencyID attribute to one of the 3-character currency codes
                 for any of the supported PayPal currencies.
                 Character length and limitations: Value is a positive number which 
                 cannot exceed $10,000 USD in any currency. It includes no currency symbol.
                 It must have 2 decimal places, the decimal separator must be a period (.),
                 and the optional thousands separator must be a comma (,).
             */
            if (parameters["taxTotal"] != string.Empty)
            {
                paydtl.TaxTotal = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), parameters["taxTotal"]);
                orderTotal += Convert.ToDecimal(parameters["taxTotal"]);
            }

            /*
             *  (Optional) Description of items the buyer is purchasing.
                 Note:
                 The value you specify is available only if the transaction includes a purchase.
                 This field is ignored if you set up a billing agreement for a recurring payment 
                 that is not immediately charged.
                 Character length and limitations: 127 single-byte alphanumeric characters
             */
            if (parameters["orderDescription"] != string.Empty)
            {
                paydtl.OrderDescription = parameters["orderDescription"];
            }

            BasicAmountType itemsTotal = new BasicAmountType();
            itemsTotal.value = Convert.ToString(itemTotal);

            //PayPal uses 3-character ISO-4217 codes for specifying currencies in fields and variables. 
            itemsTotal.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);

            paydtl.OrderTotal = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), Convert.ToString(orderTotal));
            paydtl.PaymentDetailsItem = lineItems;

            paydtl.ItemTotal = itemsTotal;
            /*
             *  (Optional) Your URL for receiving Instant Payment Notification (IPN) 
             *  about this transaction. If you do not specify this value in the request, 
             *  the notification URL from your Merchant Profile is used, if one exists.
                Important:
                The notify URL applies only to DoExpressCheckoutPayment. 
                This value is ignored when set in SetExpressCheckout or GetExpressCheckoutDetails.
                Character length and limitations: 2,048 single-byte alphanumeric characters
             */
            paydtl.NotifyURL = parameters["notifyURL"];

            payDetails.Add(paydtl);
            details.PaymentDetails = payDetails;

            setExpressCheckoutReq.SetExpressCheckoutRequestDetails = details;

            SetExpressCheckoutReq expressCheckoutReq = new SetExpressCheckoutReq();
            expressCheckoutReq.SetExpressCheckoutRequest = setExpressCheckoutReq;

            SetExpressCheckoutResponseType resp = null;
            try
            {
                resp = service.SetExpressCheckout(expressCheckoutReq);
            }
            catch (System.Exception e)
            {
                context.Response.Write(e.StackTrace);
                return;
            }

            // Display response values. 
            Dictionary<string, string> keyResponseParams = new Dictionary<string, string>();
            string redirectUrl = null;
            if (!(resp.Ack.Equals(AckCode.FAILURE) && !(resp.Ack.Equals(AckCode.FAILUREWITHWARNING))))
            {
                redirectUrl = ConfigurationManager.AppSettings["PAYPAL_REDIRECT_URL"].ToString() + "_express-checkout&token=" + resp.Token;
                keyResponseParams.Add("Acknowledgement", resp.Ack.ToString());
            }
            displayResponse(context, "SetExpressCheckoutPaymentAuthorization", keyResponseParams, service.getLastRequest(), service.getLastResponse(), resp.Errors, redirectUrl);
        }

        /// <summary>
        /// Handles Set ExpressCheckout Payment Order
        /// </summary>
        /// <param name="context"></param>
        private void SetExpressCheckoutPaymentOrder(HttpContext context)
        {
            NameValueCollection parameters = context.Request.Params;

            // Configuration map containing signature credentials and other required configuration.
            // For a full list of configuration parameters refer at 
            // [https://github.com/paypal/merchant-sdk-dotnet/wiki/SDK-Configuration-Parameters]
            Dictionary<string, string> configurationMap = Configuration.GetSignatureConfig();

            // Create the PayPalAPIInterfaceServiceService service object to make the API call
            PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(configurationMap);

            SetExpressCheckoutRequestType setExpressCheckoutReq = new SetExpressCheckoutRequestType();
            SetExpressCheckoutRequestDetailsType details = new SetExpressCheckoutRequestDetailsType();

            string requestUrl = ConfigurationManager.AppSettings["HOSTING_ENDPOINT"].ToString();

            // (Required) URL to which the buyer's browser is returned after choosing to pay with PayPal. For digital goods, you must add JavaScript to this page to close the in-context experience.
            // Note:
            // PayPal recommends that the value be the final review page on which the buyer confirms the order and payment or billing agreement.
            UriBuilder uriBuilder = new UriBuilder(requestUrl);
            uriBuilder.Path = context.Request.ApplicationPath
                + (context.Request.ApplicationPath.EndsWith("/") ? string.Empty : "/")
                + "UseCaseSamples/DoExpressCheckout.aspx";
            string returnUrl = uriBuilder.Uri.ToString();

            //(Required) URL to which the buyer is returned if the buyer does not approve the use of PayPal to pay you. For digital goods, you must add JavaScript to this page to close the in-context experience.
            // Note:
            // PayPal recommends that the value be the original page on which the buyer chose to pay with PayPal or establish a billing agreement.
            uriBuilder = new UriBuilder(requestUrl);
            uriBuilder.Path = context.Request.ApplicationPath
                + (context.Request.ApplicationPath.EndsWith("/") ? string.Empty : "/")
                + "UseCaseSamples/SetExpressCheckoutPaymentAuthorization.aspx";
            string cancelUrl = uriBuilder.Uri.ToString();

            /*
              *  (Required) URL to which the buyer's browser is returned after choosing 
              *  to pay with PayPal. For digital goods, you must add JavaScript to this 
              *  page to close the in-context experience.
               Note:
                 PayPal recommends that the value be the final review page on which the buyer 
                 confirms the order and payment or billing agreement.
                 Character length and limitations: 2048 single-byte characters
              */
            details.ReturnURL = returnUrl + "?currencyCodeType=" + parameters["currencyCode"] + "&paymentType=" + parameters["paymentType"];
            details.CancelURL = cancelUrl;

            /*
            *  (Optional) Email address of the buyer as entered during checkout.
            *  PayPal uses this value to pre-fill the PayPal membership sign-up portion on the PayPal pages.
            *	Character length and limitations: 127 single-byte alphanumeric characters
            */
            details.BuyerEmail = parameters["buyerMail"];

            decimal itemTotal = 0.0M;
            decimal orderTotal = 0.0M;

            // populate line item details
            //Cost of item. This field is required when you pass a value for ItemCategory.
            string amountItems = parameters["itemAmount"];

            /*
             * Item quantity. This field is required when you pass a value for ItemCategory. 
             * For digital goods (ItemCategory=Digital), this field is required.
               Character length and limitations: Any positive integer
               This field is introduced in version 53.0. 
             */
            string qtyItems = parameters["itemQuantity"];

            /*
             * Item name. This field is required when you pass a value for ItemCategory.
                Character length and limitations: 127 single-byte characters
                This field is introduced in version 53.0. 
             */
            string names = parameters["itemName"];

            List<PaymentDetailsItemType> lineItems = new List<PaymentDetailsItemType>();
            PaymentDetailsItemType item = new PaymentDetailsItemType();
            BasicAmountType amt = new BasicAmountType();

            //PayPal uses 3-character ISO-4217 codes for specifying currencies in fields and variables. 
            amt.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);
            amt.value = amountItems;
            item.Quantity = Convert.ToInt32(qtyItems);
            item.Name = names;
            item.Amount = amt;

            /*
             * Indicates whether an item is digital or physical. For digital goods, this field is required and must be set to Digital. It is one of the following values:
                1.Digital
                2.Physical
               This field is available since version 65.1. 
             */
            item.ItemCategory = (ItemCategoryType)Enum.Parse(typeof(ItemCategoryType), parameters["itemCategory"]);

            /*
             *  (Optional) Item description.
                Character length and limitations: 127 single-byte characters
                This field is introduced in version 53.0. 
             */
            item.Description = parameters["itemDescription"];
            lineItems.Add(item);

            /*
             * (Optional) Item sales tax.
                Note: You must set the currencyID attribute to one of 
                the 3-character currency codes for any of the supported PayPal currencies.
                Character length and limitations: Value is a positive number which cannot exceed $10,000 USD in any currency.
                It includes no currency symbol. It must have 2 decimal places, the decimal separator must be a period (.), 
                and the optional thousands separator must be a comma (,).
             */
            if (parameters["salesTax"] != string.Empty)
            {
                item.Tax = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), parameters["salesTax"]);
            }

            itemTotal += Convert.ToDecimal(qtyItems) * Convert.ToDecimal(amountItems);
            orderTotal += itemTotal;

            List<PaymentDetailsType> payDetails = new List<PaymentDetailsType>();
            PaymentDetailsType paydtl = new PaymentDetailsType();
            /*
             * How you want to obtain payment. When implementing parallel payments, 
             * this field is required and must be set to Order.
             *  When implementing digital goods, this field is required and must be set to Sale.
             *   If the transaction does not include a one-time purchase, this field is ignored. 
             *   It is one of the following values:

                Sale – This is a final sale for which you are requesting payment (default).
                Authorization – This payment is a basic authorization subject to settlement with PayPal Authorization and Capture.
                Order – This payment is an order authorization subject to settlement with PayPal Authorization and Capture.
             */
            paydtl.PaymentAction = (PaymentActionCodeType)Enum.Parse(typeof(PaymentActionCodeType), parameters["paymentType"]);

            /*
             *  (Optional) Total shipping costs for this order.
                Note:
                You must set the currencyID attribute to one of the 3-character currency codes 
                for any of the supported PayPal currencies.
                Character length and limitations: 
                Value is a positive number which cannot exceed $10,000 USD in any currency.
                It includes no currency symbol. 
                It must have 2 decimal places, the decimal separator must be a period (.), 
                and the optional thousands separator must be a comma (,)
             */
            if (parameters["shippingTotal"] != string.Empty)
            {
                BasicAmountType shippingTotal = new BasicAmountType();
                shippingTotal.value = parameters["shippingTotal"];
                shippingTotal.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);
                orderTotal += Convert.ToDecimal(parameters["shippingTotal"]);
                paydtl.ShippingTotal = shippingTotal;
            }

            /*
             *  (Optional) Total shipping insurance costs for this order. 
             *  The value must be a non-negative currency amount or null if you offer insurance options.
                 Note:
                 You must set the currencyID attribute to one of the 3-character currency 
                 codes for any of the supported PayPal currencies.
                 Character length and limitations: 
                 Value is a positive number which cannot exceed $10,000 USD in any currency. 
                 It includes no currency symbol. It must have 2 decimal places,
                 the decimal separator must be a period (.), 
                 and the optional thousands separator must be a comma (,).
                 InsuranceTotal is available since version 53.0.
             */
            if (parameters["insuranceTotal"] != string.Empty)
            {
                paydtl.InsuranceTotal = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), parameters["insuranceTotal"]);
                paydtl.InsuranceOptionOffered = "true";
                orderTotal += Convert.ToDecimal(parameters["insuranceTotal"]);
            }

            /*
            *  (Optional) Total handling costs for this order.
                Note:
                You must set the currencyID attribute to one of the 3-character currency codes 
                for any of the supported PayPal currencies.
                Character length and limitations: Value is a positive number which 
                cannot exceed $10,000 USD in any currency.
                It includes no currency symbol. It must have 2 decimal places, 
                the decimal separator must be a period (.), and the optional 
                thousands separator must be a comma (,). 
            */
            if (parameters["handlingTotal"] != string.Empty)
            {
                paydtl.HandlingTotal = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), parameters["handlingTotal"]);
                orderTotal += Convert.ToDecimal(parameters["handlingTotal"]);
            }

            /*
             *  (Optional) Sum of tax for all items in this order.
                 Note:
                 You must set the currencyID attribute to one of the 3-character currency codes
                 for any of the supported PayPal currencies.
                 Character length and limitations: Value is a positive number which 
                 cannot exceed $10,000 USD in any currency. It includes no currency symbol.
                 It must have 2 decimal places, the decimal separator must be a period (.),
                 and the optional thousands separator must be a comma (,).
             */
            if (parameters["taxTotal"] != string.Empty)
            {
                paydtl.TaxTotal = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), parameters["taxTotal"]);
                orderTotal += Convert.ToDecimal(parameters["taxTotal"]);
            }

            /*
             *  (Optional) Description of items the buyer is purchasing.
                 Note:
                 The value you specify is available only if the transaction includes a purchase.
                 This field is ignored if you set up a billing agreement for a recurring payment 
                 that is not immediately charged.
                 Character length and limitations: 127 single-byte alphanumeric characters
             */
            if (parameters["orderDescription"] != string.Empty)
            {
                paydtl.OrderDescription = parameters["orderDescription"];
            }

            BasicAmountType itemsTotal = new BasicAmountType();
            itemsTotal.value = Convert.ToString(itemTotal);

            //PayPal uses 3-character ISO-4217 codes for specifying currencies in fields and variables. 
            itemsTotal.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);

            paydtl.OrderTotal = new BasicAmountType((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]), Convert.ToString(orderTotal));
            paydtl.PaymentDetailsItem = lineItems;

            paydtl.ItemTotal = itemsTotal;
            /*
             *  (Optional) Your URL for receiving Instant Payment Notification (IPN) 
             *  about this transaction. If you do not specify this value in the request, 
             *  the notification URL from your Merchant Profile is used, if one exists.
                Important:
                The notify URL applies only to DoExpressCheckoutPayment. 
                This value is ignored when set in SetExpressCheckout or GetExpressCheckoutDetails.
                Character length and limitations: 2,048 single-byte alphanumeric characters
             */
            paydtl.NotifyURL = parameters["notifyURL"];

            payDetails.Add(paydtl);
            details.PaymentDetails = payDetails;

            setExpressCheckoutReq.SetExpressCheckoutRequestDetails = details;

            SetExpressCheckoutReq expressCheckoutReq = new SetExpressCheckoutReq();
            expressCheckoutReq.SetExpressCheckoutRequest = setExpressCheckoutReq;

            SetExpressCheckoutResponseType resp = null;
            try
            {
                resp = service.SetExpressCheckout(expressCheckoutReq);
            }
            catch (System.Exception e)
            {
                context.Response.Write(e.StackTrace);
                return;
            }

            // Display response values. 
            Dictionary<string, string> keyResponseParams = new Dictionary<string, string>();
            string redirectUrl = null;
            if (!(resp.Ack.Equals(AckCode.FAILURE) && !(resp.Ack.Equals(AckCode.FAILUREWITHWARNING))))
            {
                redirectUrl = ConfigurationManager.AppSettings["PAYPAL_REDIRECT_URL"].ToString() + "_express-checkout&token=" + resp.Token;
                keyResponseParams.Add("Acknowledgement", resp.Ack.ToString());
            }
            displayResponse(context, "SetExpressCheckoutPaymentOrder", keyResponseParams, service.getLastRequest(), service.getLastResponse(), resp.Errors, redirectUrl);
        }
        
        /// <summary>
        /// Handles DoExpressCheckout
        /// </summary>
        /// <param name="context"></param>
        private void DoExpressCheckout(HttpContext context)
        {
            NameValueCollection parameters = context.Request.Params;

            // Configuration map containing signature credentials and other required configuration.
            // For a full list of configuration parameters refer at 
            // [https://github.com/paypal/merchant-sdk-java/wiki/SDK-Configuration-Parameters]
            Dictionary<String, String> configurationMap = Configuration.GetSignatureConfig();

            // Creating service wrapper object to make an API call by loading configuration map.
            PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(configurationMap);

            DoExpressCheckoutPaymentRequestType doCheckoutPaymentRequestType = new DoExpressCheckoutPaymentRequestType();
            DoExpressCheckoutPaymentRequestDetailsType details = new DoExpressCheckoutPaymentRequestDetailsType();

            /*
             * A timestamped token by which you identify to PayPal that you are processing
             * this payment with Express Checkout. The token expires after three hours. 
             * If you set the token in the SetExpressCheckout request, the value of the token
             * in the response is identical to the value in the request.
               Character length and limitations: 20 single-byte characters
             */
            details.Token = parameters["token"];

            /*
             * Unique PayPal Customer Account identification number.
               Character length and limitations: 13 single-byte alphanumeric characters
             */
            details.PayerID = parameters["payerID"];

            /*
             *  (Optional) How you want to obtain payment. If the transaction does not include
             *  a one-time purchase, this field is ignored. 
             *  It is one of the following values:
                    Sale – This is a final sale for which you are requesting payment (default).
                    Authorization – This payment is a basic authorization subject to settlement with PayPal Authorization and Capture.
                    Order – This payment is an order authorization subject to settlement with PayPal Authorization and Capture.
                Note:
                You cannot set this field to Sale in SetExpressCheckout request and then change 
                this value to Authorization or Order in the DoExpressCheckoutPayment request. 
                If you set the field to Authorization or Order in SetExpressCheckout, 
                you may set the field to Sale.
                Character length and limitations: Up to 13 single-byte alphabetic characters
                This field is deprecated. Use PaymentAction in PaymentDetailsType instead.
             */
            details.PaymentAction = (PaymentActionCodeType)Enum.Parse(typeof(PaymentActionCodeType), parameters["paymentType"]);

            decimal itemTotalAmt = 0.0M;
            decimal orderTotalAmt = 0.0M;
            String amt = parameters["amt"];
            String quantity = parameters["itemQuantity"];
            itemTotalAmt = Convert.ToDecimal(amt) * Convert.ToDecimal(quantity);
            orderTotalAmt += itemTotalAmt;

            PaymentDetailsType paymentDetails = new PaymentDetailsType();
            BasicAmountType orderTotal = new BasicAmountType();
            orderTotal.value = Convert.ToString(orderTotalAmt);

            //PayPal uses 3-character ISO-4217 codes for specifying currencies in fields and variables.
            orderTotal.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);
            /*
             *  (Required) The total cost of the transaction to the buyer. 
             *  If shipping cost (not applicable to digital goods) and tax charges are known, 
             *  include them in this value. If not, this value should be the current sub-total 
             *  of the order. If the transaction includes one or more one-time purchases, this 
             *  field must be equal to the sum of the purchases. Set this field to 0 if the 
             *  transaction does not include a one-time purchase such as when you set up a 
             *  billing agreement for a recurring payment that is not immediately charged. 
             *  When the field is set to 0, purchase-specific fields are ignored. 
             *  For digital goods, the following must be true:
                total cost > 0
                total cost <= total cost passed in the call to SetExpressCheckout
             Note:
                You must set the currencyID attribute to one of the 3-character currency codes 
                for any of the supported PayPal currencies.
                When multiple payments are passed in one transaction, all of the payments must 
                have the same currency code.
                Character length and limitations: Value is a positive number which cannot 
                exceed $10,000 USD in any currency. It includes no currency symbol. 
                It must have 2 decimal places, the decimal separator must be a period (.), 
                and the optional thousands separator must be a comma (,).
             */
            paymentDetails.OrderTotal = orderTotal;

            BasicAmountType itemTotal = new BasicAmountType();
            itemTotal.value = Convert.ToString(itemTotalAmt);

            //PayPal uses 3-character ISO-4217 codes for specifying currencies in fields and variables.
            itemTotal.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);

            /*
             *  Sum of cost of all items in this order. For digital goods, this field is 
             *  required. PayPal recommends that you pass the same value in the call to 
             *  DoExpressCheckoutPayment that you passed in the call to SetExpressCheckout.
             Note:
                You must set the currencyID attribute to one of the 3-character currency 
                codes for any of the supported PayPal currencies.
                Character length and limitations: Value is a positive number which cannot 
                exceed $10,000 USD in any currency. It includes no currency symbol. 
                It must have 2 decimal places, the decimal separator must be a period (.), 
                and the optional thousands separator must be a comma (,).
             */
            paymentDetails.ItemTotal = itemTotal;

            List<PaymentDetailsItemType> paymentItems = new List<PaymentDetailsItemType>();
            PaymentDetailsItemType paymentItem = new PaymentDetailsItemType();

            /*
             * Item name. This field is required when you pass a value for ItemCategory.
               Character length and limitations: 127 single-byte characters
               This field is introduced in version 53.0. 
             */
            paymentItem.Name = parameters["itemName"];
            /*
             * Item quantity. This field is required when you pass a value for ItemCategory. 
             * For digital goods (ItemCategory=Digital), this field is required.
                Character length and limitations: Any positive integer
                This field is introduced in version 53.0. 
             */
            paymentItem.Quantity = Convert.ToInt32(parameters["itemQuantity"]);
            BasicAmountType amount = new BasicAmountType();

            /*
             * Cost of item. This field is required when you pass a value for ItemCategory.
            Note:
            You must set the currencyID attribute to one of the 3-character currency codes for
            any of the supported PayPal currencies.
            Character length and limitations: Value is a positive number which cannot 
            exceed $10,000 USD in any currency. It includes no currency symbol.
            It must have 2 decimal places, the decimal separator must be a period (.), 
            and the optional thousands separator must be a comma (,).
            This field is introduced in version 53.0.
             */
            amount.value = parameters["amt"];

            //PayPal uses 3-character ISO-4217 codes for specifying currencies in fields and variables.
            amount.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);
            paymentItem.Amount = amount;
            paymentItems.Add(paymentItem);
            paymentDetails.PaymentDetailsItem = paymentItems;

            /*
             *  (Optional) Your URL for receiving Instant Payment Notification (IPN) 
             *  about this transaction. If you do not specify this value in the request, 
             *  the notification URL from your Merchant Profile is used, if one exists.
                Important:
                The notify URL applies only to DoExpressCheckoutPayment. 
                This value is ignored when set in SetExpressCheckout or GetExpressCheckoutDetails.
                Character length and limitations: 2,048 single-byte alphanumeric characters
             */
            paymentDetails.NotifyURL = parameters["notifyURL"];

            List<PaymentDetailsType> payDetailType = new List<PaymentDetailsType>();
            payDetailType.Add(paymentDetails);

            /*
             * When implementing parallel payments, you can create up to 10 sets of payment 
             * details type parameter fields, each representing one payment you are hosting 
             * on your marketplace.
             */
            details.PaymentDetails = payDetailType;

            doCheckoutPaymentRequestType.DoExpressCheckoutPaymentRequestDetails = details;
            DoExpressCheckoutPaymentReq doExpressCheckoutPaymentReq = new DoExpressCheckoutPaymentReq();
            doExpressCheckoutPaymentReq.DoExpressCheckoutPaymentRequest = doCheckoutPaymentRequestType;
            DoExpressCheckoutPaymentResponseType resp = null;
            try
            {
                
                resp = service.DoExpressCheckoutPayment(doExpressCheckoutPaymentReq);
            }
            catch (System.Exception ex)
            {
                context.Response.Write(ex.StackTrace);
                return;
            }

            // Display response values. 
            Dictionary<string, string> keyResponseParams = new Dictionary<string, string>();
            string redirectUrl = null;
            if (!(resp.Ack.Equals(AckCode.FAILURE) && !(resp.Ack.Equals(AckCode.FAILUREWITHWARNING))))
            {
                keyResponseParams.Add("Acknowledgement", resp.Ack.ToString());
                keyResponseParams.Add("TransactionID", resp.DoExpressCheckoutPaymentResponseDetails.PaymentInfo[0].TransactionID);
            }
            displayResponse(context, "DoExpressCheckout", keyResponseParams, service.getLastRequest(), service.getLastResponse(), resp.Errors, redirectUrl);
        }

        /// <summary>
        /// Handles DoCapture
        /// </summary>
        /// <param name="context"></param>
        private void DoCapture(HttpContext context)
        {
            NameValueCollection parameters = context.Request.Params;

            // Configuration map containing signature credentials and other required configuration.
            // For a full list of configuration parameters refer at 
            // [https://github.com/paypal/merchant-sdk-java/wiki/SDK-Configuration-Parameters]
            Dictionary<String, String> configurationMap = Configuration.GetSignatureConfig();

            // Creating service wrapper object to make an API call by loading configuration map.
            PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(configurationMap);

            // ## DoCaptureReq
            DoCaptureReq req = new DoCaptureReq();
            // `Amount` to capture which takes mandatory params:
            //
            // * `currencyCode`
            // * `amount`
            BasicAmountType amount = new BasicAmountType(((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"])), parameters["amt"]);

            // `DoCaptureRequest` which takes mandatory params:
            //
            // * `Authorization ID` - Authorization identification number of the
            // payment you want to capture. This is the transaction ID returned from
            // DoExpressCheckoutPayment, DoDirectPayment, or CheckOut. For
            // point-of-sale transactions, this is the transaction ID returned by
            // the CheckOut call when the payment action is Authorization.
            // * `amount` - Amount to capture
            // * `CompleteCode` - Indicates whether or not this is your last capture.
            // It is one of the following values:
            // * Complete – This is the last capture you intend to make.
            // * NotComplete – You intend to make additional captures.
            // `Note:
            // If Complete, any remaining amount of the original authorized
            // transaction is automatically voided and all remaining open
            // authorizations are voided.`
            DoCaptureRequestType reqType = new DoCaptureRequestType
            (
                    parameters["authID"], 
                    amount,
                    (CompleteCodeType)Enum.Parse(typeof(CompleteCodeType), parameters["completeCodeType"])
            );    
            
            req.DoCaptureRequest = reqType;
            DoCaptureResponseType resp = null;
            try
            {
                resp = service.DoCapture(req);
            }
            catch (System.Exception ex)
            {
                context.Response.Write(ex.StackTrace);
                return;
            }

            // Display response values. 
            Dictionary<string, string> keyResponseParams = new Dictionary<string, string>();
            string redirectUrl = null;
            if (!(resp.Ack.Equals(AckCode.FAILURE) && !(resp.Ack.Equals(AckCode.FAILUREWITHWARNING))))
            {
                keyResponseParams.Add("Acknowledgement", resp.Ack.ToString());               
            }
            displayResponse(context, "DoCapture", keyResponseParams, service.getLastRequest(), service.getLastResponse(), resp.Errors, redirectUrl);
        }

        // <summary>
        /// Handles DoAuthorization
        /// </summary>
        /// <param name="context"></param>
        private void DoAuthorization(HttpContext context)
        {
            NameValueCollection parameters = context.Request.Params;

            // Configuration map containing signature credentials and other required configuration.
            // For a full list of configuration parameters refer at 
            // [https://github.com/paypal/merchant-sdk-java/wiki/SDK-Configuration-Parameters]
            Dictionary<String, String> configurationMap = Configuration.GetSignatureConfig();

            // Creating service wrapper object to make an API call by loading configuration map.
            PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(configurationMap);

            // ## DoAuthorizationReq
            DoAuthorizationReq req = new DoAuthorizationReq();

            // `Amount` which takes mandatory params:
            //
            // * `currencyCode`
            // * `amount
            BasicAmountType amount = new BasicAmountType(((CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"])), parameters["amt"]);

            // `DoAuthorizationRequest` which takes mandatory params:
            //
            // * `Transaction ID` - Value of the order's transaction identification
            // number returned by PayPal.
            // * `Amount` - Amount to authorize.
            DoAuthorizationRequestType reqType = new DoAuthorizationRequestType(parameters["authID"], amount);

            req.DoAuthorizationRequest = reqType;
            DoAuthorizationResponseType resp = null;
            try
            {
                resp = service.DoAuthorization(req);
            }
            catch (System.Exception ex)
            {
                context.Response.Write(ex.StackTrace);
                return;
            }

            // Display response values. 
            Dictionary<string, string> keyResponseParams = new Dictionary<string, string>();
            string redirectUrl = null;
            if (!(resp.Ack.Equals(AckCode.FAILURE) && !(resp.Ack.Equals(AckCode.FAILUREWITHWARNING))))
            {
                keyResponseParams.Add("Acknowledgement", resp.Ack.ToString());                
            }
            displayResponse(context, "DoAuthorization", keyResponseParams, service.getLastRequest(), service.getLastResponse(), resp.Errors, redirectUrl);
            ;
        }

        /// <summary>
        /// Handles ParallelPayment
        /// </summary>
        /// <param name="context"></param>
        private void ParallelPayment(HttpContext context)
        {
            NameValueCollection parameters = context.Request.Params;

            // Configuration map containing signature credentials and other required configuration.
            // For a full list of configuration parameters refer at 
            // [https://github.com/paypal/merchant-sdk-dotnet/wiki/SDK-Configuration-Parameters]
            Dictionary<string, string> configurationMap = Configuration.GetSignatureConfig();

            // Create the PayPalAPIInterfaceServiceService service object to make the API call
            PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(configurationMap);

            SetExpressCheckoutRequestType setExpressCheckoutReq = new SetExpressCheckoutRequestType();
            SetExpressCheckoutRequestDetailsType details = new SetExpressCheckoutRequestDetailsType();

            string requestUrl = ConfigurationManager.AppSettings["HOSTING_ENDPOINT"].ToString();

            // (Required) URL to which the buyer's browser is returned after choosing to pay with PayPal. For digital goods, you must add JavaScript to this page to close the in-context experience.
            // Note:
            // PayPal recommends that the value be the final review page on which the buyer confirms the order and payment or billing agreement.
            UriBuilder uriBuilder = new UriBuilder(requestUrl);
            uriBuilder.Path = context.Request.ApplicationPath
                + (context.Request.ApplicationPath.EndsWith("/") ? string.Empty : "/")
                + "UseCaseSamples/SetExpressCheckoutPaymentAuthorization.aspx";
            string returnUrl = uriBuilder.Uri.ToString();

            //(Required) URL to which the buyer is returned if the buyer does not approve the use of PayPal to pay you. For digital goods, you must add JavaScript to this page to close the in-context experience.
            // Note:
            // PayPal recommends that the value be the original page on which the buyer chose to pay with PayPal or establish a billing agreement.
            uriBuilder = new UriBuilder(requestUrl);
            uriBuilder.Path = context.Request.ApplicationPath
                + (context.Request.ApplicationPath.EndsWith("/") ? string.Empty : "/")
                + "UseCaseSamples/SetExpressCheckoutPaymentAuthorization.aspx";
            string cancelUrl = uriBuilder.Uri.ToString();

            /*
              *  (Required) URL to which the buyer's browser is returned after choosing 
              *  to pay with PayPal. For digital goods, you must add JavaScript to this 
              *  page to close the in-context experience.
               Note:
                 PayPal recommends that the value be the final review page on which the buyer 
                 confirms the order and payment or billing agreement.
                 Character length and limitations: 2048 single-byte characters
              */
            details.ReturnURL = returnUrl + "?currencyCodeType=" + parameters["currencyCode"];
            details.CancelURL = cancelUrl;

            /*
            *  (Optional) Email address of the buyer as entered during checkout.
            *  PayPal uses this value to pre-fill the PayPal membership sign-up portion on the PayPal pages.
            *	Character length and limitations: 127 single-byte alphanumeric characters
            */
            details.BuyerEmail = parameters["buyerMail"];

            SellerDetailsType seller1 = new SellerDetailsType();
            seller1.PayPalAccountID = parameters["receiverEmail_0"];
            PaymentDetailsType paymentDetails1 = new PaymentDetailsType();
            paymentDetails1.SellerDetails = seller1;
            paymentDetails1.PaymentRequestID = "CART286-PAYMENT1";
            BasicAmountType orderTotal1 = new BasicAmountType();
            orderTotal1.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);
            orderTotal1.value = parameters["orderTotal"];
            paymentDetails1.OrderTotal = orderTotal1;

            SellerDetailsType seller2 = new SellerDetailsType();
            seller2.PayPalAccountID = parameters["receiverEmail_1"];
            PaymentDetailsType paymentDetails2 = new PaymentDetailsType();
            paymentDetails2.SellerDetails = seller2;
            paymentDetails2.PaymentRequestID = "CART286-PAYMENT2";
            BasicAmountType orderTotal2 = new BasicAmountType();
            orderTotal2.currencyID = (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), parameters["currencyCode"]);
            orderTotal2.value = parameters["orderTotal"];
            paymentDetails2.OrderTotal = orderTotal2;

            List<PaymentDetailsType> payDetails = new List<PaymentDetailsType>();
            payDetails.Add(paymentDetails1);
            payDetails.Add(paymentDetails2);

            details.PaymentDetails = payDetails;
            setExpressCheckoutReq.SetExpressCheckoutRequestDetails = details;

            SetExpressCheckoutReq expressCheckoutReq = new SetExpressCheckoutReq();
            expressCheckoutReq.SetExpressCheckoutRequest = setExpressCheckoutReq;
            SetExpressCheckoutResponseType resp = null;

            try
            {
                resp = service.SetExpressCheckout(expressCheckoutReq);
            }
            catch (System.Exception e)
            {
                context.Response.Write(e.StackTrace);
                return;
            }

            // Display response values. 
            Dictionary<string, string> keyResponseParams = new Dictionary<string, string>();
            string redirectUrl = null;
            if (!(resp.Ack.Equals(AckCode.FAILURE) && !(resp.Ack.Equals(AckCode.FAILUREWITHWARNING))))
            {
                keyResponseParams.Add("Acknowledgement", resp.Ack.ToString());
            }
            displayResponse(context, "ParallelPayment", keyResponseParams, service.getLastRequest(), service.getLastResponse(), resp.Errors, redirectUrl);
        }

        /// <summary>
        /// Utility method for displaying API response
        /// </summary>
        /// <param name="context"></param>
        /// <param name="apiName"></param>
        /// <param name="responseValues"></param>
        /// <param name="requestPayload"></param>
        /// <param name="responsePayload"></param>
        /// <param name="errorMessages"></param>
        /// <param name="redirectUrl"></param>
        private void displayResponse(HttpContext context, string apiName, Dictionary<string, string> responseValues,
            string requestPayload, string responsePayload, List<ErrorType> errorMessages, string redirectUrl)
        {

            context.Response.Write("<html><head><title>");
            context.Response.Write("PayPal Adaptive Payments - " + apiName);
            context.Response.Write("</title><link rel='stylesheet' href='../APICalls/sdk.css' type='text/css'/></head><body>");
            context.Response.Write("<h3>" + apiName + " Response</h3>");
            if (errorMessages != null && errorMessages.Count > 0)
            {
                context.Response.Write("<div class='section_header'>Error messages</div>");
                context.Response.Write("<div class='note'>Investigate the Response object for further error information</div><ul>");
                foreach (ErrorType error in errorMessages)
                {
                    context.Response.Write("<li>" + error.LongMessage + "</li>");
                }
                context.Response.Write("</ul>");
            }
            if (redirectUrl != null)
            {
                string red = "<div>This API involves a web flow. You must now redirect your user to " + redirectUrl;
                red = red + "<br />Please click <a href='" + redirectUrl + "' target='_self'>here</a> to try the flow.</div><br/>";
                context.Response.Write(red);
            }
            context.Response.Write("<div class='section_header'>Key values from Response</div>");
            context.Response.Write("<div class='note'>Consult Response object and reference doc for complete list of Response values.</div><table>");
                       
            foreach (KeyValuePair<string, string> entry in responseValues)
            {
                context.Response.Write("<tr><td class='label'>");
                context.Response.Write(entry.Key);
                context.Response.Write(": </td><td>");

                if (entry.Key == "Redirect To PayPal")
                {
                    context.Response.Write("<a id='");
                    context.Response.Write(entry.Key);
                    context.Response.Write("' href=");
                    context.Response.Write(entry.Value);
                    context.Response.Write(">Redirect To PayPal</a>");
                }
                else
                {
                    context.Response.Write("<div id='");
                    context.Response.Write(entry.Key);
                    context.Response.Write("'>");
                    context.Response.Write(entry.Value);
                }

                context.Response.Write("</td></tr>");
            }

            context.Response.Write("</table><h4>Request:</h4><br/><textarea rows=15 cols=80 readonly>");
            context.Response.Write(requestPayload);
            context.Response.Write("</textarea><br/><h4>Response</h4><br/><textarea rows=15 cols=80 readonly>");
            context.Response.Write(responsePayload);
            context.Response.Write("</textarea>");
            context.Response.Write("<br/><br/><a href='../Default.aspx'>Home<a><br/><br/></body></html>");

            if (apiName == "DoExpressCheckout")
            {
                context.Response.Write("<div id=\"related_calls\">");
		        context.Response.Write("See also");
                string transactionID = responseValues["TransactionID"];
                context.Response.Write("<ul>If paymentType is <b>Authorization</b>. You can capture the payment directly using DoCapture API<li><a id=\"DoCapture\" href='/UseCaseSamples/PaymentCapture.aspx?TransactionId=" + transactionID + "'>DoCapture</a></li>If  paymentType is <b>Order</b>. you need to call DoAuthorization API, before you can capture the payment using DoCapture API.<li><a href='/UseCaseSamples/DoAuthorizationForOrderPayment.aspx?TransactionId=" + transactionID + "'>DoAuthorization</a></li></ul>");
                context.Response.Write("</div>");
            }
        }
    }
}