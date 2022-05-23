using System;
using System.Linq;
using System.IO;
using Umbraco.Core;
using Umbraco.Core.Composing;
using UCommerce.EntitiesV2;
using System.Reflection;
using System.Diagnostics;
using System.Web.Hosting;

namespace UCommerce.Transactions.Payments.Unzer.Components
{
    public class UcommercePaymentMethodsConfigSetup : IComponent
    {
        public void Initialize()
        {
            
        }

        public void Terminate()
        {
            //unsubscribe during shutdown
        }



        public UcommercePaymentMethodsConfigSetup()
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var companyName = versionInfo.CompanyName;

            // fix duplicate problem with default integration for quickpay gateway in uCommerce
            if (companyName.ToLower().Contains("quickpay"))
            {
                companyName += "_v2";
            }

            var serverPath = HostingEnvironment.MapPath("~/Umbraco/ucommerce/Apps/" + companyName);

            var configurationReady = Directory.Exists(serverPath);
            if (!configurationReady)
            {
                var configurationFolder = serverPath + "/Configuration";
                var binFolder = serverPath + "/bin";

                Directory.CreateDirectory(configurationFolder);
                Directory.CreateDirectory(binFolder);

                using (var stream = File.CreateText(configurationFolder + "/" + companyName + ".config"))
                {
                    stream.WriteLine("<configuration>");
                    stream.WriteLine("<components>");

                    stream.WriteLine(@"<component id=""" + companyName + @""" service=""UCommerce.Transactions.Payments.IPaymentMethodService, UCommerce"" type=""UCommerce.Transactions.Payments.Unzer.UnzerPaymentMethodService, UCommerce.Transactions.Payments.Unzer"" />");

                    stream.WriteLine(@"<component id=""" + companyName + @"PageBuilder"" service=""UCommerce.Transactions.Payments.Unzer.UnzerPageBuilder, UCommerce.Transactions.Payments.Unzer"" type=""UCommerce.Transactions.Payments.Unzer.UnzerPageBuilder, UCommerce.Transactions.Payments.Unzer"" />");

                    stream.WriteLine("</components>");
                    stream.WriteLine("</configuration>");
                }

                var fileName = Assembly.GetExecutingAssembly().GetName().Name + ".dll";
                File.Copy(HostingEnvironment.MapPath("~/bin/" + fileName), binFolder + "/" + fileName);
            }

            var definitionConfig = Definition.All().FirstOrDefault(d => d.DefinitionType.DefinitionTypeId == 4 && d.Name == companyName);
            if (definitionConfig != null)
            {
                definitionConfig.Deleted = false;
            }
            else
            {
                definitionConfig = new Definition
                {
                    Name = companyName,
                    Description = $"Configuration for {companyName}",
                    DefinitionType = DefinitionType.Get(4)
                };
            }

            var definitionfields = definitionConfig.DefinitionFields;
            if (definitionfields != null)
            {
                CreateOrUpdateDefinitionField(definitionConfig, nameof(Models.Options.ApiKey), "ShortText");
                CreateOrUpdateDefinitionField(definitionConfig, nameof(Models.Options.PrivateAccountKey), "ShortText");
                CreateOrUpdateDefinitionField(definitionConfig, nameof(Models.Options.Merchant), "ShortText", "");
                CreateOrUpdateDefinitionField(definitionConfig, nameof(Models.Options.Payment_Methods), "ShortText", "");
                CreateOrUpdateDefinitionField(definitionConfig, nameof(Models.Options.AgreementId), "ShortText", "");
                CreateOrUpdateDefinitionField(definitionConfig, nameof(Models.Options.ContinueUrl), "ShortText");
                CreateOrUpdateDefinitionField(definitionConfig, nameof(Models.Options.CancelUrl), "ShortText");
                CreateOrUpdateDefinitionField(definitionConfig, nameof(Models.Options.AutoCapture), "Boolean");
                CreateOrUpdateDefinitionField(definitionConfig, nameof(Models.Options.CancelTestCardOrders), "Boolean", "True");
                definitionConfig.Save();

                var isPaymentMethodCreated = PaymentMethod.Exists(p => p.PaymentMethodServiceName == companyName && !p.Deleted);
                if (!isPaymentMethodCreated)
                {
                    var newPaymentMethod = new PaymentMethod
                    {
                        PaymentMethodServiceName = companyName,
                        Name = companyName
                    };

                    foreach (var culture in Country.All().Where(c => !c.Deleted).Select(c => c.Culture)
                        .ToList().Where(w => !String.IsNullOrWhiteSpace(w)).Distinct(StringComparer.InvariantCultureIgnoreCase))
                    {
                        newPaymentMethod.PaymentMethodDescriptions.Add(new PaymentMethodDescription
                        {
                            DisplayName = companyName,
                            CultureCode = culture,
                            Description = companyName,
                            PaymentMethod = newPaymentMethod,
                        });
                    }
                    newPaymentMethod.Save();
                }
            }
        }
        //  Definition definition, string name, string dataType, string defaultValue = ""
        private void CreateOrUpdateDefinitionField(Definition definition, string name, string dataType, string defaultValue = "")
        {
            if (definition == null) throw new ArgumentNullException("definition " + name);

            var definitionfields = definition.DefinitionFields;
            if (!definitionfields.Any(x => x.Name == name && x.Definition.DefinitionId == definition.DefinitionId))
            {
                definition.AddDefinitionField(new DefinitionField
                {
                    Name = name,
                    Multilingual = false,
                    DisplayOnSite = true,
                    RenderInEditor = true,
                    DefaultValue = defaultValue,
                    DataType = DataType.FirstOrDefault(x => x.TypeName.Equals(dataType))
                });
            }
            else
            {
                var definitionfield = definitionfields.FirstOrDefault(x => x.Name == name && x.Definition.DefinitionId == definition.DefinitionId);
                definitionfield.Multilingual = false;
                definitionfield.Deleted = false;
                definitionfield.DisplayOnSite = true;
                definitionfield.RenderInEditor = true;
                definitionfield.DefaultValue = defaultValue;
                definitionfield.DataType = DataType.FirstOrDefault(x => x.TypeName.Equals(dataType));
            }
        }
    }
}
