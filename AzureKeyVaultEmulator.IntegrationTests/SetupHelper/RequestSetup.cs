namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper
{
    public static class RequestSetup
    {
        public static StringContent CreateSecretModel(
            string value,
            string contentType = "text/plain",
            bool enabled = true,
            int expiration = int.MaxValue,
            int notBefore = int.MinValue)
        {
            var secret = new SetSecretModel
            {
                Value = value,
                ContentType = contentType,
                Tags = [],
                SecretAttributes = new SecretAttributesModel
                {
                    Enabled = enabled,
                    Expiration = expiration,
                    NotBefore = notBefore
                }
            };

            return secret.CreateRequestModel();
        }

        private static StringContent CreateRequestModel<TModel>(this TModel model) where TModel : ICreateItem
        {
            ArgumentNullException.ThrowIfNull(model);

            var json = JsonSerializer.Serialize(model);

            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}
