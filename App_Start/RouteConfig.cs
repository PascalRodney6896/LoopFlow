using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace LoopFlow
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "LoopPaymentsApiPayToTill",
                url: "api/payments/pay-to-till",
                defaults: new { controller = "LoopPaymentsApi", action = "PayToTill" }
            );

            routes.MapRoute(
                name: "LoopPaymentsApiPayToPaybill",
                url: "api/payments/pay-to-paybill",
                defaults: new { controller = "LoopPaymentsApi", action = "PayToPaybill" }
            );

            routes.MapRoute(
                name: "LoopPaymentsApiSendMoneyLoop",
                url: "api/payments/send-money-loop",
                defaults: new { controller = "LoopPaymentsApi", action = "SendMoneyLoop" }
            );

            routes.MapRoute(
                name: "LoopPaymentsApiSendMoneyMpesa",
                url: "api/payments/send-money-mpesa",
                defaults: new { controller = "LoopPaymentsApi", action = "SendMoneyMpesa" }
            );

            routes.MapRoute(
                name: "LoopPaymentsApiSendMoneyPesalink",
                url: "api/payments/send-money-pesalink",
                defaults: new { controller = "LoopPaymentsApi", action = "SendMoneyPesalink" }
            );

            routes.MapRoute(
                name: "LoopPaymentsApiTestConnectivity",
                url: "api/payments/test-connectivity",
                defaults: new { controller = "LoopPaymentsApi", action = "TestConnectivity" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
