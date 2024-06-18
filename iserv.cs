using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IServAPI
{
    
internal class iserv
    {
        HttpClient client = new HttpClient();
        // Define the CREDUI_INFO structure
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            public string pszMessageText;
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        // Define flags for the prompt
        [Flags]
        enum PromptForWindowsCredentialsFlags
        {
            CREDUIWIN_GENERIC = 0x1,
            CREDUIWIN_CHECKBOX = 0x2,
            CREDUIWIN_AUTHPACKAGE_ONLY = 0x10,
            CREDUIWIN_IN_CRED_ONLY = 0x20,
            CREDUIWIN_ENUMERATE_ADMINS = 0x100,
            CREDUIWIN_ENUMERATE_CURRENT_USER = 0x200,
            CREDUIWIN_SECURE_PROMPT = 0x1000,
            CREDUIWIN_PREPROMPTING = 0x2000,
            CREDUIWIN_PACK_32_WOW = 0x10000000,
        }

        // P/Invoke declaration for CredUIPromptForWindowsCredentials
        [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int CredUIPromptForWindowsCredentials(
            ref CREDUI_INFO pUiInfo,
            int dwAuthError,
            ref uint pulAuthPackage,
            IntPtr pvInAuthBuffer,
            uint ulInAuthBufferSize,
            out IntPtr ppvOutAuthBuffer,
            out uint pulOutAuthBufferSize,
            ref bool pfSave,
            PromptForWindowsCredentialsFlags dwFlags);

        // P/Invoke declaration for CredUnPackAuthenticationBuffer
        [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredUnPackAuthenticationBuffer(
            uint dwFlags,
            IntPtr pAuthBuffer,
            uint cbAuthBuffer,
            StringBuilder pszUserName,
            ref int pcchMaxUserName,
            StringBuilder pszDomainName,
            ref int pcchMaxDomainName,
            StringBuilder pszPassword,
            ref int pcchMaxPassword);

        private async Task auth(HttpClient client, string server, string username, string password, bool suppressOutput)
        {
            Console.WriteLine("Authenticating...");
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://{server}/iserv/auth/login"),
                Headers =
        {
            { "User-Agent", "iserv.cs/API1.0" },
        },
                Content = new MultipartFormDataContent
        {
            { new StringContent(username), "_username" },
            { new StringContent(password), "_password" }
        }
            };

            try
            {
                Console.WriteLine("Waiting for Server...");
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStringAsync();

                    await loadHome(server, client);

                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        public async Task UserAuth(HttpClient client, string server)
        {
            var creduiInfo = new CREDUI_INFO
            {
                cbSize = Marshal.SizeOf(typeof(CREDUI_INFO)),
                pszMessageText = $"Please enter your credentials for {server}.",
                pszCaptionText = "User Authentication",
                hwndParent = IntPtr.Zero,
                hbmBanner = IntPtr.Zero
            };

            uint authPackage = 0;
            bool save = false;
            IntPtr outCredBuffer;
            uint outCredSize;

            int result = CredUIPromptForWindowsCredentials(
                ref creduiInfo,
                0,
                ref authPackage,
                IntPtr.Zero,
                0,
                out outCredBuffer,
                out outCredSize,
                ref save,
                PromptForWindowsCredentialsFlags.CREDUIWIN_GENERIC);

            if (result == 0)
            {
                int maxUserName = 100;
                int maxDomainName = 100;
                int maxPassword = 100;
                StringBuilder userName = new StringBuilder(maxUserName);
                StringBuilder domainName = new StringBuilder(maxDomainName);
                StringBuilder password = new StringBuilder(maxPassword);

                if (CredUnPackAuthenticationBuffer(
                    0,
                    outCredBuffer,
                    outCredSize,
                    userName,
                    ref maxUserName,
                    domainName,
                    ref maxDomainName,
                    password,
                    ref maxPassword))
                {
                    Console.WriteLine($"Username: {userName.ToString()}");
                    Console.WriteLine($"Password: {password.ToString()}");

                    await auth(client, server, userName.ToString(), password.ToString(), false);
                }
                else
                {
                    Console.WriteLine("Failed to unpack credentials.");
                }

                // Free the memory allocated for the outCredBuffer
                Marshal.FreeCoTaskMem(outCredBuffer);
            }
            else
            {
                Console.WriteLine("User canceled the credential prompt.");
            }
        }

        async Task loadHome(string server, HttpClient client)
        {
            Console.WriteLine("Loading Home...");
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://{server}/iserv"),
                Headers =
            {
                { "User-Agent", "iserv.cs/API1.0" },
            }
            };

            try
            {
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStringAsync();

                    // Save the response body to a file
                    File.WriteAllText("response.html", responseBody);
                    Console.WriteLine("Response body saved to response.html.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        public async Task getMail(HttpClient client, string server)
        {
            Console.WriteLine("Loading Mail...");
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://{server}/iserv/mail"),
                Headers =
            {
                { "User-Agent", "iserv.cs/API1.0" },
            }
            };

            try
            {
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStringAsync();

                    // Save the response body to a file
                    File.WriteAllText("mail.html", responseBody);
                    Console.WriteLine("Mail saved to mail.html.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
