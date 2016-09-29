using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using NBrightCore.common;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components;


using DnnC.Mollie.Api;
using System.IO;
using System.Globalization;
using System.Net;

namespace DnnC.Mollie
{
    public class DnnCMolliePaymentProvider : Nevoweb.DNN.NBrightBuy.Components.Interfaces.PaymentsInterface
    {

        public override string Paymentskey { get; set; }

        public override string GetTemplate(NBrightInfo cartInfo)
        {
            var info = ProviderUtils.GetProviderSettings("DnnCMolliepayment");
            var templ = ProviderUtils.GetTemplateMollieData(info.GetXmlProperty("genxml/textbox/checkouttemplate"), info);

            return templ;
        }

        public override string RedirectForPayment(OrderData orderData)
        {
            var appliedtotal = orderData.PurchaseInfo.GetXmlPropertyDouble("genxml/appliedtotal");
            var alreadypaid = orderData.PurchaseInfo.GetXmlPropertyDouble("genxml/alreadypaid");

            var info = ProviderUtils.GetProviderSettings("DnnCMolliepayment");
            var cartDesc = info.GetXmlProperty("genxml/textbox/cartdescription");
            var testMode = info.GetXmlPropertyBool("genxml/checkbox/testmode");
            var testApiKey = info.GetXmlProperty("genxml/textbox/testapikey");
            var liveApiKey = info.GetXmlProperty("genxml/textbox/liveapikey");
            var notifyUrl = Utils.ToAbsoluteUrl("/DesktopModules/NBright/DnnCMollie/notify.ashx");
                                                
            var returnUrl = Globals.NavigateURL(StoreSettings.Current.PaymentTabId, "");
            var ItemId = orderData.PurchaseInfo.ItemID.ToString("");

            var nbi = new NBrightInfo();
            nbi.XMLData = orderData.payselectionXml;
            var paymentMethod = nbi.GetXmlProperty("genxml/textbox/paymentmethod");
            var paymentBank = nbi.GetXmlProperty("genxml/textbox/paymentbank");
            var apiKey = testApiKey;

            if (!testMode)
            {
                apiKey = liveApiKey;
            }

            MollieClient mollieClient = new MollieClient();
            mollieClient.setApiKey(apiKey);

            Payment payment = new Payment()
            {
                //amount = decimal.Parse((appliedtotal - alreadypaid).ToString("0.00", CultureInfo.InvariantCulture)),
                amount = decimal.Parse((appliedtotal - alreadypaid).ToString("0.00")),
                description = cartDesc,
                redirectUrl = returnUrl + "/orderid/" + ItemId,
                method = (Method)Enum.Parse(typeof(Method), paymentMethod, true),
                issuer = paymentBank,
                metadata = ItemId,
                webhookUrl = notifyUrl,// + "/orderid/" + ItemId,
            };

            PaymentStatus paymentStatus = mollieClient.StartPayment(payment);

            orderData.OrderStatus = "020";
            orderData.PurchaseInfo.SetXmlProperty("genxml/paymenterror", "");
            orderData.PurchaseInfo.SetXmlProperty("genxml/posturl", paymentStatus.links.paymentUrl.Trim());
            orderData.PurchaseInfo.Lang = Utils.GetCurrentCulture();
            orderData.SavePurchaseData();
            try
            {
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.Redirect(paymentStatus.links.paymentUrl);
            }
            catch (Exception ex)
            {
                // rollback transaction
                orderData.PurchaseInfo.SetXmlProperty("genxml/paymenterror", "<div>ERROR: Invalid payment data </div><div>" + ex + "</div>");
                orderData.PaymentFail();
                var param = new string[3];
                param[0] = "orderid=" + orderData.PurchaseInfo.ItemID.ToString("");
                param[1] = "status=0";
                return Globals.NavigateURL(StoreSettings.Current.PaymentTabId, "", param);
            }

            try
            {
                HttpContext.Current.Response.End();
            }
            catch (Exception ex)
            {
                // this try/catch to avoid sending error 'ThreadAbortException'  
            }

            return "";
        }
        
        public override string ProcessPaymentReturn(HttpContext context)
        {

            var orderid = Utils.RequestQueryStringParam(context, "orderid");
            if (Utils.IsNumeric(orderid))
            {
                var orderData = new OrderData(Convert.ToInt32(orderid));


                // Test bebugging for dev porposes
                var rtnStr = "Order stats when returned = " + orderData.OrderStatus;
                rtnStr += "<br/> orderId from ipn = " + orderid;
                rtnStr += "<br/> orderId from ipn = " + orderData.OrderNumber;
                rtnStr += "<br/> status or order after ipn = " + orderData.OrderStatus;
                File.WriteAllText(PortalSettings.Current.HomeDirectoryMapPath + "\\debug_DnnC_Bank_return.html", rtnStr.ToString());

                if (orderData.OrderStatus == "010") // check we have a waiting for bank status (Cancel from bank seems to happen even after notified has accepted it as valid??)
                {
                    var rtnerr = orderData.PurchaseInfo.GetXmlProperty("genxml/paymenterror");
                    rtnerr = "Mollie Incomplete/cancelled (010)"; // to return this so a fail is activated.
                    orderData.PaymentFail();
                    return rtnerr;
                }

                if (orderData.OrderStatus == "020") // check we have a waiting for bank status (Cancel from bank seems to happen even after notified has accepted it as valid??)
                {
                    var rtnerr = orderData.PurchaseInfo.GetXmlProperty("genxml/paymenterror");
                    rtnerr = "Mollie Open (020)"; // to return this so a fail is activated.
                    orderData.PaymentFail();
                    return rtnerr;
                }

                if (orderData.OrderStatus == "030") //Cancelled
                {
                    var rtnerr = orderData.PurchaseInfo.GetXmlProperty("genxml/paymenterror");
                    rtnerr = "Mollie Cancelled (030)"; // to return this so a fail is activated.
                    orderData.PaymentFail();
                    return rtnerr;
                }

                if (orderData.OrderStatus == "050") //Failed
                {
                    var rtnerr = orderData.PurchaseInfo.GetXmlProperty("genxml/paymenterror");
                    rtnerr = "Mollie Failed (050)"; // to return this so a fail is activated.
                    orderData.PaymentFail();
                    return rtnerr;
                }

                if (orderData.OrderStatus == "060") //Failed
                {
                    var rtnerr = orderData.PurchaseInfo.GetXmlProperty("genxml/paymenterror");
                    rtnerr = "Mollie Failed (060)"; // to return this so a fail is activated.
                    orderData.PaymentFail();
                    return rtnerr;
                }

            }
            return "";
        }        

    }
}
