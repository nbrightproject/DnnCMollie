using System;
using System.Collections;
using System.Diagnostics.Eventing.Reader;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using Nevoweb.DNN.NBrightBuy.Components;

namespace DnnC.Mollie
{

    public class PayData
    {

        public PayData(OrderData oInfo)
        {
            LoadSettings(oInfo);
        }

        public void LoadSettings(OrderData oInfo)
        {
            var settings = ProviderUtils.GetProviderSettings("DnnCMolliepayment");
            var appliedtotal = oInfo.PurchaseInfo.GetXmlPropertyDouble("genxml/appliedtotal");
            var alreadypaid = oInfo.PurchaseInfo.GetXmlPropertyDouble("genxml/alreadypaid");

            var orderTotal = (appliedtotal - alreadypaid).ToString("0.00");

        }

    }


}
