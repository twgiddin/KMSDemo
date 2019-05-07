using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using System.Configuration;


namespace QuickConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            var encryptionKey = GetKeyByAlias("NetDataEncryptionKeyForLegacyApplication");
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

             foreach (string settingKey in config.AppSettings.Settings.AllKeys)
            {
                if (settingKey.Contains("Secret"))
                {
                    Console.WriteLine($"Encrypting {settingKey} original data {ConfigurationManager.AppSettings[settingKey]}");
                    var encrypted = EncryptText(ConfigurationManager.AppSettings[settingKey], encryptionKey);
                    Console.WriteLine($"encrypted {settingKey} as {encrypted}");
                    var decrypted = DecryptText(encrypted);
                    Console.WriteLine($"decrypted {settingKey} as {decrypted}");
                    config.AppSettings.Settings[settingKey].Value = encrypted;
                    Console.WriteLine("---------------------------------------------------------");
                }
           
            }
            config.Save(ConfigurationSaveMode.Modified);

            Console.WriteLine("done");
            Console.ReadKey();


        }


        static string GetKeyByAlias(string alias)
        {
            //how does it know what credentials to use? - it first looks at app.config or webconfig, 
            // then the profile in the SDK Store, then the local credentials file
            //then the environment variables AWS_ACCESS_KEY_ID & AWS_SECRET_KEY, 
            // then finally it will look at the instance profile on an EC2 instance

            //since this is a demo / local we are going to use the default profile in the SDK store, 
            // for production we would use the local store on the EC2 instance
            //this should be transparent and allow for definition by environment

            var client = new AmazonKeyManagementServiceClient();
          

           var aliasResponse =  client.ListAliases(new ListAliasesRequest() { Limit = 1000 });

          if (aliasResponse == null || aliasResponse.Aliases == null)
            {
                return null;
            }

           var foundAlias = aliasResponse.Aliases.Where(r => r.AliasName == "alias/" + alias).FirstOrDefault();
            if (foundAlias != null)
            {
                return foundAlias.TargetKeyId;
            }

            return null;
        }

      
        static string EncryptText(string textToEncrypt, string keyID)
        {
            if (string.IsNullOrWhiteSpace(textToEncrypt))
            {
                return "";
            }
            try
            {
                var client = new AmazonKeyManagementServiceClient();
                var encryptRequest = new Amazon.KeyManagementService.Model.EncryptRequest();
                encryptRequest.KeyId = keyID;
                //Encode the string as a UTF8 byte array to pass it in as the plaintext
                var textBytes = Encoding.UTF8.GetBytes(textToEncrypt);
                encryptRequest.Plaintext = new System.IO.MemoryStream(textBytes,0, textBytes.Length);
                var response = client.Encrypt(encryptRequest);
                if (response != null)
                {
                    //return it as base 64 encoded so that you can easily save it without special characters
                    return Convert.ToBase64String(response.CiphertextBlob.ToArray());
                }

            }
            catch (Exception )
            {

            }

            return "";

        }

        static string DecryptText(string encryptedText)
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
            {
                return "";
            }
            try
            {
                var client = new AmazonKeyManagementServiceClient();
                var decryptRequest = new Amazon.KeyManagementService.Model.DecryptRequest();
                //since you returned it as base64, now convert it back to the original bytes
                var fromBase64Bytes = Convert.FromBase64String(encryptedText);
                decryptRequest.CiphertextBlob = new System.IO.MemoryStream(fromBase64Bytes, 0, fromBase64Bytes.Length);
                var response = client.Decrypt(decryptRequest);
                if (response != null)
                {
                    return Encoding.UTF8.GetString(response.Plaintext.ToArray());
                }

            }
            catch (Exception )
            {

            }

            return encryptedText;

        }

        static Dictionary<string, string> GetAllConnectionStrings()
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            ConnectionStringSettingsCollection allConnectionSettings = ConfigurationManager.ConnectionStrings;
            if (allConnectionSettings != null)
            {
                foreach (ConnectionStringSettings connectionData in allConnectionSettings)
                {
                    output[connectionData.Name] = connectionData.ConnectionString;

                }

            }
            return output;
        }

    }
}
