using Grand.Web.Framework;
using Grand.Web.Framework.Mvc;

namespace NopBrasil.Plugin.Payments.PagSeguro.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [GrandResourceDisplayName("Plugins.Payments.EmailAdmin.PagSeguro")]
        public string PagSeguroEmail { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.Token.PagSeguro")]
        public string PagSeguroToken { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.MethodDescription.PagSeguro")]
        public string PaymentMethodDescription { get; set; }
    }
}
