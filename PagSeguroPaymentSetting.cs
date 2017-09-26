using Grand.Core.Configuration;

namespace NopBrasil.Plugin.Payments.PagSeguro
{
    public class PagSeguroPaymentSetting : ISettings
    {
        public string PagSeguroEmail { get; set; }
        public string PagSeguroToken { get; set; }
        public string PaymentMethodDescription { get; set; }
    }
}
