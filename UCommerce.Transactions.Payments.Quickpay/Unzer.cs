using Umbraco.Core;
using Umbraco.Core.Composing;
using UCommerce.Transactions.Payments.Unzer.Components;



namespace UCommerce.Transactions.Payments.Unzer
{
    public class UnzerComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Components().Append<UcommercePaymentMethodsConfigSetup>();
        }
    }
}
