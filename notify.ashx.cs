using System;
using System.Web;
using Nevoweb.DNN.NBrightBuy.Components;
using NBrightDNN;
using DnnC.Mollie.Api;
using System.IO;
using DotNetNuke.Entities.Portals;
using NBrightCore.common;

namespace DnnC.Mollie
{
    /// <summary>
    /// Summary description for XMLconnector
    /// </summary>
    public class DnnCMollieNotify : IHttpHandler
    {
        private String _lang = "";

        /// <summary>
        /// This function needs to process and returned message from the bank.
        /// Thsi processing may vary widely between banks.
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            var modCtrl = new NBrightBuyController();
            var info = ProviderUtils.GetProviderSettings("DnnCMolliepayment");

            try
            {

                var debugMode = info.GetXmlPropertyBool("genxml/checkbox/debugmode");

                var debugMsg = "START CALL" + DateTime.Now.ToString("s") + " </br>";
                debugMsg += "returnmessage: " + context.Request.Form.Get("returnmessage") + "</br>";
                if (debugMode)
                {
                    info.SetXmlProperty("genxml/debugmsg", debugMsg);
                    modCtrl.Update(info);
                }
                debugMsg = "DnnCMollie DEBUG: " + DateTime.Now.ToString("s") + " </br>";
                var rtnMsg = "version=2" + Environment.NewLine + "cdr=1";

                // ------------------------------------------------------------------------
                // In this case the payment provider passes back data via form POST.
                // Get the data we need.

                var testMode = info.GetXmlPropertyBool("genxml/checkbox/testmode");
                var testApiKey = info.GetXmlProperty("genxml/textbox/testapikey");
                var liveApiKey = info.GetXmlProperty("genxml/textbox/liveapikey");

                var nbi = new NBrightInfo();
                var paymentMethod = nbi.GetXmlProperty("genxml/textbox/paymentmethod");
                var paymentBank = nbi.GetXmlProperty("genxml/textbox/paymentbank");
                var apiKey = testApiKey;

                if (!testMode)
                {
                    apiKey = liveApiKey;
                }

                string molliePaymentId = context.Request.Form["id"];
                int oId = -1;                

                int.TryParse(context.Request.Form["orderid"], out oId);
                if (oId <= 0)
                {
                    int.TryParse(context.Request.Form["ordid"], out oId);
                }

                MollieClient mollieClient = new MollieClient();
                mollieClient.setApiKey(apiKey);
                PaymentStatus paymentStatus = mollieClient.GetStatus(molliePaymentId);
                
                var orderid = paymentStatus.metadata;
                var nbInfo = modCtrl.Get(Convert.ToInt32(orderid), "ORDER");
 
                if (nbInfo != null)
                {
                    var orderData = new OrderData(nbInfo.ItemID);
                    
                    switch (paymentStatus.status.Value)
                    {
                        case Status.paid:
                            //set order status to Paid 040
                            orderData.PaymentOk();
                            break;
                        case Status.cancelled:
                            //set order status to Cancelled 030
                            orderData.PaymentFail("030");
                            break;
                        case Status.failed:
                            //set order status to payment failed 050
                            orderData.PaymentFail();
                            break;
                        case Status.open:
                            //set order status to Waiting for payment 060
                            orderData.PaymentOk("060");
                            break;
                        case Status.pending:
                            //set order status to Payment Not Verified 060
                            orderData.PaymentOk("060");
                            break;
                        case Status.expired:
                            //set order status to Incomplete 050
                            orderData.PaymentFail();
                            break;
                        default:
                            //set order status to payment failed 050
                            orderData.PaymentFail();
                            break;

                    }
                    
                    // Test bebugging for dev porposes
                    var rtnStr = "id returned from mollie = " + molliePaymentId;
                    rtnStr += "<br/> payment status from mollie = " + paymentStatus.status.Value;
                    rtnStr += "<br/> orderId from ipn = " + orderid;
                    rtnStr += "<br/> status or order after ipn = " + orderData.OrderStatus;
                    File.WriteAllText(PortalSettings.Current.HomeDirectoryMapPath + "\\debug_DnnC_IPN_return.html", rtnStr.ToString());

                }

            }
            catch (Exception exc)
            {
                File.WriteAllText(PortalSettings.Current.HomeDirectoryMapPath + "\\debug_DnnC_IPN_returnError.html", exc.ToString());
            }                   


        } //end

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }


    }
}